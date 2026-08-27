# ADR-0011: Auth — RS256 JWT + JWKS, rotating refresh families, BFF-lite

## Status

Accepted — 2026-08-27

## Context

Six services must authenticate requests without calling Identity on every hop;
the browser must never hold tokens; dealer permissions must be revocable faster
than token expiry.

## Decision

- Identity signs **asymmetric JWTs (RS256/ES256)**, 15-minute lifetime, and exposes **JWKS**; every service validates locally — no per-request Identity call.
- **Rotating refresh-token families** with reuse detection (pattern proven in the owner's `akiron-seo`), stored hashed.
- **BFF-lite:** HttpOnly/Secure/SameSite=Lax cookies on the app domain; Next.js server code holds access+refresh tokens and attaches `Authorization: Bearer` to gateway calls. **Tokens never reach browser JavaScript.**
- Role claims gate route groups in middleware; **fine-grained permission flags are always checked server-side** via Identity's `GetDealerPermissions` gRPC (Redis cache, TTL 5 min, invalidated by `DealerPermissionsChanged`) — JWT claims are display hints, never authority (see ADR-0012 for pricing).

## Alternatives considered

- **Symmetric HS256 shared secret** — every service could *mint* tokens; one leak compromises all.
- **Tokens in browser storage + SPA calls** — XSS-exfiltratable; rejected outright.
- **Central introspection per request** — correct revocation, but couples every request to Identity's uptime; the 5-min cached server-side check is the middle path.

## Consequences

### Positive

- Local validation scales; browser attack surface minimized; revocation latency bounded at ~5 min for permissions, 15 min for identity itself.

### Negative

- Key rotation and JWKS caching become operational topics; BFF adds a session layer to get right (single-flight refresh etc.).

## Exit strategy

If BFF-lite proves too fiddly, fall back to the gateway terminating cookies and
exchanging them for JWTs (gateway-session pattern) — service-side validation is
unchanged either way.
