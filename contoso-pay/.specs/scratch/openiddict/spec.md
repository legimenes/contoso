Status: ready-for-agent

## Problem Statement

Today the planned credential model is a home-grown one: `AppClient` is designed as a business aggregate with its own `is_active` and `created_at`, a merchant can own many app clients, and there is no way to replace a leaked secret. The planned connect slice would also implement token issuance, secret hashing, and JWT signing by hand. That duplicates security-critical behavior, leaves two independent active flags to reconcile, makes the token's merchant scope an afterthought, and gives an integrator no safe recovery path after a secret leak.

## Solution

Adopt OpenIddict as the OAuth 2.0 engine and treat the app client as a credential, not a business aggregate. Each merchant has exactly one app client, identified by an opaque `client_id`; its secret is shown once and can be rotated by proving the current secret. `POST /connect/token`, form-urlencoded and limited to `client_credentials`, issues a one-hour HS256 JWT carrying `sub` (the client id) and `merchant_id` (the owning merchant). OpenIddict persists credentials and tokens in two tables inside the existing `contosopay` schema. App-client use cases stay in Core behind ports, a new `Adapters.Identity` implements those ports, and the merchant must be active both when the app client is created and when a token is issued. A deactivated merchant stops obtaining tokens immediately; a token already issued remains valid until it expires.

## User Stories

1. As an integrator, I want to create the single app client for my merchant with a display name, so that my integration has its own OAuth 2.0 credential.
2. As an integrator, I want the create response to return the opaque client id and the plain secret exactly once, so that I can store the secret securely and never see it again.
3. As an integrator, I want the client id to be an opaque random value rather than my merchant name, so that ids do not leak business information or invite guessing.
4. As an integrator, I want a second creation for the same merchant to fail with conflict, so that the 1:1 rule is enforced and I do not end up with two ambiguous credentials.
5. As an integrator, I want creation to fail with not found when the merchant does not exist, so that I cannot create an orphan credential.
6. As an integrator, I want creation to fail with conflict when the merchant is inactive, so that a deactivated merchant cannot obtain a new credential.
7. As an integrator, I want creation to be public, so that I can bootstrap the first credential right after creating a merchant without a chicken-and-egg token.
8. As an integrator, I want to read my app client by client id, so that I can confirm which merchant it belongs to and its display name.
9. As an integrator, I want the read response to omit the secret, so that a read can never leak credential material.
10. As an integrator, I want the read endpoint to require a Bearer token, so that client ids are not openly enumerable.
11. As an integrator, I want to read only the app client owned by the token's merchant, so that another merchant's client id returns not found and merchant isolation is preserved.
12. As an integrator, I want to rotate the secret by proving the current secret, so that possession of a Bearer token alone cannot rotate the credential.
13. As an integrator, I want rotation to return the new plain secret exactly once and keep the client id unchanged, so that my integration can adopt the new secret without re-registering.
14. As an integrator, I want the old secret to stop authenticating immediately after rotation, so that a leaked secret has no overlap window.
15. As an integrator, I want rotation with a wrong current secret to return unauthorized, so that credential guessing is rejected.
16. As an integrator, I want rotation of an unknown or foreign client to return not found, so that the endpoint does not reveal other merchants' clients.
17. As an integrator, I want to exchange client id and secret at `POST /connect/token` using `application/x-www-form-urlencoded` and `grant_type=client_credentials`, so that standard OAuth 2.0 clients work without a custom JSON contract.
18. As an integrator, I want the token response to carry `access_token`, `token_type=Bearer`, and `expires_in=3600`, so that my client knows how long to cache the token.
19. As an integrator, I want the access token to be a JWT signed with HS256 and valid for one hour, so that I can use it as a Bearer credential without a server-side session.
20. As an integrator, I want the token to carry `sub` equal to the client id and a `merchant_id` claim, so that downstream endpoints can scope operations to my merchant.
21. As an integrator, I want unknown clients, wrong secrets, and inactive merchants to fail with `401 invalid_client`, so that credential failures look the same and do not reveal which part failed.
22. As an integrator, I want an unsupported grant type or a malformed form to fail with an OAuth 400 error, so that client bugs are obvious.
23. As an integrator, I want the token endpoint to reject requests without the required form fields, so that incomplete requests fail fast.
24. As an integrator, I want a missing, invalid, or expired Bearer token to be rejected on protected endpoints, so that credentials are actually enforced.
25. As a platform operator, I want a deactivated merchant to stop obtaining new tokens immediately, so that deactivation has an effect even though existing tokens last until expiry.
26. As a platform operator, I want token storage to be on but token entry validation off, so that tokens are auditable in the database without a database lookup on every request.
27. As a platform operator, I want no app-client deactivation endpoint, so that rotation is the single, explicit way to replace a credential.
28. As a platform operator, I want exactly two tables in the existing schema, so that the identity model stays small and the single SQL objects file remains the source of truth.
29. As a platform operator, I want a token issued before a rotation or a merchant deactivation to remain valid until it expires, so that the token contract stays stateless and predictable.
30. As a developer, I want app clients and tokens persisted by OpenIddict through a dedicated `Adapters.Identity`, so that security-critical hashing and token signing are not reimplemented.
31. As a developer, I want Core to depend on ports rather than OpenIddict types, so that use cases stay testable without the framework.
32. As a developer, I want `ResultType.Unauthorized` and its HTTP mapping, so that unauthorized use-case outcomes use the same envelope as other expected failures.
33. As a developer, I want app-client use-case errors in the standard problem envelope, so that they match merchants, sales, and the rest of the API.
34. As a developer, I want token endpoint errors in OAuth shape, so that standard OAuth clients can parse them.
35. As a developer, I want the app-client and token endpoints in the Scalar reference with runnable examples, so that the slice is manually verifiable.
36. As a student, I want the route exception for `/connect/token` documented, so that I understand why the token route has no version prefix while belonging to version one.
37. As a student, I want the 1:1 merchant-app-client relationship and the rotation rule explained, so that I can reason about isolation and credential lifecycle.

