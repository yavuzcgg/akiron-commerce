# ADR-0017: AwesomeAssertions for test assertions

## Status

Accepted — 2026-09-08

## Context

The first test projects arrived with the Catalog slice and needed an assertion
library. FluentAssertions — the default choice in .NET and the one used in this
owner's other projects — requires a paid licence from version 8 onwards, and this
repository is public: anyone cloning it must be able to run `dotnet test` without
buying anything. This is the third dependency in the project to move behind a
commercial licence, after MassTransit v9 (ADR-0005) and MediatR v13 (ADR-0006).

## Decision

- Use **AwesomeAssertions 9.6.0** (Apache-2.0) for assertions in every test project.
- Keep it in the "License pins" group of `Directory.Packages.props`, next to the other pins, so the constraint is visible in one place.
- Do not add FluentAssertions to this repository at any version.

## Alternatives considered

- **FluentAssertions 7.x** — the last free version, and functionally fine, but frozen: it receives no fixes and pulls the project toward an upgrade it cannot legally take.
- **Shouldly (MIT)** — healthy, independent, and genuinely pleasant, but a different syntax the owner would be learning while also learning the domain. Kept as the fallback if AwesomeAssertions stalls.
- **Plain xUnit asserts** — zero dependencies and zero licence risk, at the cost of weaker failure messages on collections and object graphs. That cost is paid on every failing test, which is exactly when clarity matters most.

## Consequences

### Positive

- Familiar `result.Should().Be(...)` syntax, so no time is spent relearning assertions.
- `git clone && dotnet test` works for anyone, which a public portfolio repository depends on.

### Negative

- A fork carries fork risk: it is maintained by a smaller group than the original, and could fall behind .NET releases.

## Exit strategy

Trigger: AwesomeAssertions stops releasing for a current .NET version, or its
licence changes. Path: the assertion calls are the only affected code and they are
mechanical to convert — Shouldly is the pre-picked replacement, and the migration
is a find-and-replace over test files, with no production code touched.
