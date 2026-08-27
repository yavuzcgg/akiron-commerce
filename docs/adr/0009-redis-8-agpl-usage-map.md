# ADR-0009: Redis 8 (AGPLv3) and its usage map

## Status

Accepted — 2026-08-27

## Context

Redis serves several distinct jobs here; unscoped "Redis for everything" is how
caches quietly become unofficial databases. Redis 8 is tri-licensed
(AGPLv3 / SSPLv1 / RSALv2) and the choice should be conscious.

## Decision

- **Redis 8.x**, consciously under **AGPLv3** (we use it unmodified over the network; AGPL obligations don't reach our application code).
- Usage map (exhaustive — anything else needs this ADR updated):
  1. **Catalog cache** (cache-aside, TTL);
  2. **Dealer-permission cache** (TTL 5 min + event invalidation);
  3. **Distributed-lock lab** (`SET NX PX`, compared against DB locking under k6);
  4. **Idempotency keys** (Payment; `SETNX` + TTL, PG unique index as the durable backstop);
  5. **Cart storage** — the single case where Redis is authoritative business state (accepted: carts are ephemeral by nature; AOF on).
- Redis is otherwise **never** authoritative persistent business storage.

## Alternatives considered

- **Valkey** — the BSD fork, drop-in; fine plan B, smaller mindshare on the CV.
- **In-memory + PG for locks/idempotency** — fewer moving parts, but loses the distributed-cache/lock curriculum.

## Consequences

### Positive

- Each usage is a separate, explainable lesson; the "cache became a database" failure mode is fenced off in writing.

### Negative

- Cart durability is bounded by Redis persistence (AOF everysec) — a crash can lose ~1 s of cart mutations (accepted).

## Exit strategy

Valkey is protocol-compatible: an image swap. If cart-in-Redis proves wrong,
carts move to `akiron_order` PG tables behind the same cart API.