## Implementation Decisions

- **Stack**: OpenIddict `EntityFrameworkCore`, `Server.AspNetCore`, `Validation.AspNetCore`, and `Validation.ServerIntegration` (7.7.x) on the existing .NET 10 stack. Npgsql EF Core provider reused. `UseLocalServer` is used by validation so issued tokens are validated locally from the server's keys.
- **Adapter**: a new `Adapters.Identity` project owns OpenIddict, the `IdentityDbContext`, the persistence entity, the custom application manager/descriptor, and the implementations of the Core ports. Core never references OpenIddict types.
- **Credential shape**: `client_id` is `Guid.NewGuid().ToString("N")` (32 lowercase hex characters), never a readable name. The secret is 32 random bytes encoded as Base64URL (43 characters), generated by the adapter, shown in plain text only in the create and rotate responses, and stored only as the PBKDF2 hash produced by OpenIddict.
- **1:1 relationship**: one merchant has at most one app client. `merchant_id` is a unique column in `app_clients`; the use case checks existence first, and a unique-violation from the database is mapped to conflict as the race safety net. There is no listing and no second credential per merchant.
- **Lifecycle**: the app client has no `is_active` and no `created_at`. There is no deactivation or reactivation endpoint. Rotation replaces the secret, keeps `client_id` immutable, and invalidates the previous secret immediately. The merchant's `is_active` is the only lifecycle gate: it is required at creation and at token issuance.
- **Schema**: exactly two tables in `contosopay`: `app_clients` and `app_client_tokens`. The OpenIddict EF model registers the four entity types the package requires (application, authorization, scope, and token), but only application and token exist in the DDL; authorization and scope are never used by the `client_credentials` flow and have no tables. `app_client_tokens` keeps `authorization_id` as a nullable column without a foreign key because the model includes it in the insert. List-shaped application properties (`permissions`, redirect URIs, and `requirements`) are `TEXT` columns holding serialized JSON. `merchant_id` is a UUID with a unique index and a foreign key to `merchants` declared only in the DDL, not in the EF model. All names are snake_case and `docker/scripts/postgresql/01-objects.sql` remains the single source of truth; no migrations, no volume recreation.
- **OpenIddict server**: only `AllowClientCredentialsFlow()`; `/connect/token`; HS256 signing key supplied by configuration (Base64, at least 32 bytes); access-token encryption disabled; access-token lifetime one hour; audience `contoso-pay-api`; issuer `https://contoso-pay/`; token storage enabled; token entry validation disabled. OpenIddict 7 also requires an asymmetric signing key, so an ephemeral one is registered alongside the symmetric key; the symmetric key remains preferred for access tokens, and ephemeral signing/encryption keys cover only internal artifacts this flow does not issue. Issued tokens are not checked against the database, so deleting a token row does not revoke it.
- **Token exchange**: the endpoint is an unversioned route mapped into the version-one documentation group, accepts only `application/x-www-form-urlencoded`, and returns OAuth JSON. A Core port `IExchangeToken` returns `Result<ClaimsPrincipal>`; the `Adapters.Identity` implementation finds the app client by `client_id`, validates the secret with OpenIddict, loads the merchant through the existing `IMerchantRepository`, requires the merchant to exist and be active, and builds a principal with `sub` = client id and `merchant_id` = owning merchant. Unknown client, wrong secret, and inactive merchant all return unauthorized with the OAuth `invalid_client` error. Malformed requests return `invalid_request`; other grant types return `unsupported_grant_type`.
- **API contracts** (single base, no `/api` prefix):
  - `POST /v1/appclients` public, body `{merchantId, name}`, returns `201 {clientId, clientSecret}`; `400` for invalid body, `404` for an unknown merchant, `409` for an inactive merchant, `409` when the merchant already has an app client.
  - `GET /v1/appclients/{clientId}` requires Bearer, returns `200 {clientId, merchantId, name}` with no secret; `401` without a valid token; `404` for an unknown client or a client owned by another merchant.
  - `POST /v1/appclients/{clientId}/rotate-secret` requires Bearer, body `{currentSecret}`, returns `200 {clientId, clientSecret}`; `401` when the current secret is wrong; `404` for an unknown or foreign client.
  - `POST /connect/token` form-urlencoded with `grant_type=client_credentials`, `client_id`, `client_secret`, returns `200 {access_token, token_type, expires_in}`; `401 invalid_client` for credential failures; `400 invalid_request` or `unsupported_grant_type` for malformed requests.
