---
name: finish-session
description: End-of-session ritual — walks the Definition of Done chain, writes the devlog entry, commits, and asks the owner for push approval. Use when the owner says the session is ending or asks to wrap up.
---

# Finish Session

Walk this chain in order. Skip a step only if it does not apply, and say so.

1. **Format & build** — `dotnet format` on touched projects (once real code exists), then `dotnet build AkironCommerce.sln --configuration Release`. Warnings are errors; fix, never suppress.
2. **Tests** — run unit + integration tests for every service touched this session. Frontend touched → `pnpm lint && pnpm test` in `frontend/`. Name any suite you did not run and why.
3. **Diff review** — `git status` + `git diff`; confirm the diff is narrowly aligned with the session's tasks (AGENTS.md Scope Discipline). Unrelated changes get reverted or moved to their own commit with an explanation.
4. **Docs matrix** — did this session: change architecture → ADR updated/created? · add an integration event → `docs/EVENT_CATALOG.md`? · create a procedure → runbook? · change scope → `docs/ROADMAP.md`? · finish a feature plan → move it to `docs/plans/completed/`?
5. **Devlog** — append today's entry to `docs/devlog/<YYYY-MM>.md` (create the month file if needed):

   ```md
   ## YYYY-MM-DD

   ### Goal
   ### Done
   ### Learned
   ### Decisions
   ### Problems
   ### Next
   ### Commits
   ```

6. **Commit** — Conventional Commits, small and meaningful (split if the session produced distinct concerns). NEVER add Co-Authored-By or any AI attribution.
7. **Push (ask first)** — ask the owner: "Push to origin?" Pushing every session is the norm (repo history was lost once, July 2026) — but the push itself always waits for an explicit yes.
8. **Report** — one short summary: what landed, what's verified, what's next.
