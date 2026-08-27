# System Architecture

Deep reference. The two-minute map lives in [/ARCHITECTURE.md](../ARCHITECTURE.md);
decisions and their rationale live in [adr/](adr/). This document describes *how
the pieces work together*.

## 1. Services

Exactly six services. Shipping, Review, Promotion are explicit non-goals (see ROADMAP).

| Service | Data | CQRS | Notes |
| --- | --- | --- | --- |
| Identity | PG `akiron_identity` | no | Users, dealers, sub-dealer tree, price-group assignment, permission flags. Issues JWTs (RS256/ES256) + JWKS endpoint; rotating refresh-token families. |
| Catalog | PG `akiron_catalog` + ES index | no | Products, categories, price lists per price group, markup-chain resolution. Owns the Elasticsearch index and the indexer module. |
| Order (+Cart) | PG `akiron_order` (Marten) + Redis cart | yes | Cart is state-based in Redis; the Order aggregate is event-sourced; the checkout saga lives here. |
| Payment | PG `akiron_payment` | yes | `IPaymentProvider` abstraction, PayTR first. Webhook endpoint; idempotency lab. |
| Inventory | PG `akiron_inventory` | yes | Stock and reservations; the stock race-condition lab. |
| Notification | PG `akiron_notification` | no | Thin: consumers + e-mail templates. Built last; at most 2 projects. |

## 2. Communication rules

**gRPC asks synchronous questions; the broker carries asynchronous commands and events.**

- **Query** (gRPC, read-only, in the request path): `GetPricingSnapshot`, `CheckAvailability`, `GetDealerPermissions`.
- **Command** (RabbitMQ, one consumer, directed): `ReserveStock`, `ProcessPayment`.
- **Event** (RabbitMQ, fact, n consumers): `StockReserved`, `PaymentCompleted`, `ProductUpserted`.

gRPC call map (internal only — never exposed through YARP):

| Caller → Callee | RPC | Why |
| --- | --- | --- |
| Order → Catalog | `GetPricingSnapshot(items, priceGroupId)` | Checkout prices are server-authoritative; JWT pricing claims are never trusted. |
| Order → Inventory | `CheckAvailability(sku[], qty[])` | Availability *hint* for cart/checkout preview. Deadline 200 ms; fallback "unknown — proceed, saga will verify". |
| Any → Identity | `GetDealerPermissions(dealerId)` | Permissions are checked server-side; Redis cache (TTL 5 min) invalidated by `DealerPermissionsChanged`. |

Rules: resilience pipelines (timeout, retry, circuit breaker) on every gRPC client ·
no gRPC inside saga steps · no gRPC chains (A→B→C) · external edge is REST/JSON via YARP only.

## 3. Search (Elasticsearch)

PostgreSQL is the system of record; **Elasticsearch is a disposable projection**.

- Catalog publishes `ProductUpserted` / `ProductDeleted` / `PriceListChanged`.
- An **Indexer module inside Catalog** (MassTransit consumer) writes to ES — deliberately event-driven even within one service; it proves the reindex path.
- `GET /catalog/search`: Turkish analyzer, full-text, facets (category/brand/attributes), price-group-aware prices.
- A `reindex` command rebuilds the index from PostgreSQL (recovery story + demo).
- ES heap is capped at 512 MB everywhere (local and VPS).

## 4. Checkout saga (MassTransit state machine, hosted in Order)

```text
OrderSubmitted
  → cmd ReserveStock (Inventory)     — fail → OrderRejected
  → evt StockReserved
  → cmd ProcessPayment (Payment)     — fail → cmd ReleaseStock → OrderCancelled
  → evt PaymentCompleted
  → OrderConfirmed → evt OrderConfirmedEvent → Notification sends e-mail
```

Saga state is persisted via the **MassTransit EF Core saga repository** (dedicated
tables in `akiron_order`). Clean line: Marten = aggregate, EF = saga/infrastructure state.

## 5. Event sourcing (Order aggregate only)

- **Marten 9.x** on the same PostgreSQL. Stream: `OrderPlaced, OrderPriceLocked, OrderStockReserved, OrderPaymentRecorded, OrderConfirmed, OrderCancelled`.
- Write path: command handler → `session.Events.Append(...)` → single PG transaction (events *are* the state — atomicity on this side is free).
- **Marten Event Relay** (outbox-like, not an outbox): a Marten subscription in the async daemon reads committed events in order from a checkpoint and publishes integration events via MassTransit. The RabbitMQ publish is **not** atomic with the PG commit → delivery is at-least-once → **consumers must be idempotent** (MassTransit inbox / messageId dedupe).
- Other services (Payment, Inventory) use the standard **MassTransit EF Core transactional outbox/inbox** — two outbox styles, learned side by side.
- Not event-sourced, by decision: Cart, saga state, every other service. Exit ramp: ADR-0007.

## 6. Identity, auth & dealer pricing

- Access JWT (15 min) signed RS256/ES256; JWKS endpoint; services validate locally — no per-request Identity call.
- Rotating refresh-token families with reuse detection (pattern proven in the owner's `akiron-seo`).
- **BFF-lite**: HttpOnly/Secure/SameSite=Lax cookies on the app domain; the Next.js server holds tokens and attaches `Authorization: Bearer` to gateway calls. Tokens never reach browser JS.
- Dealer model: price groups + sub-dealer markup chain. Pricing inputs ride in JWT claims for display, but **every authoritative price is recomputed server-side by Catalog**; fine-grained permission flags are checked server-side via Identity.

## 7. Frontend

Single Next.js 16 App Router app; route groups `(storefront)`, `(dealer)`, `(admin)`, `(auth)`.

- SEO-critical storefront pages: RSC/server fetch to the gateway. Client interactivity: TanStack Query 5 with optimistic updates.
- Cart is server-authoritative (Redis via Order); Zustand only for ephemeral UI state. Anonymous cart = HttpOnly `cart-id` cookie, merged on login.
- Tailwind 4 `@theme` tokens + shadcn/ui, self-written (no purchased themes); design system documented in `frontend/DESIGN_SYSTEM.md` (born in Faz 2).
- i18n with next-intl from day one — TR default, EN filled lazily.
- Admin ships read-only first (order timeline, product list); mutations later.

## 8. Observability

- OpenTelemetry (traces + metrics + logs over OTLP) wired from the **first vertical slice**, not retrofitted.
- Local UIs: **Jaeger v2** (traces) and the standalone **Aspire dashboard** (logs + metrics + traces in one pane). Serilog console JSON + OTLP exporter; errors as RFC 7807.
- Prometheus + Grafana deliberately deferred to the Kubernetes phase.

## 9. Topology

- **Local (daily loop):** `deploy/compose/docker-compose.infra.yml` runs PostgreSQL 16, Redis 8, RabbitMQ 4.1, Elasticsearch 9.5, Jaeger 2, Aspire dashboard, Mailpit — all with pinned versions. Services run from the IDE against this infra.
- **Kubernetes is a local learning environment** (k3d — real k3s in Docker), phase 8. It is not the deployment target.
- **Deployment target is docker-compose on the owner's VPS** behind a shared Caddy (subdomain routing, no published host ports). Resource caps documented in ADR-0014. Live demo goes up in Faz 7.

## 10. Performance

First-class goal: every phase demo carries a k6/API p95 target; frontend phases
carry Core Web Vitals budgets; caching (Redis, ES, HTTP) is part of each phase —
not an afterthought.
