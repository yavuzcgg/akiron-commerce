# ADR-0001: Full microservices from day one

## Status

Accepted — 2026-08-27 (re-recording a July 2026 decision after repository loss)

## Context

For a commercial product of this size, a modular monolith would be the sane
default. But this project is explicitly a **distributed-systems learning
project**: sagas, outboxes, race conditions, gRPC, per-service data ownership
and independent deployment are the curriculum, not incidental complexity.

## Decision

- Microservices from the first line of code: six services, database-per-service, broker in between.
- The cost is acknowledged and managed structurally: vertical-slice build order (one service must be demo-able before the next exists), no speculative scaffolding, honest roadmap.

## Alternatives considered

- **Modular monolith, split later** — cheaper to ship, but defers exactly the lessons this project exists to teach; "split later" rarely happens on a solo project.
- **Monolith + a single satellite service** — teaches integration once, not the discipline of boundaries.

## Consequences

### Positive

- Every targeted skill (saga, outbox, gRPC, per-service data, observability across processes) is exercised for real.
- Portfolio value: demonstrable distributed system, not claims.

### Negative

- Slower feature throughput; heavier local environment; more ways to stall.

## Exit strategy

If the operational weight kills momentum (two consecutive months without a
demo-able increment), collapse un-started services into the existing ones and
finish as a 2–3 service system. Existing boundaries (separate DBs, contracts)
make the merge mechanical, not architectural.
