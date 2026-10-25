Status: ready-for-agent

## Problem Statement

As a platform operator of a fictitious study payment gateway, I need to deactivate a merchant without deleting it, so that a merchant that should stop operating keeps its history and identifier while being unable to obtain credentials or create payments. Today a merchant has no lifecycle at all: it is created and stays reachable forever, and there is no way to say "this merchant must stop operating" other than deleting the row, which would destroy the identity that future app clients and payments depend on.

## Solution

Add an active/inactive state to `Merchant`, owned by the aggregate, exposed through two explicit transitions: public `POST /v1/merchants/{id}/activate` and `POST /v1/merchants/{id}/deactivate`, both answering `204 No Content` and `404` in the standard problem envelope for unknown ids. Every merchant is created as an active merchant. The state is returned by `GET /v1/merchants/{id}` and by the `GET /v1/merchants` listing, but the listing cannot be filtered by it. The flag carries no effect inside this slice: the enforcement that an inactive merchant cannot authenticate or originate app clients and payments belongs to the connect, app-clients and sales slices, and is recorded as a deliberate forward dependency.

## User Stories

1. As a platform operator, I want to deactivate a merchant, so that a merchant that must stop operating can be blocked without losing its identity.
2. As a platform operator, I want to activate a merchant again, so that a temporary suspension is reversible.
3. As a platform operator, I want every new merchant to start active, so that registration keeps working without an extra step.
4. As a platform operator, I want deactivation to be idempotent, so that a retried call is not reported as an error.
5. As a platform operator, I want activation to be idempotent, so that activating an already active merchant is a success.
6. As a platform operator, I want the activation state visible when I read a merchant, so that I can confirm the transition took effect.
7. As a platform operator, I want the activation state visible in the merchant listing, so that I can audit which merchants are inactive without opening each one.
8. As a platform operator, I want unknown ids rejected with not found, so that I can distinguish a wrong id from a failed transition.
9. As a platform operator, I want the listing not to be filterable by activation state, so that the query contract I already use stays stable.
10. As a platform operator, I want deactivation to keep the merchant's name, document and creation timestamp untouched, so that the registry record remains historically accurate.
11. As a platform integrator, I want a deactivated merchant to stop obtaining access tokens, so that a disabled integration stops working without me rotating secrets.
12. As a platform integrator, I want a token obtained before deactivation to keep working until it expires, so that an in-flight integration is not cut mid-request.
13. As a platform integrator, I want deactivation to leave my app clients untouched, so that reactivating the merchant does not resurrect credentials I deactivated individually.
14. As a platform integrator, I want an inactive merchant to be rejected when creating app clients, so that orphan credentials cannot be created for a merchant that is not allowed to operate.
15. As a platform developer, I want the transitions expressed as named operations, so that activating and deactivating are separate, testable capabilities instead of a generic merchant update.
16. As a platform developer, I want the state to change only through domain methods, so that no use case can assign the flag directly.
17. As a platform developer, I want both transitions to commit through a single explicit write port, so that persistence does not depend on the entity happening to be tracked.
18. As a platform developer, I want `204 No Content` on success, so that the contract does not carry a body whose content is already determined by the route.
19. As a platform developer, I want all errors in the standard problem envelope already used by the API, so that merchants, app clients, connect and sales share one error shape.
20. As a platform developer, I want the new column to fail loudly when a writer forgets it, so that no merchant can end up with an undefined activation state.
21. As a platform developer, I want the existing databases to receive a documented backfill, so that applying the change does not require dropping the volume.
22. As a platform developer, I want the endpoints documented in the Scalar reference with version grouping, so that a student can discover them without reading code.
23. As a platform developer, I want runnable request examples for both transitions, so that the slice is manually verifiable from the repo.
24. As a student, I want the merchant lifecycle explained as a reversible state and not as a status machine, so that the model stays small and the later auth and sales rules make sense.

## Implementation Decisions

- Capability scope: merchant lifecycle only. Authentication enforcement, app-client rules and sales rules are separate slices; this spec defines the state, not its consumers.
- Domain: `Merchant` gains a boolean activation state. It is the first mutable state on the aggregate: every other property stays get-only, and the state exposes a private setter reachable only by the two domain methods.
- The single-constructor rule is preserved: the constructor gains the boolean and the static factory always passes active. No second constructor, no parameterless constructor, no factory overload that creates an inactive merchant.
- Transitions: two parameterless domain methods, one for each direction, with no guard and no exception. Repeating a transition is a no-op that succeeds; the entity never rejects a valid target state because it is already in it.
- Naming: the canonical term is **Deactivate**, never "inactivate" or "disable". The glossary carries `Active merchant` and `Deactivate`, with the rejected synonyms listed.
- Application: two vertical slices under the merchants feature, one per transition, each owning its request record and its interface. Neither owns a response: the success contract has no body.
- Requests carry only the merchant identifier, which arrives from the route as a bound GUID; therefore no FluentValidation validator is introduced, following the precedent set by the read slice.
- Use-case flow for both slices: read the aggregate through the repository; a missing aggregate returns not found with the existing merchant-not-found message; otherwise invoke the domain method, commit through the write port, and return success with no content.
- Every failure and success is expressed through the existing Result pattern. The result type enumeration gains a no-content value.
- HTTP contract (single versioned base, no `/api` prefix):
  - `POST /v1/merchants/{id}/activate` returns `204` with no body, or `404` in the standard problem envelope for an unknown id.
  - `POST /v1/merchants/{id}/deactivate` returns `204` with no body, or `404` in the standard problem envelope for an unknown id.
  - Both are public, like the existing create endpoint, with the same explicit debt: Bearer protection is wired when the connect slice lands, and the contract does not change then.
