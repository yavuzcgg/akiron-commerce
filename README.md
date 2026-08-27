# AkironCommerce

**Learning-first B2B/B2C e-commerce platform on .NET 10 microservices — built slowly, in the open, with honest status tables.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1)](https://www.postgresql.org/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-4.1-FF6600)](https://www.rabbitmq.com/)
[![Elasticsearch](https://img.shields.io/badge/Elasticsearch-9.5-005571)](https://www.elastic.co/)
[![CI](https://github.com/yavuzcgg/akiron-commerce/actions/workflows/ci.yml/badge.svg)](https://github.com/yavuzcgg/akiron-commerce/actions/workflows/ci.yml)
[![Status](https://img.shields.io/badge/status-Faz%200%20·%20bootstrap-orange)](docs/ROADMAP.md)

> ### ⚠️ Early days — there is no product to run yet
>
> This repository is in **Faz 0 (bootstrap)**: architecture, decision records,
> governance and infrastructure are in place; the first vertical slice
> (Catalog) starts next. The roadmap below is honest about what exists.

## What this is

A full commerce platform (storefront + dealer portal + admin) built as a
**deliberate distributed-systems curriculum**: saga orchestration with
compensation, transactional outbox (two styles), event sourcing on one
aggregate, gRPC with deadlines and fallbacks, faceted search as a disposable
projection, race-condition labs measured with k6, OpenTelemetry from the first
slice — and a B2B dealer model with server-authoritative pricing.

The build order is **vertical-slice-first**: a service must be end-to-end
demonstrable (endpoint + migration + integration test + Dockerfile + green CI)
before the next service directory may exist. Every phase ends with something
you can click or measure.

## Architecture at a glance

See [ARCHITECTURE.md](ARCHITECTURE.md) for the map, and
[docs/SYSTEM_ARCHITECTURE.md](docs/SYSTEM_ARCHITECTURE.md) for how the pieces
work together. Six services (Identity, Catalog, Order+Cart, Payment, Inventory,
Notification) — PostgreSQL database-per-service, RabbitMQ + MassTransit
between them, gRPC for synchronous read-only queries, YARP at the edge,
Next.js 16 in front.

Every significant decision is an ADR with a mandatory **exit strategy**:
[docs/adr/](docs/adr/).

## Tech stack

| Layer | Choice |
| --- | --- |
| Runtime | .NET 10 · C# 14 · Minimal APIs (endpoint modules) · Clean Architecture |
| Data | PostgreSQL 16 · EF Core 10 · Marten 9 (Order aggregate only) · Redis 8 |
| Messaging | RabbitMQ 4.1 · MassTransit 8.5 (saga, outbox/inbox) — [license pin](docs/adr/0005-masstransit-v8-license-pin.md) |
| Sync RPC | gRPC (internal, read-only queries) |
| Search | Elasticsearch 9.5 (Turkish analyzer, facets) — disposable projection |
| Edge | YARP |
| Observability | OpenTelemetry → Jaeger 2 + Aspire dashboard · Serilog · RFC 7807 |
| Frontend | Next.js 16 · React 19 · TypeScript strict · Tailwind 4 + shadcn/ui · TanStack Query 5 · next-intl |
| Testing | xUnit v3 · Testcontainers · Respawn · k6 (race-condition labs) |
| Delivery | Docker Compose (prod target) · k3d (Kubernetes learning, local) · GitHub Actions |

## Getting started (infrastructure only, for now)

```bash
git clone https://github.com/yavuzcgg/akiron-commerce.git
cd akiron-commerce
docker compose -f deploy/compose/docker-compose.infra.yml up -d --wait
```

That gives you PostgreSQL (6 databases), Redis, RabbitMQ (UI :15672),
Elasticsearch (:9200), Jaeger (:16686), Aspire dashboard (:18888) and Mailpit
(:8025) — all pinned, all healthchecked.

## Roadmap & status

Full detail with dates and cut-order: [docs/ROADMAP.md](docs/ROADMAP.md).

| Phase | Content | Status |
| --- | --- | --- |
| 0 | Bootstrap: governance, ADRs, infra compose, CI | 🔨 in progress |
| 1 | Catalog vertical slice (+ OTel from slice one) | ⏳ planned |
| 2 | Identity + YARP + storefront shell + first gRPC | ⏳ planned |
| 3 | Search: MassTransit → ES indexer, faceted UI | ⏳ planned |
| 4 | Cart + Inventory + race labs (k6-proven) | ⏳ planned |
| 5 | Saga + event sourcing + PayTR + Notification | ⏳ planned |
| 6 | Frontend deepening (dealer portal, admin) | ⏳ planned |
| 7 | Live deploy — `akiron.yavuzcelik.com` | ⏳ planned |
| 8 | Kubernetes (k3d, local) | ⏳ planned |

Non-goals for v1: Shipping/Review/Promotion services, multi-tenancy claims,
marketplace integrations, mobile app.

## How this repo governs itself

[`AGENTS.md`](AGENTS.md) is the constitution — hard invariants (no
cross-service DB access, no speculative scaffolding, feature-first folders),
protected decisions, and a learning mode that forbids hiding mechanics behind
abstractions. AI assistants working in this repo are bound by it; so is the
owner.

## License

Not yet licensed — all rights reserved while the license choice is pending
(tracked in the backlog).
