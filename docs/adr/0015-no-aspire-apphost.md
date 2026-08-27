# ADR-0015: No .NET Aspire AppHost; standalone dashboard only

## Status

Accepted — 2026-08-27

## Context

.NET Aspire's AppHost offers F5 orchestration of services + infra. This project
already commits to docker-compose (the deployment target) and k3d (the learning
target).

## Decision

- **No Aspire AppHost.** It would be a third orchestration model that ships to neither target, and its abstractions would hide the compose/k8s mechanics this project exists to teach.
- **Yes to the standalone Aspire dashboard container** (`mcr.microsoft.com/dotnet/aspire-dashboard`) in the local compose stack — a free OTLP-native pane for logs+metrics+traces next to Jaeger.
- The idea of Aspire's ServiceDefaults survives as our own `Akiron.ServiceDefaults` project (Serilog + OTel + health checks + ProblemDetails wiring), written by hand and understood.

## Alternatives considered

- **Full Aspire (AppHost + manifests)** — genuinely good DX, but the manifest-to-deployment story would replace exactly the compose/k8s learning in scope.
- **No Aspire at all** — discards a zero-cost, high-value local telemetry UI for purity.

## Consequences

### Positive

- One fewer orchestration model; local telemetry UX nearly as good as full Aspire.

### Negative

- No F5-runs-everything convenience; developers start infra via compose and services via IDE (documented in the local-development runbook).

## Exit strategy

Adopting AppHost later is additive (a new project referencing existing
services); nothing in the codebase blocks it. Revisit only if local F5 pain
becomes real and measured.
