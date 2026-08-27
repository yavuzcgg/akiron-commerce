# ADR-0008: Elasticsearch 9.5 for catalog search

## Status

Accepted — 2026-08-27

## Context

Faceted, full-text, Turkish-language product search is a core commerce feature
and a stated learning goal. The index must never become a second source of
truth, and it must run in 512 MB heap on a shared VPS.

## Decision

- **Elasticsearch 9.5.x** (current major; a new repo starts on the current major unless an ADR says otherwise).
- PostgreSQL stays the system of record; **the ES index is a disposable projection**, fed by `ProductUpserted`/`ProductDeleted`/`PriceListChanged` events consumed by an Indexer module inside Catalog; a `reindex` command rebuilds from Postgres.
- Turkish analyzer; facets over category/brand/attributes; price-group-aware pricing in results.
- Heap capped at **512 MB** everywhere. A small spike at the start of Faz 3 validates the 9.x .NET client (`Elastic.Clients.Elasticsearch`) + analyzer config; fallback to 8.19.x if it misbehaves.
- Licensing noted consciously: the official Docker image is Elastic License 2.0 (free basic tier); AGPLv3 exists as a source option.

## Alternatives considered

- **PostgreSQL full-text search** — fine for v0-scale search, but facets/analyzers/relevance are exactly the skills being targeted.
- **OpenSearch** — documented plan B (Apache-2.0); smaller .NET client ecosystem and less CV recognition.
- **Meilisearch/Typesense** — lighter and lovely, but off the learning target.

## Consequences

### Positive

- Real search UX (facets, Turkish full-text) and a clean event-driven-projection lesson with a practiced recovery path.

### Negative

- Heaviest infra container (~1 GB RSS at 512 MB heap); index/mapping migrations become a maintenance topic.

## Exit strategy

Because ES is a projection behind a search interface, swapping to OpenSearch
(or degrading to PG full-text) means re-implementing the indexer + search
endpoint — zero domain-model impact. The `reindex` command is the migration tool.
