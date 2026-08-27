# ADR-0004: gRPC for sync queries; broker for async commands/events

## Status

Accepted — 2026-08-27

## Context

Six services need both synchronous answers (what does this cost? is it in
stock?) and asynchronous workflows (reserve, pay, notify). Mixing the two
styles ad hoc is how distributed monoliths are born.

## Decision

- **gRPC carries synchronous, read-only queries** needed to complete an in-flight user request: `GetPricingSnapshot` (Order→Catalog), `CheckAvailability` (Order→Inventory, 200 ms deadline, "unknown — proceed" fallback), `GetDealerPermissions` (any→Identity, Redis-cached).
- **RabbitMQ/MassTransit carries everything that changes state**, split explicitly into **commands** (one directed consumer: `ReserveStock`, `ProcessPayment`) and **events** (facts, n consumers: `StockReserved`, `PaymentCompleted`).
- Hard rules: no gRPC inside saga steps; no gRPC chains (A→B→C); every gRPC client wrapped in resilience pipelines (timeout, retry, circuit breaker); the external edge is REST/JSON via YARP only — gRPC is internal.

## Alternatives considered

- **REST for internal sync calls** — works, but gRPC is a deliberate learning goal and gives contract-first + deadlines for free.
- **Everything async** — purist, but turns simple lookups into request/response choreography nobody enjoys debugging.
- **Everything sync** — the distributed-monolith trap; kills the saga lesson.

## Consequences

### Positive

- One sentence answers "which transport?" for every future interaction.
- Command/event distinction stays visible in EVENT_CATALOG.md entries.

### Negative

- Two transports to operate and trace; proto contracts to maintain in `Akiron.Contracts`.

## Exit strategy

If gRPC operational friction outweighs its value, the three query endpoints
degrade gracefully to internal REST — the "sync read-only query" rule and call
map survive unchanged; only the transport swaps.
