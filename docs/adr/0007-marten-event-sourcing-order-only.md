# ADR-0007: Marten event sourcing, Order aggregate only

## Status

Accepted — 2026-08-27 · **Energy gate at end of Faz 4**

## Context

Event sourcing is a stated learning goal. It is also the easiest way to drown a
solo project if applied broadly. The event store choice must not add stateful
infrastructure to an 8 GB shared VPS.

## Decision

- **Marten 9.x** as the event store, running on the PostgreSQL we already operate.
- Scope discipline: **only the Order aggregate** is event-sourced (`OrderPlaced`, `OrderPriceLocked`, `OrderStockReserved`, `OrderPaymentRecorded`, `OrderConfirmed`, `OrderCancelled`). Cart (Redis), saga state (EF) and all other services are state-based.
- **Before Marten**: a 1-week hand-rolled event-store spike in `/labs` (append-only table, `(stream_id, version)` unique index, naive projector) — built, understood, thrown away.
- Publication path — the **Marten Event Relay**: a Marten subscription in the async daemon reads committed events from a checkpoint and publishes integration events via MassTransit. This is outbox-*like*, not an outbox: the broker publish is not atomic with the PG commit → at-least-once → consumers must be idempotent. Payment/Inventory use the standard MassTransit EF outbox — both styles learned side by side.
- Killer demo: admin order-timeline screen rendered from the event stream.

## Alternatives considered

- **EventStoreDB/Kurrent** — purpose-built, but a new stateful container (RAM, backups, ops) on a constrained VPS, for less transferable Postgres knowledge.
- **Hand-rolled store kept forever** — great to learn from, terrible to own; hence build-then-discard.
- **No event sourcing** — remains the explicit fallback (below), not the default.

## Consequences

### Positive

- Zero new infrastructure; `mt_events` is inspectable SQL; `FetchForWriting` gives optimistic concurrency that feeds the race-condition curriculum.

### Negative

- Marten + MassTransit is a less-trodden pairing than Marten + Wolverine; integration friction is budgeted.

## Exit strategy

Two triggers, one path. Trigger A: Marten+MassTransit friction exceeds ~2 weeks.
Trigger B: energy gate at end of Faz 4 fails. Path: ship **state-based Order +
EF Core + standard MassTransit outbox** — the saga (the core lesson) survives
unchanged; only the aggregate's persistence changes.