- Rationale for POST over PATCH: one route maps to one slice and one use case. A single `PATCH /v1/merchants/{id}` with an activation field would either dispatch inside the endpoint, which puts a business decision in the HTTP layer, or create a generic merchant-update slice that the merchants spec explicitly excludes. Named transition sub-resources keep the one-route-one-slice rule intact.
- Rationale for 204 over 200: the response body would be fully determined by the route (activation always yields active, deactivation always yields inactive), so it would carry no information. The cost is accepting a shared-infrastructure change: the result type gains no-content and the endpoint base gains a success-without-value mapping, since today every success in the API carries a body and an unmapped result type silently degrades to an internal error.
- Read side: the by-id response and the listing item both gain the activation state. The listing request is unchanged and gains no filter, so the query-string contract of `GET /v1/merchants` stays stable.
- Persistence: the merchants table gains a required boolean column named `is_active`, with no database default. The value is always written by the application: a writer that forgets it fails on the not-null constraint instead of silently receiving `TRUE`. The entity mapping is required and must not declare a database default, because a declared default would make the provider omit the column when the value is false.
- Existing databases are updated by a manual, ordered delta (add nullable, backfill with true, then set not null); the single SQL objects file remains the source of truth and no volume is dropped.
- Write port: the merchant repository gains an explicit update operation that attaches/updates the aggregate and commits. No unit of work is introduced; the repository keeps owning the commit boundary, as decided in the repository/queries split.
- Concurrency: last-write-wins. No concurrency token and no row lock are introduced. Concurrent opposite transitions are a race whose winner is undefined by nature, so a conflict response would only transfer the problem to the client.
- No cascade: deactivating a merchant does not change its app clients. The two flags are independent, and a physical cascade would erase the difference between "deactivated because the merchant was deactivated" and "deactivated individually".
- Forward dependencies, recorded in a decision record rather than implemented here: token issuance requires an active merchant; app-client creation and sales require an existing and active merchant. The check is evaluated at token issuance only, accepting that an already issued token remains valid for up to one hour after deactivation.
- Documentation: both endpoints join the existing versioned API documentation and appear in the Scalar reference grouped by version. No Swagger UI. The glossary and the decision record are updated as part of this slice.
- No new library, package or migration framework is introduced.

## Testing Decisions

- A good test asserts externally observable behavior: the HTTP status and the absence of a body, the persisted state readable through the existing read endpoints, and the calls the use case makes to its ports. It does not assert private methods, entity internals or provider-generated SQL.
- Seams: the primary seam is the existing HTTP endpoint seam backed by the shared Result-to-response mapper, exercised against the real PostgreSQL instance. The second seam is the existing use-case seam, exercised with the real domain aggregate and substituted ports. No new seam is introduced for this feature, and no standalone repository test is written; the database round-trip is proven through the endpoint seam, following the merchants precedent.
- Modules under test: the aggregate (creation starts active, each transition, repeated transition), the two use cases (success path commits once, unknown id does not commit), the endpoint mapping (204 without body, repeated call still 204, 404 in the problem envelope), and the read contracts (activation state present and accurate in both by-id and listing responses).
- Prior art: the implemented merchants slices and the conventions in the automated-tests documentation (xUnit, builders, NSubstitute, Given-When-Then naming, a single parenthesized trait). The domain builder gains a way to produce an inactive merchant through the domain method, not through a constructor shortcut.
- Integration coverage is limited to one invalidation test per endpoint; the full transition matrix lives in the unit and domain tests.

## Out of Scope

- Authentication implementation: token issuance, token validation, JWT keys, bearer guards and the per-request merchant check. This slice only records what the connect slice must enforce.
- App-client behavior: creating, reading, filtering or cascading activation state to app clients, and the connection between merchant state and app-client state.
- Sales behavior: blocking, rejecting or annotating payments of an inactive merchant.
- Audit columns: no update timestamp, no deactivation timestamp, no reason, no actor, no audit log.
- Listing filters by activation state, sorting changes, pagination changes and any change to the query-string contract of `GET /v1/merchants`.
- Optimistic concurrency, row locks, conflict responses for concurrent transitions, and idempotency keys.
- Merchant update and delete, renaming, document change, roles, permissions, backoffice, UI and production hardening.
- Token revocation, forced logout and immediate cut-off of already issued tokens.
- Status machines beyond the single boolean: no pending, suspended, blocked or expired states.

## Further Notes

- Decision record: `.specs/adr/0003-estado-ativo-do-merchant-barra-credenciais-e-pagamentos.md` records why the gate lives at token issuance, why there is no cascade, and what a future immediate-revocation requirement would cost.
- Spec amendments required by this slice: connect gains "active merchant at issuance"; app clients gains "existing and active merchant" at creation; the merchants spec gains a pointer to this lifecycle so the two do not contradict each other.
- The no-content contract is the first success-without-body in the API and the first slice without a response record. Reviewers should expect that gap and not read it as an omission.
- Build order: this slice lands after the merchants read/write slices and before app clients and connect, so that the forward dependency can be wired as soon as those slices exist.
- Backfill is a manual, documented delta; fresh environments get the column from the single SQL objects file.
