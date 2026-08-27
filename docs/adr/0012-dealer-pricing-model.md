# ADR-0012: Dealer pricing — price groups + markup chain, server-authoritative

## Status

Accepted — 2026-08-27 (re-recording a July 2026 decision)

## Context

The B2B side needs per-dealer pricing with sub-dealers applying markups down a
chain, and dynamic permission flags per dealer. Pricing data placed in tokens
is a classic tamper target.

## Decision

- **Price groups** define base dealer pricing (Catalog owns definitions; Identity owns the dealer→group assignment).
- **Sub-dealer markup chain:** a dealer may create sub-dealers; effective price = group price + accumulated markups, resolved by Catalog.
- **Server-authoritative pricing:** JWT carries price-group claims for *display*; every checkout price is recomputed by Catalog via `GetPricingSnapshot` and locked into the order (`OrderPriceLocked` event). The 15-minute token bounds staleness of display prices.
- **Dynamic permission flags** (e.g. can-see-stock, can-create-subdealer) are enforced server-side (ADR-0011), never from claims.

## Alternatives considered

- **Prices trusted from claims** — tamper- and staleness-prone; rejected.
- **Per-dealer price lists (no groups)** — simplest mentally, O(dealers) maintenance; groups + markups model the real-world hierarchy.
- **Pricing microservice** — a seventh service for what is a Catalog concern; violates ADR-0002.

## Consequences

### Positive

- The tamper-proof pricing story is a strong portfolio/security talking point; markup chain is a genuinely interesting domain model for Faz 1.

### Negative

- Markup-chain resolution needs care (depth limits, cycles forbidden); price display vs charge divergence must be handled in UX (checkout re-quote).

## Exit strategy

If the chain model proves too heavy, flatten to "every dealer has exactly one
price group, markups only at order line level" — Catalog's public contract
(`GetPricingSnapshot`) is unchanged.
