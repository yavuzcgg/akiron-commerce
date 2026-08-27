# ADR-0006: MediatR pinned to 12.5.x; CQRS only in Order/Payment/Inventory

## Status

Accepted — 2026-08-27

## Context

MediatR v13+ moved to commercial licensing; 12.5.0 is the last Apache release.
Separately: CQRS everywhere is ceremony; CQRS nowhere wastes the services where
write/read models genuinely diverge.

## Decision

- Pin **MediatR 12.5.0** in `Directory.Packages.props` (license pin).
- **CQRS/MediatR only in Order, Payment, Inventory** — the services with real command/query asymmetry, pipeline-behavior needs (validation, idempotency) and saga interplay.
- Identity, Catalog, Notification use plain endpoint-module + handler slices (the owner's proven `akiron-seo` convention) — no mediator.

## Alternatives considered

- **MediatR everywhere** — uniform, but adds indirection exactly where a direct handler is clearer.
- **Hand-rolled dispatcher** — trivial to write, but 12.5.0 is free, familiar, and pipeline behaviors are the learning surface (validation, logging, idempotency behaviors).
- **Wolverine as mediator** — couples the mediator bet to the messaging bet; keep them separable.

## Consequences

### Positive

- License risk contained; ceremony only where it pays for itself; two styles side by side is itself instructive.

### Negative

- Frozen major: no fixes coming; two in-repo idioms to keep consistent (documented in service AGENTS.md files).

## Exit strategy

MediatR's surface here is `IRequest`/`IRequestHandler`/behaviors. If 12.x ever
becomes untenable, a ~200-line in-repo dispatcher with the same signatures
replaces it without touching handler code.
