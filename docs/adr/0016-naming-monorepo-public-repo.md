# ADR-0016: `Akiron.*` naming, monorepo, public repository

## Status

Accepted — 2026-08-27

## Context

Names and repo shape are cheap to decide now and expensive to change later.
Windows path-length limits punish long project names. A previous private
attempt was lost with zero commits pushed.

## Decision

- **Namespace/package prefix `Akiron.*`** (not `AkironCommerce.*`): shorter Windows paths; the repo name already provides context. Pattern: `Akiron.<Service>.<Layer>` (e.g. `Akiron.Catalog.Domain`), building blocks `Akiron.SharedKernel` / `Akiron.ServiceDefaults` / `Akiron.Contracts`.
- **Monorepo**: backend, frontend, deploy, docs, labs in one repository — one history, one CI, cross-cutting refactors atomic.
- **Public repository** (`yavuzcgg/akiron-commerce`): portfolio visibility, unlimited GitHub Actions minutes, "built in the open" discipline with honest status tables.
- Git discipline (hard rule born from the July 2026 data loss): commit every meaningful step; push every session (with owner approval); tag per phase (`faz-1`, …). Conventional Commits; trunk-based.

## Alternatives considered

- **`AkironCommerce.*` prefix** — 8 characters longer in every path for zero information.
- **Polyrepo per service** — realistic for orgs, pure overhead for one person.
- **Private repo** — hides the work from the audience it exists for, and caps CI minutes (Testcontainers CI is minute-hungry).

## Consequences

### Positive

- Everything about the project is inspectable by a recruiter in one link; CI is free at any volume.

### Negative

- Mistakes are public (accepted — honest status tables and ADRs turn them into content); licensing must be chosen consciously before third-party contributions arrive (tracked in backlog).

## Exit strategy

Namespaces and repo visibility are one-time mechanical changes (rename +
`gh repo edit --visibility`); no code depends on either.
