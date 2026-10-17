Status: ready-for-agent

## Problem Statement

As an integrator of a fictitious study payment gateway inspired by Cielo E-commerce, I need a minimal merchant registry so that every payment and credential can be scoped to a merchant, without building a full backoffice, KYC, or user management system.

## Solution

Provide a minimal `Merchants` capability with two endpoints on the single versioned base: public `POST /v1/merchants` to create a merchant and `GET /v1/merchants/{id}` to read it back (protected by Bearer once the connect slice exists). Creation returns only the generated identifier. This is the root of the `Merchant 1-N AppClients` model and the future `merchant_id` claim used for payment isolation.

## User Stories

1. As a platform integrator, I want to create a merchant with name and document, so that I can obtain a merchant identity for the sandbox gateway.
2. As a platform integrator, I want the create response to contain only the merchant id, so that no sensitive or redundant data leaks at creation time.
3. As a platform integrator, I want to fetch a merchant by id, so that I can confirm the registration before creating app clients.
4. As a platform integrator, I want public creation without a token, so that bootstrapping a study environment has no chicken-and-egg auth problem.
5. As a platform operator, I want the read endpoint to require a valid Bearer token, so that merchant data is not openly enumerable.
6. As a platform operator, I want a merchant to never see another merchant's record through id guessing, so that isolation is enforced by the merchant scope.
7. As a developer, I want name and document validated as required and non-empty, so that invalid registrations fail fast with 400.
8. As a developer, I want document uniqueness enforced, so that duplicate merchant registrations are rejected with 409.
9. As a developer, I want unknown ids to return 404 with a standard problem body, so that clients can distinguish not-found from invalid input.
10. As a developer, I want all errors in the standard problem envelope already used by the API, so that merchants, app clients, connect and sales share one error shape.
11. As a developer, I want the endpoints documented in the Scalar reference with version grouping, so that a student can discover and try them without reading code.
12. As a developer, I want a runnable request example for create and get, so that the slice is manually verifiable from the repo.
13. As a student, I want the merchant model explained as the owner of payments and app clients, so that the later auth and sales slices make sense.

## Implementation Decisions

- Capability scope: `Merchants` only. `AppClients`, `connect/token`, and `sales` are separate slices and must not be implemented here.
- API contracts (simplified from Cielo, single base, no `/api` prefix):
  - `POST /v1/merchants` with body `{name, document}` returns `201 {id}`.
  - `GET /v1/merchants/{id}` returns `200 {id, name, document}` and requires `Authorization: Bearer <access_token>` (token issuance itself lives in the connect slice; until connect lands, the protection is specified but not verifiable end-to-end).
  - `POST` endpoints are public. `GET` endpoints require Bearer.
- Domain: a single `Merchant` aggregate with identity, name, document, and creation timestamp. No roles, no address, no status machine in this slice.
- Validation: name and document are required, trimmed, non-empty with a sensible max length; document is unique system-wide. Duplicates map to conflict, validation failures to bad request, unknown id to not found, all via the existing Result pattern.
- Architecture: follow the repo's Clean + Ports and Adapters + Vertical Slice approach. The use-case layer exposes create and get capabilities with request/response plus validator; the persistence contract is a port defined in the application layer with a relational adapter implementation; composition happens at the API composition root. No business rules in the HTTP layer.
- Persistence: relational source of truth gains a `merchants` table with primary key id, name, document (unique), and creation timestamp. The single SQL objects definition is updated in place; no migration framework. No document store usage for merchant data in this slice.
- Seams for testing (proposed and accepted): the highest existing seam is the HTTP endpoint seam backed by the shared Result-to-response mapper; the second seam is the application use-case seam; the third is the repository port seam. One new port is introduced for merchant persistence because no persistence seam exists yet. Fewer seams preferred; endpoint seam is the primary verifiable seam.
- Documentation: the new endpoints join the existing versioned API documentation and appear in the Scalar reference grouped by version. No Swagger UI.
- Idempotency: no dedicated idempotency key in this slice; document uniqueness is the only guard against duplicate merchants.

## Testing Decisions

- A good test asserts externally observable behavior (status code plus response shape and persistence effect), not internal implementation details such as private methods or adapter internals.
- Modules under test: the create/get use-case validation and conflict behavior, the repository round-trip for the merchants table, and the endpoint mapping from Result to HTTP status plus problem envelope.
- Prior art in the codebase: only smoke coverage exists (versioned test endpoints returning OK) and the shared Result and problem-envelope helpers. The test project is otherwise empty with xUnit configured, so this slice establishes the first real unit plus integration pattern for later slices to copy.

## Out of Scope

- `AppClients` lifecycle (`POST/GET /v1/appclients`, secret hashing, activation).
- OAuth2 `POST /connect/token`, JWT issuance/validation, refresh, scopes beyond merchant ownership.
- `sales` flows (credit authorize/capture/void, debit immediate capture, simulator rules, masked card handling, voided vs refunded, merchant isolation of payments).
- Pix, boleto, recorrencia, tokenizacao, antifraude, 3DS, webhooks, split de bases transacional/consulta.
- Merchant update, delete, listing/pagination, roles, UI, backoffice, production hardening.

## Further Notes

- The read-protection of `GET /v1/merchants/{id}` creates a forward dependency on the connect slice; build order remains merchants first, then app clients, then connect, wiring the auth guard when connect lands.
- Triage vocabulary file was not found in the repo; `ready-for-agent` is applied per the skill instructions. Glossary and ADRs do not exist yet and were skipped silently per the domain guidance.