- **Validation and errors**: request models validated with FluentValidation (merchant id required, name required, trimmed, maximum 100 characters; client id and current secret required). App-client failures use the existing Result-to-ProblemDetails mapper. `ResultType.Unauthorized = 401` is added to the domain result enum and mapped in `EndpointBase`. Missing or invalid Bearer on protected endpoints is rejected by the authentication challenge; the use-case unauthorized on rotation uses the problem envelope.
- **Routing**: app-client endpoints are versioned under `/v1`. The token endpoint is the single route exception: it is grouped with version one for documentation but mapped through a no-prefix group so its route stays `/connect/token`.
- **Seams**: the highest seam is the HTTP endpoint seam backed by the shared Result-to-response mapper; the second seam is the Core use-case seam for unit tests. New ports are `IAppClientCredentials` (create, find, validate secret, rotate; owned by the identity adapter), `IAppClientQueries` (read projection), and `IExchangeToken` (token exchange, under `Core/Abstractions/Identity`). The identity adapter is an implementation seam exercised through HTTP; there are no standalone OpenIddict store tests. The merchant existence/active check reuses `IMerchantRepository`.
- **Documentation**: new ADR for OpenIddict, the 1:1 relationship, and rotation; ADR-0003 is amended to record that the app client no longer has its own active flag; the glossary changes the merchant relationship to 1:1 and gains `AppClient`, `Client secret`, `Rotate secret`, and `Access token`; the old `appclients` and `connect` specs are marked superseded.

## Testing Decisions

