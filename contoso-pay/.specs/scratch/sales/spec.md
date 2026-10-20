Status: ready-for-agent

## Problem Statement

As an integrator of a fictitious study payment gateway inspired by Cielo E-commerce, I need simplified credit and debit flows (authorize, capture, void, consult) with realistic state transitions but no real processing, PCI scope, 3DS, or multiple base URLs, so that I can study design, implementation, and system integration on a single base.

## Solution

Provide a unified `sales` capability on the single versioned base with one `Payment` aggregate typed as credit or debit. Credit supports deferred capture via a capture flag; debit is always immediately captured. A deterministic simulator decides approval. All operations are merchant-scoped by the Bearer token, idempotent by merchant order id, and consultable by payment id or merchant order id.

## User Stories

1. As an integrator, I want to create a credit payment with capture true, so that authorization and capture happen in one step as confirmed.
2. As an integrator, I want to create a credit payment with capture false, so that I can capture later after reviewing the order.
3. As an integrator, I want to create a debit payment without a capture option, so that debit stays always immediate per the simplified model.
4. As an integrator, I want denied payments reported explicitly instead of as errors, so that risk refusal is a normal outcome.
5. As an integrator, I want to capture a previously authorized credit totally by omitting amount, so that the common case needs no math.
6. As an integrator, I want to capture a previously authorized credit partially exactly once, so that split fulfillment is possible without double capture.
7. As an integrator, I want partial capture above the authorized amount rejected, so that over-capture is impossible.
8. As an integrator, I want to void an authorized-only credit as voided, so that pre-capture cancellation is distinguishable.
9. As an integrator, I want to void a confirmed credit or debit as refunded with total or partial amounts, so that post-capture reversal is explicit.
10. As an integrator, I want repeated partial voids allowed until the captured total is exhausted, so that incremental refunds work.
11. As an integrator, I want to consult a payment by payment id, so that I have a single source of truth for polling.
12. As an integrator, I want to consult a payment by my merchant order id, so that reconciliation uses my own key.
13. As an integrator, I want duplicate merchant order ids within my merchant rejected as conflict, so that retries do not create double charges.
14. As an integrator, I want another merchant's payment ids and order ids invisible to me, so that isolation holds across all sales reads and writes.
15. As an integrator, I want amounts expressed in integer cents with fixed BRL currency, so that the Cielo-style money handling is preserved simply.
16. As an integrator, I want installments validated only for credit within one to twelve, so that debit misuse fails fast.
17. As a cardholder, I want my full card number and security code to never be stored or returned, so that the study gateway stays out of PCI scope.
18. As a developer, I want deterministic approval (even card suffix approves, odd denies, plus an amount ceiling), so that tests and demos are repeatable with no randomness.
19. As a developer, I want invalid state transitions rejected as unprocessable, so that capture-after-capture or void-after-void cannot corrupt the ledger.
20. As a developer, I want unknown payment ids to return not found, so that clients distinguish missing payments from invalid transitions.
21. As a developer, I want every error in the standard problem envelope, so that sales errors match merchants, app clients, and connect.
22. As a developer, I want the sales endpoints in the Scalar reference with runnable examples, so that students can try the full lifecycle without reading code.

## Implementation Decisions

- Capability scope: credit plus debit only. Pix, boleto, wallets, recorrencia, tokenizacao, antifraude, 3DS, webhooks, and the Cielo transactional/query base split are out of scope.
- API contracts (single base, no `/api` prefix, all protected by Bearer except where noted):
  - `POST /v1/sales` with `{merchantOrderId, amount, currency: BRL, installments, capture, type: credit|debit, card: {number, holder, expiry, cvv, brand}}` returns `201 {paymentId, merchantOrderId, type, amount, status: authorized|confirmed|denied, tid, nsu, authCode, returnCode, returnMessage}`. The capture flag applies to credit only and is ignored for debit. Installments apply to credit only.
  - `PUT /v1/sales/{paymentId}/capture` with optional `{amount}` returns `200 {status: confirmed}`. Credit only, from authorized state, total when omitted, one partial at most below the authorized total.
  - `PUT /v1/sales/{paymentId}/void` with optional `{amount}` returns `200 {status: voided|refunded}`. Void from authorized yields voided; void from confirmed yields refunded with total or partial amounts decremented from the captured total.
  - `GET /v1/sales/{paymentId}` and `GET /v1/sales?merchantOrderId=` return the full payment view including masked card only. No general listing or pagination in this slice.
  - Duplicate merchant order id within the same merchant maps to conflict; invalid transitions map to unprocessable; unknown ids map to not found; validation failures map to bad request; all via the existing Result pattern.
