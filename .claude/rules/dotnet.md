---
paths:
  - "src/**/*.cs"
---

# C# style in this repository

- File-scoped namespaces. `sealed` on anything not designed to be inherited from.
- Primary constructors for dependency injection.
- Never `.Result` or `.Wait()`. Pass `CancellationToken` to every async call that takes one.
- Logging goes through source-generated `[LoggerMessage]` partial methods. The analyzers reject `logger.LogInformation("...", args)`, and any argument that comes from a property must be read into a local first.
- Comments explain **why**, in full sentences. A comment that restates the code is worse than no comment.
- Descriptive lambda parameters in LINQ (`category`, `failure`), not `x`.
