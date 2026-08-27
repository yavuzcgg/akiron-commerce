# AkironCommerce — Agent Guide

AkironCommerce is two things at once:

1. a production-quality B2B/B2C commerce platform;
2. the owner's deliberate distributed-systems learning project.

Correctness, explainability and learning take priority over implementation speed.

## Where truth lives (read on demand — never bulk-load)

- Architecture-sensitive work → read `docs/SYSTEM_ARCHITECTURE.md`
- Service & data ownership → read `docs/DATA_OWNERSHIP.md`
- Recorded decisions → read the relevant files under `docs/adr/` (a decision recorded in an ADR is never silently changed)
- Current scope & phases → `docs/ROADMAP.md`; the active feature → `docs/plans/active/`
- Messaging contracts → `docs/EVENT_CATALOG.md` (exists from Faz 3 on)
- Before modifying a service or the frontend, read the nearest `AGENTS.md`.

## MUST (hard invariants)

- Work vertically, not breadth-first. A service must be end-to-end demonstrable (endpoint + migration + integration test + Dockerfile + green CI) before the next service directory is created.
- Never scaffold future services or empty projects. Every created `.csproj` gains real code and a passing test within the same phase.
- No cross-service database access. Cross-service information flows only through gRPC queries, integration events, or commands.
- No shared abstraction until equivalent code exists in at least two services. No `Common`, `Utils`, or catch-all projects.
- A new infrastructure technology requires an ADR before code.
- Package versions pinned in `Directory.Packages.props` are license pins — never bump them without the owner (ADR-0005, ADR-0006).
- Dependency direction: Api → Application → Domain; Infrastructure → Domain. Domain references nothing.
- CQRS/MediatR only in Order, Payment, Inventory. Marten/event sourcing only in the Order aggregate.
- gRPC carries synchronous read-only queries; RabbitMQ carries asynchronous commands and events. No gRPC inside saga steps; no gRPC chains (A→B→C).
- Feature-first folders (`Products/CreateProduct/…`). Technical-bucket folders (`Services/`, `Repositories/`, `Managers/`, `Helpers/`, `Dtos/`) are forbidden.
- Secrets never appear in source. Auth tokens never reach browser JavaScript.

## MUST NOT (agent behavior)

- Do not refactor, rename, reformat, or upgrade anything unrelated to the task. Keep the diff narrowly aligned with the task.
- Do not disable or skip tests to make CI pass; do not suppress warnings without a documented reason; do not swallow exceptions.
- Do not add TODO-only implementations to "complete" an architecture.
- Do not introduce abstractions for hypothetical future requirements.
- Do not push to remote without explicit owner approval (approval is requested at session end — see the `finish-session` skill).

## Protected decisions (changing them requires owner approval)

Service boundaries · database ownership · messaging topology · authentication model · event sourcing scope · public API contracts · dependency licensing strategy · target framework · deployment topology.

If a protected decision looks problematic: STOP implementation, describe the problem, propose alternatives, wait for the owner.

## Learning mode

The owner is learning the technologies used here. When introducing an unfamiliar mechanism:

1. explain the problem it solves;
2. where educational, show the naive implementation and demonstrate how it fails;
3. implement the real solution;
4. prove it with a test;
5. record meaningful findings in the devlog.

Never hide important distributed-systems mechanics behind abstractions, and never replace a planned learning milestone (labs, spikes) with a shortcut library call.

## Definition of Done

"Done" means the whole chain, not the code compiling:

format → build (warnings are errors) → relevant unit tests → relevant integration tests → frontend checks if the frontend changed → review `git diff` → update affected docs (architecture change → ADR · new integration event → EVENT_CATALOG · new procedure → runbook · progress → devlog · scope change → ROADMAP) → devlog entry → commit.

Name explicitly any check you did not run.

## Style

explicit > clever · boring > magical · vertical slice > speculative framework · working software > empty architecture · integration tests > mocks for infrastructure · business language > technical jargon.

## Meta

This file stays under 150 lines. If it grows past that, refactor: move detail into `docs/` and keep the rule here as a single line.
