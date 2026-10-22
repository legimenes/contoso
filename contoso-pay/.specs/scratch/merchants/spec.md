Status: ready-for-agent

## Problem Statement

As an integrator of a fictitious study payment gateway inspired by Cielo E-commerce, I need a minimal merchant registry so that every payment and credential can be scoped to a merchant, without building a full backoffice, KYC, or user management system.

## Solution

Provide a minimal `Merchants` capability on the single versioned base: public `POST /v1/merchants` to create a merchant, `GET /v1/merchants/{id}` to read it back (protected by Bearer once the connect slice exists), and `GET /v1/merchants` to list merchants with optional filters and pagination. Creation returns only the generated identifier. This is the root of the `Merchant 1-N AppClients` model and the future `merchant_id` claim used for payment isolation.

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
14. As a platform operator, I want to list merchants with pagination, so that I can browse a growing registry page by page.
15. As a platform operator, I want to find merchants by their exact document, so that deterministic lookups do not require remembering identifiers.
16. As a platform operator, I want to search merchants by name prefix without case sensitivity, so that I can find entries without knowing the exact registered casing.
17. As a platform operator, I want to combine document and name filters, so that I can narrow results when names repeat.
18. As a platform operator, I want to list all merchants when no filter is given, so that I can audit the whole registry.
19. As a platform developer, I want page and pageSize to default to 1 and 20 when omitted, so that a naive client gets a predictable first page.
20. As a platform developer, I want pageSize capped and out-of-range pages rejected with a 400 in the standard problem envelope, so that no single call can overload the API.
21. As a platform developer, I want pagination metadata (page, pageSize, totalItems, totalPages) on every listing, so that clients can walk all pages deterministically.
22. As a platform developer, I want listing results ordered by name case-insensitively with a stable tiebreaker, so that records never repeat or vanish across pages.
23. As a platform developer, I want a page beyond the last to return 200 with empty items, so that walking past the end is a normal empty result, not an error.

## Implementation Decisions

- Capability scope: `Merchants` only. `AppClients`, `connect/token`, and `sales` are separate slices and must not be implemented here.
- API contracts (simplified from Cielo, single base, no `/api` prefix):
  - `POST /v1/merchants` with body `{name, document}` returns `201 {id}`.
  - `GET /v1/merchants/{id}` returns `200 {id, name, document}` and requires `Authorization: Bearer <access_token>` (token issuance itself lives in the connect slice; until connect lands, the protection is specified but not verifiable end-to-end).
  - `GET /v1/merchants` returns `200 {page, pageSize, totalItems, totalPages, items: [{id, name, document}]}` with optional combinable query filters `document` and `name`, plus `page` (default 1, minimum 1) and `pageSize` (default 20, range 1–100; out of range → 400 in the standard problem envelope).
  - `POST` endpoints are public. `GET` endpoints require Bearer.
- Domain: a single `Merchant` aggregate with identity, name, document, and creation timestamp. No roles, no address, no status machine in this slice.
- Validation: name and document are required, trimmed, non-empty with a sensible max length; document is unique system-wide. Duplicates map to conflict, validation failures to bad request, unknown id to not found, all via the existing Result pattern.
- Listing semantics: filters are optional and combine (AND) when both are given; with no filters the endpoint lists every merchant paginated. `document` matches exact equality on raw trimmed digits; `name` matches a case-insensitive prefix (starts with) of the trimmed term, with wildcard characters treated literally. Order is case-insensitive by name with id as tiebreaker for stable pagination. A page beyond the last returns 200 with empty items; zero items yields totalPages 0. The pagination envelope is a standardized generic contract in the application layer `Abstractions` folder, composed into the slice-owned response whose item carries id, name, document (no creation timestamp).
- Architecture: follow the repo's Clean + Ports and Adapters + Vertical Slice approach. The use-case layer exposes create, get and list capabilities with request/response plus validator; the persistence contract is a port defined in the application layer with a relational adapter implementation — the read side has its own port (`IMerchantQueries`), split from the aggregate repository per ADR-0002; composition happens at the API composition root. No business rules in the HTTP layer.
- Persistence: relational source of truth gains a `merchants` table with primary key id, name, document (unique), and creation timestamp. The single SQL objects definition is updated in place; no migration framework. No document store usage for merchant data in this slice.
- Seams (proposed and accepted): the primary testing seam is the HTTP endpoint seam backed by the shared Result-to-response mapper; the second seam is the application use-case seam for unit tests. The repository port is an implementation seam only — the round-trip against the real database is verified through endpoint tests, with no standalone repository tests. One new port is introduced for merchant persistence because no persistence seam exists yet. Fewer seams preferred; endpoint seam is the primary verifiable seam.
- Documentation: the new endpoints join the existing versioned API documentation and appear in the Scalar reference grouped by version. No Swagger UI.
- Idempotency: no dedicated idempotency key in this slice; document uniqueness is the only guard against duplicate merchants.

## Testing Decisions

- A good test asserts externally observable behavior (status code plus response shape and persistence effect), not internal implementation details such as private methods or adapter internals.
- Modules under test: the create/get use-case validation and conflict behavior as unit tests, and the endpoint mapping from Result to HTTP status plus problem envelope — including the real-database round-trip — as integration tests. No standalone repository tests; the validation matrix lives in unit tests, with at most one invalidation integration test per endpoint.
- Prior art in the codebase: the implemented merchants create slice under the conventions of `backend/.specs/docs/automated-tests.md` (xUnit, builders, NSubstitute, Given–When–Then naming, single parenthesized trait). This slice is the reference application of those conventions for later slices to copy.
- Listing coverage: page/pageSize bounds live in the validation matrix (unit); the endpoint integration verifies filter semantics (exact document, case-insensitive name prefix, AND combination), default values, envelope metadata and the empty-page behavior against real PostgreSQL with SQL cleanup; no standalone repository tests.

## Out of Scope

- `AppClients` lifecycle (`POST/GET /v1/appclients`, secret hashing, activation).
- OAuth2 `POST /connect/token`, JWT issuance/validation, refresh, scopes beyond merchant ownership.
- `sales` flows (credit authorize/capture/void, debit immediate capture, simulator rules, masked card handling, voided vs refunded, merchant isolation of payments).
- Pix, boleto, recorrencia, tokenizacao, antifraude, 3DS, webhooks, split de bases transacional/consulta.
- Merchant update, delete, roles, UI, backoffice, production hardening.
- Custom sort options, cursor-based pagination, and full-text/approximate name search (only exact document and case-insensitive name prefix are in scope; a name index remains out of scope until it is a measured problem).

## Further Notes

- The read-protection of `GET /v1/merchants/{id}` creates a forward dependency on the connect slice; build order remains merchants first, then app clients, then connect, wiring the auth guard when connect lands.
- Build status: the create and get endpoints are implemented (see `issues/01-create-merchant.md` and `issues/02-get-merchant.md`; the get read is temporarily public by its ticket, with Bearer protection registered as explicit debt for the connect slice). Glossary and ADRs do not exist yet and were skipped silently per the domain guidance.