- A good test asserts externally observable behavior: status codes, response shapes, one-time secret exposure, absence of secret material on reads, the updated secret authenticating while the old one does not, token claims, token lifetime, merchant isolation, and rejection of missing/invalid/expired tokens. It does not assert OpenIddict internals, PBKDF2 details, or EF entity shapes.
- Modules under test: `CreateAppClient`, `GetAppClient`, and `RotateAppClientSecret` as use-case unit tests with NSubstitute and builders; the HTTP endpoints as integration tests against the real Postgres instance, including the token endpoint and Bearer validation. The identity adapter is covered through the endpoint seam; there are no standalone repository, store, or adapter tests.
- Unit coverage: the validation matrix for create (missing merchant id, blank name, name over 100 characters), merchant not found, inactive merchant, existing app client, success; get success, unknown client, foreign merchant; rotation success, unknown/foreign client, wrong current secret, missing current secret.
- Integration coverage: create `201` with client id and secret, then `GET` returns the same client without a secret; duplicate create `409`; unknown merchant `404`; inactive merchant `409`; get without token `401`; get with another merchant's token `404`; rotate `200`, new secret obtains a token, old secret returns `401`; token `200` with the expected `token_type` and `expires_in`, claims `sub` and `merchant_id` present, Bearer accepted on read; unknown client, wrong secret, and inactive merchant at token issuance return `401 invalid_client`; unsupported grant type returns `400`; expired and tampered tokens are rejected. Per the repo rule, at most one request-validation integration test per endpoint; the matrix stays in unit tests.
- Integration tests seed app clients through the identity adapter/manager when a case needs a credential before the public create endpoint exists, and clean the identity tables between tests. The signing key is deterministic in tests and the clock is controllable so expiry can be tested without sleeping.
- Prior art: the merchants slice and `backend/.specs/docs/automated-tests.md` (xUnit v3, builders, NSubstitute, Given-When-Then names, one parenthesized trait such as `[Trait("AppClient", "CreateAppClient (Integration)")]`). The same `WebApplicationFactory` and real-Postgres setup are reused.

## Out of Scope

- Changing the existing merchant endpoints to require Bearer (option A: merchant endpoints stay as they are; the debt is recorded, not paid here).
- More than one app client per merchant, listing/pagination of clients, app-client deactivation or reactivation, or a client lifecycle history.
- Authorization-code, implicit, hybrid, refresh-token, logout, revocation, introspection, or user-account flows; scopes, roles, or per-endpoint permissions beyond merchant ownership.
- Sales flows, Pix, boleto, recurrence, card tokenization, anti-fraud, 3DS, webhooks, and the payment simulator.
- Key rotation automation, production key management, rate limiting, lockout, audit UI, and secret recovery if both the secret and the current secret are lost.
- Migrating existing app-client data (none exists yet) or changing the `merchants` table.

## Further Notes

- Public creation is an accepted risk: anyone can create the credential for a merchant that does not yet have one, and the merchant then cannot create another (409). If the squatter holds the current secret, the merchant cannot rotate either; recovery needs operator/database intervention. This is acceptable only because the project is a study gateway; a real deployment would need an authenticated bootstrap path. Because the existing merchant read/list endpoints stay public in this initiative (option A), merchant ids are discoverable, which makes the squatting risk easier to exercise; retrofitting Bearer onto those endpoints is the natural mitigation and is recorded as debt.
- A token obtained before a secret rotation or a merchant deactivation remains valid until it expires, because token entry validation is off. This is the deliberate trade-off recorded in the ADR; immediate revocation would need token entry validation or a revocation list.
- The EF model retains four OpenIddict entity types while the database has two tables. Operations that query authorization/scope tables (for example, deleting an app client through the OpenIddict manager) are not supported and must not be added casually.
- The token route is the single versioning exception. It belongs to version one for ownership and documentation but its route carries no version segment.
- Build order: ticket 01 delivers the identity foundation, the two tables, the credential port, and token issuance; ticket 02 delivers the public create, protected read, and rotation endpoints on top. Ticket 01 is testable by seeding an app client through the identity adapter.
- The merchant endpoints remain public in this initiative. The future protected API should reuse the same validation scheme and the `merchant_id` claim.
