Status: ready-for-agent

## Problem Statement

As an integrator of a fictitious study payment gateway inspired by Cielo E-commerce, I need per-application credentials scoped to my merchant so that each storefront or integration can authenticate independently via OAuth2 without sharing a single merchant-wide password.

## Solution

Provide a minimal `AppClients` capability on the single versioned base: public `POST /v1/appclients` to create a credential bound to an existing and active merchant and `GET /v1/appclients/{id}` to read its public data (protected by Bearer). Creation returns only the generated client id and the one-time plain secret. The stored secret is never readable again. These credentials are the `client_id/client_secret` consumed by the later connect slice to issue JWTs.

## User Stories

1. As a platform integrator, I want to create an app client bound to my merchant with a name, so that each integration has its own credential.
2. As a platform integrator, I want the create response to contain only client id and plain secret, so that I can store the secret once and it never leaks again.
3. As a platform integrator, I want the plain secret shown exactly once at creation, so that later reads cannot expose it.
4. As a platform integrator, I want to fetch an app client by id to see its merchant binding, name, and active flag, so that I can audit my credentials.
5. As a platform integrator, I want creation to fail when the merchant does not exist or is inactive, so that orphan credentials cannot be created and a deactivated merchant cannot obtain new credentials.
6. As a platform integrator, I want public creation without a token, so that bootstrapping after merchant creation has no chicken-and-egg problem.
7. As a platform operator, I want the read endpoint to require a valid Bearer token, so that client ids are not openly enumerable.
8. As a platform operator, I want a merchant to read only its own app clients, so that one merchant can never discover another merchant's client ids.
9. As a developer, I want name and merchant reference validated as required, so that invalid registrations fail fast with bad request.
10. As a developer, I want unknown client ids to return not found with the standard problem body, so that clients distinguish not-found from invalid input.
11. As a developer, I want the secret persisted only as a salted hash with an active flag defaulting to active, so that a database leak does not expose usable secrets.
12. As a developer, I want all errors in the standard problem envelope already used by the API, so that merchants, app clients, connect and sales share one error shape.
13. As a developer, I want the endpoints documented in the Scalar reference with version grouping, so that a student can discover and try them without reading code.
14. As a developer, I want a runnable request example for create and get, so that the slice is manually verifiable from the repo.
15. As a student, I want the `Merchant 1-N AppClients` relationship explained, so that the later connect and sales isolation rules make sense.

## Implementation Decisions

- Capability scope: `AppClients` only. Merchant creation, token issuance/validation, and sales are separate slices and must not be implemented here.
- API contracts (single base, no `/api` prefix):
  - `POST /v1/appclients` with body `{merchantId, name}` returns `201 {clientId, clientSecret}`. Public, no token required.
  - `GET /v1/appclients/{id}` returns `200 {clientId, merchantId, name, isActive}` with no secret field. Requires `Authorization: Bearer <access_token>` (token issuance lives in the connect slice; until connect lands, the protection is specified but not verifiable end-to-end).
- Domain: an `AppClient` bound to exactly one `Merchant`, with generated client id, secret hash, display name, active flag, and creation timestamp. No roles, no scopes beyond merchant ownership, no secret rotation in this slice.
- Validation: merchant reference must point to an existing merchant and that merchant must be active; name is required and trimmed with a sensible max length. An unknown client is not found and validation failures are bad request; an unknown or inactive merchant is rejected through the existing Result pattern, with the status for those two cases decided by the slice that implements them.
- Secrets: generate a strong random secret per client, show it in plain text only in the create response, persist only a salted hash. The read path never returns any secret material. Active defaults to true; deactivation has no endpoint in this slice.
- Architecture: follow the repo's Clean + Ports and Adapters + Vertical Slice approach. The application layer exposes create and get capabilities with request/response plus validator; persistence contracts are ports with a relational adapter implementation; composition happens at the API composition root. No business rules in the HTTP layer.
- Persistence: relational source of truth gains an `appclients` table with client id primary key, merchant id foreign key to merchants, name, secret hash, active flag, and creation timestamp. The single SQL objects definition is updated in place; no migration framework. No document store usage for credential data.
- Seams for testing (same seam strategy accepted for merchants): the primary testing seam is the HTTP endpoint seam backed by the shared Result-to-response mapper; the second seam is the application use-case seam for unit tests. The repository port is an implementation seam only — the round-trip against the real database is verified through endpoint tests, with no standalone repository tests. One new port is introduced for app-client persistence plus reuse of the merchant-existence check. Endpoint seam is the primary verifiable seam.
- Documentation: the new endpoints join the existing versioned API documentation and appear in the Scalar reference grouped by version. No Swagger UI.
- Forward dependency: the credentials created here are consumed by the connect slice (`client_credentials` grant). The read-protection of the get endpoint is wired when connect lands.
- Merchant activation (see `.specs/scratch/merchant-lifecycle/spec.md`): the merchant `is_active` state already exists in the merchants table and aggregate, but this slice must require "existing and active merchant" at creation when implemented. Deactivation of a merchant does not cascade to `appclients`: the two active flags are independent, and reactivating a merchant must not resurrect credentials deactivated individually (`ADR-0003`).

## Testing Decisions

- A good test asserts externally observable behavior (status code plus response shape, one-time secret presence only on create, hash-not-plain in storage, and ownership scoping), not internal implementation details.
- Modules under test: create/get validation including unknown-merchant handling, secret hashing and one-time exposure, and active-default behavior as unit tests; endpoint mapping from Result to HTTP status plus problem envelope — including the real-database round-trip for the appclients table — as integration tests. No standalone repository tests; the validation matrix lives in unit tests, with at most one invalidation integration test per endpoint.
- Prior art in the codebase: the implemented merchants slice under `backend/.specs/docs/automated-tests.md` (xUnit, builders, NSubstitute, Given–When–Then naming, single parenthesized trait). This slice copies that pattern and follows those conventions.

## Out of Scope

- Merchant lifecycle beyond the existence check.
- OAuth2 `POST /connect/token`, JWT issuance/validation/claims, refresh, logout, revocation endpoints.
- `sales` flows (credit authorize/capture/void, debit immediate capture, simulator, masked cards, voided vs refunded).
- Pix, boleto, recorrencia, tokenizacao, antifraude, 3DS, webhooks.
- Secret rotation, activation/deactivation endpoints, listing/pagination of clients, roles, UI, production hardening.

## Further Notes

- Build order remains merchants, then app clients, then connect. The get-protection and the token grant are specified here but become verifiable only with connect.
- Glossary (`.specs/GLOSSARY.md`) and ADRs (`.specs/adr/`) are maintained; ADR-0003 records why merchant activation is not cascaded to app clients. Test conventions are documented in `backend/.specs/docs/automated-tests.md`.
