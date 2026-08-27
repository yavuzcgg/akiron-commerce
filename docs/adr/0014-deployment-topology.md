# ADR-0014: Local compose + VPS compose; Kubernetes local-only (k3d)

## Status

Accepted — 2026-08-27

## Context

Kubernetes is a learning goal. The only server is a shared 4 vCPU / 8 GB Hetzner
VPS already running three production projects behind one Caddy. Its disk was
83% full until a 2026-08-27 BuildKit-cache prune freed ~47 GB (now ~25%).

## Decision

- **Daily development is fully local** against `deploy/compose/docker-compose.infra.yml` (pinned, healthchecked infra).
- **Kubernetes lives locally via k3d** (real k3s in Docker) as Faz 8 — manifests, probes, limits, HPA, kube-prometheus-stack. **k3s is never installed on the VPS**: shared production box, RAM headroom, and containerd double-image-store cost.
- **The deployment target is docker-compose on the VPS** (Faz 7): joins the shared Caddy network, publishes no host ports, subdomains `akiron.yavuzcelik.com` (web) + `api.akiron.yavuzcelik.com` (gateway).
- VPS resource caps: ES heap 512 MB; per-service .NET heap limits (~200 MB); compose log caps (`max-size: 10m`, `max-file: 3`); weekly `docker builder prune` cron. Operating condition: the co-hosted FormReader VLM sidecar is off while AkironCommerce runs.
- An optional early live preview may go up after Faz 3 (decided then).

## Alternatives considered

- **k3s on the VPS** — "real prod k8s" on paper; in practice risks three unrelated production apps for zero extra learning over k3d.
- **A second dedicated k3s VPS** — clean, ~€4–10/month; deferred as a 2027+ option, not needed for the learning goal.
- **Cloud (Azure/AWS)** — explicitly backlog; the architecture stays 12-factor so the door remains open.

## Consequences

### Positive

- k8s learning decoupled from production risk; live demo achievable on hardware already paid for.

### Negative

- The live demo doesn't run on k8s (portfolio tells this story honestly: compose in prod, k8s in the lab write-up).

## Exit strategy

If the VPS becomes too tight even with caps, first drop Jaeger from the VPS
stack, then rent the dedicated k3s/compose VPS — deploy files already isolate
environment differences in a compose override.