- Domain: a single `Payment` aggregate with type, merchant owner, merchant order id, amount, captured amount, installments, capture flag, simplified status, masked card (brand, holder, last four, expiry), and fake acquirer references (transaction id, sequence number, authorization code) plus return code and message. Status is shared with a type guard that forbids capture for debit.
- Card handling: full number and security code travel only in the create request, are used once by the simulator, and are never persisted, logged, or returned. Responses and reads expose only the masked view.
- Simulator: deterministic rule approved during grilling (even final card digit approves, odd denies, plus a fixed amount ceiling denies). Generates fake acquirer references. No randomness, no external calls, lives in the application layer for testability.
- Money: integer cents, positive only, fixed BRL in this slice. No multi-currency conversion.
- Isolation and idempotency: every write and read is filtered by the merchant id from the validated token; cross-merchant access behaves as not found. Merchant order id is unique per merchant.
- Architecture: Clean + Ports and Adapters + Vertical Slice. The application layer exposes create, capture, void, and consult capabilities with request/response plus validator; persistence and simulator-clock contracts are ports with relational and in-memory adapters; composition happens at the API composition root. No business rules in the HTTP layer.
- Persistence: relational source of truth gains a `payments` table with payment id primary key, merchant foreign key, merchant order id, type, amount, captured amount, installments, capture flag, status, acquirer references, return code/message, and masked card columns plus creation timestamp, with a unique constraint on merchant plus merchant order id. The single SQL objects definition is updated in place. The document store is used only for payment audit logs, never as truth.
- Seams for testing (same accepted strategy): the highest existing seam is the HTTP endpoint seam backed by the shared Result-to-response mapper; the second seam is the use-case seam per operation; new ports are introduced only for payment persistence and the deterministic simulator where absent. Endpoint seam is the primary verifiable seam covering the authorize, capture, void, and consult lifecycle.
- Documentation: sales endpoints join the versioned API documentation and Scalar reference grouped by version, with runnable examples. No Swagger UI.

## Testing Decisions

- A good test asserts externally observable behavior (status codes, status transitions, masked-only card exposure, isolation, idempotency, deterministic approve/deny, partial math, and audit logging), not adapter internals or simulator implementation details.
- Modules under test: create validation per type, simulator approve/deny/ceiling matrix, capture total/partial/over-capture rules, voided versus refunded branching with partial exhaustion, and consult by both keys with cross-merchant invisibility as unit tests; endpoint mapping to HTTP plus problem envelope — including the real-database round-trip with uniqueness enforcement — as integration tests. No standalone repository tests; the validation matrix lives in unit tests, with at most one invalidation integration test per endpoint.
- Prior art: the conventions codified in `backend/.specs/docs/automated-tests.md` and their application in earlier slices (xUnit, builders, NSubstitute, Given–When–Then naming, single parenthesized trait). This slice copies that pattern and adds state-machine transition coverage as the new prior art for lifecycle testing.

## Out of Scope

- Pix, boleto, wallets, QR, carne, recurring, tokenization, installments beyond validation, fraud analysis, 3DS, webhooks/notification posts, currency conversion, facilitators.
- Real acquiring, real card processing, PCI certification, network token vaults, capture-splitting beyond one partial, general payment listing/pagination, merchant backoffice UI, production hardening.

## Further Notes

- This is the last slice of the agreed build order (merchants, app clients, connect, then sales) and becomes verifiable end-to-end only with a Bearer token from connect.
- Glossary and ADRs are intentionally skipped per explicit user direction. Test conventions are documented in `backend/.specs/docs/automated-tests.md`.
