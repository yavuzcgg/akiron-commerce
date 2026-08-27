# ADR-0003: PostgreSQL, database-per-service

## Status

Accepted — 2026-08-27 (re-recording a July 2026 decision)

## Context

Each service must own its data for the boundaries to be real. Running six
database containers locally and on an 8 GB shared VPS is wasteful.

## Decision

- **One PostgreSQL 16 container, one database per service**: `akiron_identity`, `akiron_catalog`, `akiron_order`, `akiron_payment`, `akiron_inventory`, `akiron_notification` (bootstrapped by `deploy/compose/init-databases.sql`).
- Ownership boundary is the **database**, not the instance. Cross-service database access is forbidden (see `docs/DATA_OWNERSHIP.md`).
- EF Core 10 + Npgsql as the default data access; Dapper allowed for demonstrated hot read paths in CQRS services.

## Alternatives considered

- **One container per service** — honest physical isolation, but ~6× the RAM for zero learning value at this scale.
- **MS SQL Server** — the owner's day-job stack; PostgreSQL broadens the CV and is free to operate.
- **Schema-per-service in one database** — weaker isolation story; migration tooling gets awkward.

## Consequences

### Positive

- Real ownership boundaries at near-monolith resource cost; single backup/ops surface.

### Negative

- One Postgres outage takes down all services (accepted for a demo system); the isolation is by convention + credentials, not physical.

## Exit strategy

Boundaries being per-database means promoting any database to its own instance
(or managed Postgres) is a connection-string change, not a refactor.
