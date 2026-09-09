# ADR-0018: The API returns error codes; clients do the translating

## Status

Accepted — 2026-09-09

## Context

Every error message the Catalog service produces today is English prose, written
for whoever reads the logs. The product is Turkish-first with English alongside
(ADR-0013 puts next-intl in the frontend from its first day), so something has to
turn "A product with SKU 'ABC-1' already exists." into Turkish. The API is not the
only consumer of these errors either: the storefront, the dealer portal, the admin
area and later integrations all read them.

## Decision

- Every failure response carries a **stable machine-readable `code`** plus the
  **`params`** needed to render it, alongside the RFC 7807 fields already in place.
- Codes are namespaced `<service>.<resource>.<reason>`, lower snake within a
  segment: `catalog.product.sku_conflict`, `catalog.category.not_found`.
- **`detail` stays English.** It is written for developers, log search and bug
  reports; it is not what an end user should be shown.
- Validation failures carry a code per field rule, set with FluentValidation's
  `WithErrorCode`, so the field errors localise the same way as everything else.
- Clients own the translation. The frontend maps `code` + `params` through
  next-intl; adding a language is a dictionary file, not a backend release.
- A code, once published, is part of the public contract: renaming one is a
  breaking change and needs the same care as renaming a field.
- Implemented in slice 1.3, next to `docs/API_CONVENTIONS.md`, which is where the
  catalogue of codes lives.

Unchanged by this decision: source code, comments, commit messages and log output
stay English, as they already are.

## Alternatives considered

- **The API translates, driven by `Accept-Language`** (`RequestLocalizationMiddleware` plus `.resx` resources) — the familiar .NET answer and genuinely simpler when one frontend is the only caller. Rejected because every new language would then be a backend change, logs would arrive in mixed languages, and the same failure could no longer be grepped by one string.
- **Both: codes in the contract, and a translated `detail` when `Accept-Language` is present** — the most flexible, and the most to maintain: a code catalogue *and* resource files, kept in step forever. Not worth it while every client is one we write.
- **Leave it English** — honest for an internal tool, wrong for a storefront where the shopper reads what the API said.

## Consequences

### Positive

- One place per language, in the layer that already knows the user's locale.
- Logs and traces stay searchable in a single language.
- Codes are stable identifiers, so a client can branch on a failure without string matching.

### Negative

- Two things to keep in step: the code emitted by the server and the message that renders it. A code with no dictionary entry shows up as a raw code in the UI.
- Slightly more ceremony in handlers: throwing now means naming the failure, not just describing it.

## Exit strategy

If a client that cannot translate ever appears — a partner integration, a report
generator — the API can add `Accept-Language` handling that fills `detail` from a
resource file. The codes stay the contract either way, so that addition is
additive and breaks nothing.
