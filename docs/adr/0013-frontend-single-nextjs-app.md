# ADR-0013: Single Next.js app, self-written UI

## Status

Accepted — 2026-08-27 (July 2026 decision, scope updated: full frontend, not lean)

## Context

Storefront, dealer portal and admin could be three apps or one. Purchased
themes were evaluated and rejected in July 2026 (licensing + loss of control).
The owner explicitly wants a big, real commerce frontend this time, built slowly.

## Decision

- **One Next.js 16 App Router app**, route groups: `(storefront)`, `(dealer)`, `(admin)`, `(auth)`.
- **Fully self-written UI**: Tailwind 4 `@theme` tokens + shadcn/ui primitives; no purchased themes. Design system documented in `frontend/DESIGN_SYSTEM.md`; a local-only `/dev/components` playground instead of Storybook.
- Data: RSC/server fetch for SEO-critical pages; TanStack Query 5 for client interactivity; Zustand only for ephemeral UI state.
- i18n via next-intl from day one (TR default, EN lazy).
- Admin ships read-only first; every backend phase has a visible frontend deliverable (the motivation engine).

## Alternatives considered

- **Three separate apps** — cleaner isolation, 3× the build/deploy/auth plumbing for one developer.
- **Admin from a dashboard starter/Refine** — evaluated in July; rejected for control and license reasons.
- **Blazor** — stack-coherent, but Next.js/React is the owner's frontend skill investment.

## Consequences

### Positive

- One deploy, one auth session, shared design system; route groups keep concerns visibly separated.

### Negative

- One bundle serves three audiences (mitigated by route-group code splitting); a slow storefront redesign blocks the whole app's deploy.

## Exit strategy

Route groups are the split line: any group can be extracted into its own app
later by moving the folder and sharing the design-system package.
