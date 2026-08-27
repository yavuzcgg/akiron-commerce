@AGENTS.md

# Claude Code — project-specific

- Use plan mode before: architectural changes, changes spanning multiple services, database schema changes, messaging changes, security-sensitive changes.
- Before implementing, locate and read the relevant ADRs under `docs/adr/`.
- Do not implement a proposed architecture until its trade-offs have been explained to the owner.
- Roles: Claude Code writes the code. Codex is used by the owner for plan/design review — do not delegate implementation to it.
- Commits: Conventional Commits, small and meaningful. NEVER add Co-Authored-By or any AI attribution.
- Session end: run the `finish-session` skill (it walks the Definition of Done chain and asks for push approval — pushing every session is the norm; this project's history was lost once before).
- Talk to the owner in Turkish; write code, docs, and commits in English.
