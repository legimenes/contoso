Status: ready-for-agent

## Problem Statement

As an owner of app-client credentials for a fictitious study payment gateway, I need to exchange my client id and secret for a short-lived access token so that all other versioned endpoints can authorize me as my merchant without sending the secret on every call.

## Solution

Provide a single OAuth2-style `client_credentials` capability outside the versioned group: `POST /connect/token` with client id and secret in a JSON body returns a signed JWT Bearer token valid for one hour. Every protected endpoint validates the token and scopes access to the token's merchant. No refresh, logout, users, or authorization-code flows in this slice.

## User Stories

1. As an integrator, I want to exchange client id and secret for an access token, so that I can call protected endpoints with a Bearer header.
2. As an integrator, I want the token request to use a JSON body with grant type plus credentials, so that the study flow stays simple and toolable.
3. As an integrator, I want the token response to carry token, type, and lifetime, so that my client knows when to request a new one.
4. As an integrator, I want inactive or unknown credentials rejected, so that disabled integrations stop working immediately.
5. As an integrator, I want wrong secrets rejected without revealing which field was wrong, so that credential guessing is not aided.
6. As a platform operator, I want tokens to expire after one hour, so that leaked tokens have a bounded blast radius.
7. As a platform operator, I want each token bound to exactly one client and one merchant, so that merchant isolation can be enforced downstream.
8. As a developer, I want invalid grant types and missing fields to fail fast with bad request, so that client bugs are obvious.
9. As a developer, I want auth failures in the standard problem envelope, so that connect errors look like every other API error.
10. As a developer, I want the token endpoint documented in the Scalar reference, so that students can discover the non-versioned route exception.
11. As a developer, I want a runnable request example for the token call plus a protected call, so that the slice is manually verifiable.
12. As a student, I want the route exception explained (organization under version one, route without version prefix), so that the single deviation from the versioning pattern is explicit.

## Implementation Decisions

- Capability scope: token issuance plus the shared Bearer validation used by all protected endpoints. Merchant/app-client registration and sales are separate slices.
- API contract (the single route exception, simplified from Cielo header auth to OAuth2 body auth):
  - `POST /connect/token` with body `{grant_type: client_credentials, client_id, client_secret}` returns `200 {access_token, token_type: Bearer, expires_in: 3600}`.
  - Unknown/inactive client or wrong secret returns unauthorized; malformed grant or missing fields returns bad request. All via the existing Result pattern.
  - Organization exception: the capability is grouped with version-one code for ownership but its route carries no version prefix.
- Tokens: symmetric-signed JWT with one-hour lifetime, carrying client id, merchant id, and unique token id claims. Signed by the API's configured key. No refresh tokens, no revocation list, no scopes beyond merchant ownership in this slice.
- Validation: every protected read and all future sales endpoints require `Authorization: Bearer <access_token>`, validate signature plus expiry plus active credential, and expose the merchant and client identity to the handler for ownership checks. Expired or tampered tokens map to unauthorized.
- Credential check: look up the app client by id, require active flag, compare the presented secret against the stored salted hash with a constant-time comparison. No plain-secret storage or logging.
- Architecture: follow the repo's Clean + Ports and Adapters + Vertical Slice approach. The application layer exposes a token-issuance capability plus a token-validation service consumed by the HTTP layer; signing/verification and clock are behind ports so tests can run without network or wall-clock flakiness. No business rules in the HTTP layer.
- Persistence: no new tables. This slice reads the existing merchants and appclients tables only. Token state is stateless; no server-side session storage.
- Seams for testing (accepted strategy carried forward): the highest existing seam is the HTTP endpoint seam backed by the shared Result-to-response mapper; the second seam is the token use-case seam; new ports are introduced only for signing/verification plus clock and secret comparison if not already present. Endpoint seam remains the primary verifiable seam.
- Documentation: the non-versioned token endpoint still appears in the Scalar reference alongside the versioned documents, with its exception called out. No Swagger UI.

## Testing Decisions

- A good test asserts externally observable behavior (token issued for valid credentials, correct lifetime field, Bearer accepted downstream, invalid/expired/forged tokens rejected, no secret leakage in responses or logs), not signing internals or framework plumbing.
- Modules under test: grant validation, credential lookup plus hash verification plus active check, token minting with expected claims and expiry, validation of good/expired/tampered tokens, endpoint mapping to HTTP status plus problem envelope, and the merchant-scoping values exposed to downstream handlers.
- Prior art: smoke endpoints plus shared Result/problem helpers, with merchants and app clients establishing the unit plus integration pattern on xUnit. This slice copies that pattern and adds deterministic signing keys and controllable clocks in tests.

## Out of Scope

- Merchant/app-client registration beyond read-only lookup.
- Refresh tokens, revocation endpoints, logout, activation/deactivation endpoints, listing credentials.
- User accounts, authorization-code/implicit/hybrid grants, external identity providers, per-endpoint scopes or roles.
- Sales flows, simulator rules, card handling, Pix, boleto, recorrencia, antifraude, 3DS, webhooks.
- Key rotation automation, production key management, rate limiting, lockout.

## Further Notes

- Build order remains merchants, then app clients, then connect. Landing connect makes the specified Bearer guards on the earlier get endpoints verifiable end-to-end and unblocks the sales slice.
- Glossary and ADRs are intentionally skipped per explicit user direction. Triage vocabulary file was not found; `ready-for-agent` is applied per the skill instructions.
