# ADR-0005: MassTransit pinned to v8.5.x (license)

## Status

Accepted — 2026-08-27 · **Re-open at the start of Faz 5** (decision gate)

## Context

MassTransit v9 moved to a commercial license (Massient); v8 remains
Apache-2.0 but reaches **end-of-life at the end of 2026** — frozen, no further
security fixes. Massient offers a 100% discounted v9 license to organizations
under $1M revenue, but every person cloning this public repo would need their
own license to run v9.

## Decision

- Pin **MassTransit 8.5.10** (and `MassTransit.RabbitMQ`) in `Directory.Packages.props`. The pin is a license pin: never bumped casually.
- Accept running a frozen-but-Apache v8 into 2027 for a learning/demo system.
- **Re-open this ADR at the start of Faz 5** (when saga work begins, post-EOL): compare "v8 frozen" vs "v9 free license" with fresh facts about v9's activation/clone-and-run friction.

## Alternatives considered

- **v9 + free <$1M license** — current features and fixes, but adds license activation friction for anyone cloning a public portfolio repo (`git clone && docker compose up` must stay frictionless).
- **Wolverine** — excellent OSS and Marten's natural partner, but MassTransit is the market-recognized skill this project targets. Recorded as the long-term escape hatch.
- **Raw RabbitMQ client** — maximal learning, but rebuilds saga/outbox machinery the project wants to *use*, not re-implement.

## Consequences

### Positive

- Zero-friction clone-and-run; Apache license; battle-tested docs for sagas/outbox.

### Negative

- No security patches after 2026; ecosystem attention moves to v9; some newer features unavailable.

## Exit strategy

Trigger: a relevant v8 CVE, or v9 activation proving frictionless for OSS
clones. Path A: adopt v9 free license. Path B: migrate saga + consumers to
Wolverine (contracts in `Akiron.Contracts` are transport-agnostic records, so
the blast radius is configuration + saga definition, not domain code).
