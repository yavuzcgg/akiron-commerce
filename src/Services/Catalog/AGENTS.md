# Catalog Service

## Owns

Products, categories, price groups, price lists, and the markup chain that turns
them into a dealer price. From Faz 3 it also owns the Elasticsearch index and the
indexer that feeds it.

## Does not own

Dealer identity or the dealer → price-group assignment (Identity) · stock and
reservations (Inventory) · orders and the prices locked into them (Order).
Full table: `docs/DATA_OWNERSHIP.md`.

## Invariants

- **No MediatR here** (ADR-0006). Endpoint modules call handler classes directly, and every handler and validator is registered by name in `Program.cs`. No assembly scanning anywhere in this service — the wired-up set stays greppable.
- **`ICatalogDbContext` must never gain a method.** It exists solely to invert the Application → Infrastructure reference. A `GetById` or `FindAsync` on it turns it into the generic repository the root AGENTS.md forbids; handlers write their own LINQ against the sets.
- **Uniqueness lives in the index, not in the handler.** A pre-check buys a friendly 409 for the ordinary case, but only the unique index holds under concurrency. Every new unique index gets its constraint name added to `CatalogExceptionHandler.DescribeUniqueViolation`, and a test that races two writers.
- **Value objects validate in their factory and nowhere else.** `Slug.Create` either returns a valid slug or throws; nothing downstream re-checks the format.
- **Every id is a typed id.** Register the conversion once in `CatalogDbContext.ConfigureConventions`, and give it a JSON converter plus an entry in `TypedIdSchemaTransformer` so the wire format and the OpenAPI document agree.
- **Prices are server-authoritative** (ADR-0012). A price arriving in a request is an input to a calculation, never the answer.

## Persistence

PostgreSQL `akiron_catalog`, snake_case naming, entity configurations listed one
by one in `OnModelCreating`. Timestamps come from `Timestamp.UtcNow()`, which
truncates to the microseconds `timestamptz` actually stores.

New migration:

```
dotnet ef migrations add <Name> \
  --project src/Services/Catalog/Akiron.Catalog.Infrastructure/Akiron.Catalog.Infrastructure.csproj \
  --output-dir Persistence/Migrations
```

Startup applies migrations only in Development, and readiness queries a table
rather than merely opening a connection — a service whose migrations never ran
must report itself unready instead of serving 500s.
