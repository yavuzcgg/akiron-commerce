# Architecture Map

Two-minute overview. The full story lives in [docs/SYSTEM_ARCHITECTURE.md](docs/SYSTEM_ARCHITECTURE.md).

```text
                      Browser
                         │
                         ▼
                 Next.js 16 (BFF-lite)
                         │
                         ▼
                   YARP Gateway
                         │
    ┌──────────┬─────────┼──────────────┐
    ▼          ▼         ▼              ▼
 Identity   Catalog ── Elasticsearch   Order ──── Redis (cart)
    ▲          ▲                        │  (Marten event store)
    │ gRPC     │ gRPC                   │
    └──────────┴────────────────────────┤ Saga (MassTransit)
                                        ▼
                                    RabbitMQ
                                   ↙        ↘
                             Inventory     Payment ── PayTR
                                   ↘        ↙
                                 Notification ── SMTP
```

Each service owns its own PostgreSQL database. One PostgreSQL container, one
database per service. gRPC carries synchronous read-only queries; RabbitMQ
carries asynchronous commands and events.

## Service ownership

| Service | Owns |
| --- | --- |
| Identity | users, dealers, sub-dealer tree, price-group assignment, permissions, tokens |
| Catalog | products, categories, price lists, markup chain, search index |
| Order | cart, checkout orchestration (saga), orders, order lifecycle |
| Inventory | stock quantities, reservations |
| Payment | payment transactions, provider integration (PayTR) |
| Notification | outbound e-mail/notifications |

Cross-service database access is forbidden — see [docs/DATA_OWNERSHIP.md](docs/DATA_OWNERSHIP.md).
