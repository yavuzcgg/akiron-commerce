---
name: create-adr
description: Create a new Architecture Decision Record from the template, number it, and index it. Use when a decision needs recording — new infrastructure, changed architecture, resolved trade-off — or when the owner asks for an ADR.
---

# Create ADR

1. **Number** — read `docs/adr/README.md`; next number = highest existing + 1, zero-padded to 4 digits.
2. **File** — copy `docs/adr/template.md` to `docs/adr/NNNN-short-kebab-title.md`.
3. **Fill every section.** Rules that matter here:
   - **Context**: the forcing problem, not history. 2–5 sentences.
   - **Decision**: imperative bullets, concrete enough that a stranger could enforce them.
   - **Alternatives considered**: at least two real ones, each with the one-line reason it lost. No strawmen.
   - **Exit strategy is mandatory** — the trigger that would reopen this decision, and the concrete path out. An ADR without a credible exit is not done.
4. **Index** — add the row to the table in `docs/adr/README.md`.
5. **Cross-links** — if this supersedes an ADR, mark the old one `Superseded by ADR-NNNN` (never rewrite its content). If AGENTS.md states the rule this ADR justifies, make sure the two match — the rule lives in AGENTS.md as one line; the reasoning lives here.
6. **Commit** — `docs: ADR-NNNN <title>` (include related rule/doc updates in the same commit).
