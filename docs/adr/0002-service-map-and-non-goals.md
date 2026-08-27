# ADR-0002: Six services; Shipping/Review/Promotion are non-goals

## Status

Accepted — 2026-08-27 (re-recording a July 2026 decision)

## Context

Service count is the main scope dial. An earlier abandoned attempt
(February 2026) scaffolded 12 services + gateway as 45 empty projects and died
before the first line of business logic.

## Decision

- Exactly six services: **Identity, Catalog, Order(+Cart), Payment, Inventory, Notification**.
- **Non-goals for v1:** Shipping, Review, Promotion, marketplace integrations, mobile app, multi-tenancy claims. They live only as a backlog list in ROADMAP.md.
- A seventh service requires a new ADR and owner approval (protected decision).
- Notification is built last and stays thin (max 2 projects).

## Alternatives considered

- **12-service map** (previous attempt) — proven motivation cliff for a solo developer.
- **4 services** (merge Inventory into Catalog, Cart into a Gateway session) — merges the exact boundaries where the race-condition and saga lessons live.

## Consequences

### Positive

- Every service has a distinct pedagogical purpose; none exists "for symmetry".

### Negative

- Some real-commerce features (shipping tracking, reviews) will be visibly absent from the demo.

## Exit strategy

If a non-goal becomes genuinely needed (e.g. shipping for the live demo), it
enters as a module inside an existing owner service first; it graduates to a
service only with a new ADR.
