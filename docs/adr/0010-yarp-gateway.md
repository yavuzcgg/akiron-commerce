# ADR-0010: YARP as the API gateway

## Status

Accepted — 2026-08-27 (resolving a question left open in earlier planning: "YARP or Ocelot")

## Context

The frontend needs one origin; services must not be exposed individually; the
edge needs routing, auth passthrough and rate limiting. The previous project
attempt left "YARP veya Ocelot" undecided across three documents — an unresolved
decision that blocked progress.

## Decision

- **YARP 2.x** hosted in `Akiron.Gateway`.
- Responsibilities: routing to services, JWT validation passthrough, rate limiting, CORS — **no business logic, no aggregation** (aggregation belongs to the BFF layer in Next.js or the owning service).
- External edge is REST/JSON only; gRPC never crosses the gateway (ADR-0004).

## Alternatives considered

- **Ocelot** — community-maintained, aging; YARP is Microsoft-backed and the current industry default in .NET.
- **Nginx/Caddy as gateway** — fine as reverse proxies, but the learning value (middleware, rate limiting, auth integration in .NET) lives in an in-process gateway.
- **No gateway (BFF calls services directly)** — fewer hops, but loses the single policy point and the YARP learning goal.

## Consequences

### Positive

- One place for cross-cutting edge policy; hot-reloadable config; C# extensibility.

### Negative

- One more always-on process (~100–150 MB); one more hop to trace (which is itself an OTel lesson).

## Exit strategy

The gateway is policy + config, not logic. If YARP disappoints, its route table
maps 1:1 onto Caddy/nginx config; only rate-limit policies would need rehoming.
