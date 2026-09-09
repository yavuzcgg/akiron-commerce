# Roadmap

Vertical-slice-first: a service must be end-to-end demonstrable before the next
one is started. Assumes 6–10 focused hours/week, solo. Dates are honest
estimates, not promises — December 2026 realistically means "end of Faz 3",
which is already a strong portfolio artifact on its own.

## Phases

### Faz 0 — Bootstrap (Aug 2026, done 2026-08-27) — COMPLETE

- [x] `git init` + public GitHub repo + first push on day one
- [x] Governance core: `AGENTS.md`, `CLAUDE.md`, `ARCHITECTURE.md`, `docs/` (SYSTEM_ARCHITECTURE, ROADMAP, DATA_OWNERSHIP), ADR batch, devlog, 2 skills
- [x] Build foundation: `.sln` + CPM (`Directory.Packages.props` license pins) + `Directory.Build.props` (warnings-as-errors)
- [x] Infra compose (PG 16 / Redis 8 / RabbitMQ 4.1 / ES 9.5 / Jaeger 2 / Aspire dashboard / Mailpit), all pinned, healthchecked
- [x] CI green on the empty solution (build + compose validate)
- [x] All infra containers healthy via `docker compose up`

### Faz 1 — Catalog slice (→ mid-Oct 2026, 4–5 wk) — IN PROGRESS

Catalog end-to-end: domain (price groups + markup chain), EF Core + first
migration, minimal-API endpoint modules, Testcontainers integration tests,
Dockerfile, a CI test job (the path-filtered reusable workflow is deferred to Faz 2,
when a second service makes it more than premature abstraction), **OTel traces
visible in Jaeger from this slice**.
Governance layer-1 files born here: `Catalog/AGENTS.md`, `.claude/rules/dotnet.md`
+ `testing.md`, `DOMAIN_GLOSSARY.md`, `API_CONVENTIONS.md`, first feature plan in
`docs/plans/active/`, `runbooks/local-development.md`.
**Demo:** product/category API answering, traces in Jaeger, tests green in CI.

### Faz 2 — Identity + Gateway + Frontend shell (→ end of Nov 2026, 5–6 wk)

JWT + JWKS + rotating refresh families, dealer/price-group claims, YARP, Next.js
app shell with storefront listing, BFF-lite auth, **first gRPC call**
(`GetDealerPermissions`) + Redis permission cache.
**Demo:** login → storefront showing dealer-specific prices.

### Faz 3 — Search (→ Dec 2026, 4 wk)

MassTransit enters: `ProductUpserted` events → ES indexer, faceted search API +
UI, Turkish analyzer, `reindex` command. `EVENT_CATALOG.md` is born.
**Demo:** faceted search page — the honest "December 2026" milestone.
Optional: early live preview on the VPS (decided then).

### Faz 4 — Cart + Inventory + gRPC + race labs 1 & 3 (Jan–Feb 2027, 6–8 wk)

Redis cart + cart-concurrency lab; Inventory service + stock-reservation lab
(naive → oversell demonstrated → optimistic `xmin` → `FOR UPDATE` → Redis lock,
all measured with k6); `CheckAvailability` + `GetPricingSnapshot` with resilience
pipelines. **Decision gate:** event sourcing energy check (see Cut order).
**Demo:** add-to-cart → checkout preview with live stock; k6 report proving no oversell.

### Faz 5 — Saga + Event Sourcing + Payment + Notification (Mar–May 2027, 8–10 wk)

Reopen MassTransit license ADR (v8 EOL will have passed). 1-week hand-rolled
event-store spike in `/labs`, then Marten Order aggregate + Event Relay; saga
with compensation; PayTR sandbox + idempotency lab 2; Notification (thin) + Mailpit.
**Demo:** full checkout (pay → confirm → e-mail); admin order timeline rendered
from the event stream; kill-a-service-mid-saga demo.

### Faz 6 — Frontend deepening (May–Jun 2027, 6–8 wk, partly overlapping)

Dealer portal (sub-dealers, markup management), checkout polish, admin
mutations, EN locale fill.
**Demo:** the "real commerce site".

### Faz 7 — VPS deploy + hardening (Jul 2027, 3–4 wk)

VLM-off runbook, capped compose joining the shared Caddy network, weekly
prune cron, k6 against the VPS, security pass.
**Demo:** public live demo at `akiron.yavuzcelik.com`.

### Faz 8 — Kubernetes, local k3d (Aug–Sep 2027, 4–6 wk)

Manifests/kustomize, probes, resource limits, HPA demo, kube-prometheus-stack.
**Demo:** cluster demo + write-up for the portfolio.

## Non-goals (v1)

Shipping, Review, Promotion services · multi-tenancy claims · marketplace
integrations · mobile app · cloud (Azure/AWS — architecture stays 12-factor so
the door is open; 2027+ backlog).

## Cut order (if life happens)

1. Kubernetes → 2028, no guilt
2. Prometheus/Grafana (tied to k8s anyway)
3. Admin mutation breadth (read-only admin stays)
4. EN locale
5. Event sourcing — gate at end of Faz 4: if energy is low, ship state-based
   Order + EF outbox; the saga stays (the saga is the core lesson).

gRPC and Elasticsearch are never cut — cheapest CV value among the additions.
