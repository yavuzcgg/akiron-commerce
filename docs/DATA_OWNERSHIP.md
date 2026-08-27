# Data Ownership

Single source of truth for "who owns what". If data is not listed here, the
service introducing it must add it in the same PR.

| Entity / data | Owner service | Database |
| --- | --- | --- |
| User, credentials, refresh-token families | Identity | `akiron_identity` |
| Dealer, sub-dealer tree | Identity | `akiron_identity` |
| Price-group **assignment** (dealer → group) | Identity | `akiron_identity` |
| Permission flags | Identity | `akiron_identity` |
| Product, category, attributes | Catalog | `akiron_catalog` |
| Price lists, markup chain **definition** | Catalog | `akiron_catalog` |
| Search index (projection) | Catalog | Elasticsearch (disposable) |
| Cart | Order | Redis (`cart:*`) |
| Order aggregate (event-sourced) | Order | `akiron_order` (Marten) |
| Saga state | Order | `akiron_order` (EF tables) |
| Stock quantities, reservations | Inventory | `akiron_inventory` |
| Payment transactions, provider records | Payment | `akiron_payment` |
| Notification templates & log | Notification | `akiron_notification` |

## Hard rule

**No service ever queries another service's database — not even "just a read".**

Cross-service information flows only through:

1. **gRPC queries** — synchronous, read-only (`GetPricingSnapshot`, `CheckAvailability`, `GetDealerPermissions`);
2. **Integration events** — facts, published after commit (`StockReserved`, `PaymentCompleted`);
3. **Commands** — directed instructions over the broker (`ReserveStock`, `ProcessPayment`).

The forbidden shortcut this rule exists to prevent:

```csharp
// 💀 inside Order service — NEVER
var product = await _catalogDbContext.Products.FindAsync(id);
```

## Distinctions that matter

- `Product.Price` ≠ final checkout price (checkout price is recomputed by Catalog per price group).
- `AvailableStock` ≠ `PhysicalStock` (reservations in between).
- `OrderSubmitted` ≠ `OrderConfirmed` (saga sits in between).
- `PaymentCompleted` ≠ `OrderConfirmed` (confirmation is the saga's decision).
