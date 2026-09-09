# Architecture Decision Records

One file per decision, numbered, never rewritten — superseded decisions get a
new ADR that links back. Every ADR has a mandatory **Exit strategy** section
(we use deliberately experimental technology; every bet needs a way out).
Create new ones with the `create-adr` skill.

| # | Decision | Status |
| --- | --- | --- |
| [0001](0001-full-microservices-from-day-one.md) | Full microservices from day one | Accepted |
| [0002](0002-service-map-and-non-goals.md) | Six services; Shipping/Review/Promotion are non-goals | Accepted |
| [0003](0003-postgresql-db-per-service.md) | PostgreSQL, database-per-service | Accepted |
| [0004](0004-grpc-queries-broker-commands-events.md) | gRPC for sync queries; broker for async commands/events | Accepted |
| [0005](0005-masstransit-v8-license-pin.md) | MassTransit pinned to v8.5.x (license) | Accepted |
| [0006](0006-mediatr-v12-pin-and-cqrs-scope.md) | MediatR pinned to 12.5.x; CQRS only in Order/Payment/Inventory | Accepted |
| [0007](0007-marten-event-sourcing-order-only.md) | Marten event sourcing, Order aggregate only | Accepted |
| [0008](0008-elasticsearch-for-catalog-search.md) | Elasticsearch 9.5 for catalog search | Accepted |
| [0009](0009-redis-8-agpl-usage-map.md) | Redis 8 (AGPLv3) and its usage map | Accepted |
| [0010](0010-yarp-gateway.md) | YARP as the API gateway | Accepted |
| [0011](0011-auth-model-jwt-jwks-bff.md) | Auth: RS256 JWT + JWKS, rotating refresh families, BFF-lite | Accepted |
| [0012](0012-dealer-pricing-model.md) | Dealer pricing: price groups + markup chain, server-authoritative | Accepted |
| [0013](0013-frontend-single-nextjs-app.md) | Single Next.js app, self-written UI | Accepted |
| [0014](0014-deployment-topology.md) | Local compose + VPS compose; Kubernetes local-only (k3d) | Accepted |
| [0015](0015-no-aspire-apphost.md) | No .NET Aspire AppHost; standalone dashboard only | Accepted |
| [0016](0016-naming-monorepo-public-repo.md) | `Akiron.*` naming, monorepo, public repository | Accepted |
| [0017](0017-assertion-library-licensing.md) | AwesomeAssertions for test assertions (licence) | Accepted |
| [0018](0018-error-codes-not-translated-messages.md) | API returns error codes; clients translate | Accepted |
