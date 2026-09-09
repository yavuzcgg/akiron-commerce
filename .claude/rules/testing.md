---
paths:
  - "src/**/tests/**/*.cs"
---

# Testing rules

- Infrastructure is tested against the real thing: Testcontainers PostgreSQL. EF Core InMemory is not a substitute and is not used here.
- One container per collection, migrated once with the real migrations; Respawn empties the tables between tests. Do not start a container per test.
- Test names are sentences: `Method_WithCondition_DoesThing`. Underscores are allowed here and nowhere else in the repository.
- Assertions use AwesomeAssertions (ADR-0017). Add a `because` wherever a failure message would not explain itself.
- Pass `TestContext.Current.CancellationToken` to every async call in a test.
- Tests must be deterministic. `Guid.CreateVersion7()` only sorts across millisecond boundaries; timing and ordering assumptions need an explicit gap or a different assertion.
- Give a test class a comment saying **why it exists** when the name does not carry it.
- Concurrency tests demonstrate the failure before demonstrating the fix, per the learning mode in the root AGENTS.md.
