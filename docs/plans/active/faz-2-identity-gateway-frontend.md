# Faz 2 — Identity, gateway, frontend shell

**Status:** Draft — awaiting owner review (Codex review welcome). Nothing here is
implemented until the open decisions below are answered.
**Goal (ROADMAP):** login → storefront showing dealer-specific prices.
**Governing ADRs:** 0004 (gRPC vs broker), 0010 (YARP), 0011 (auth), 0012 (dealer
pricing), 0013 (single Next.js app), 0018 (error codes).

## How plans live here

One file per phase or slice in `docs/plans/active/`, written **before** implementation
and updated as decisions land; moved to `docs/plans/completed/` when the slice closes. Each
slice below gets its own detailed plan file when it starts; this file is the phase map.

## Slices

Each slice ends demonstrable, with tests, a devlog entry and a commit — the same rule
as Faz 1.

| # | Slice | Demo at the end | Learning milestone |
| --- | --- | --- | --- |
| 2.1 | **Identity service, first vertical:** users, registration, login, password hashing, RS256-signed 15-minute access JWT, `/.well-known/jwks.json`. Four layers, migrations, Testcontainers, Dockerfile, CI. | `POST /login` returns a JWT that jwt.io verifies against the JWKS | Asymmetric signing: why a service that can *verify* must not be able to *mint* |
| 2.2 | **`Akiron.ServiceDefaults`:** Serilog, OTel, ProblemDetails, health checks — now duplicated in two services, so the rule of two finally allows extracting them. | Both services boot through `AddAkironDefaults()`; Catalog's behaviour unchanged (its 185 tests prove it) | Extracting an abstraction only once it has two real consumers |
| 2.3 | **Rotating refresh-token families** with reuse detection, stored hashed. | Replaying a used refresh token revokes the whole family | **Race lab:** two tabs refreshing at once look like token theft — naive version fails first, then the grace-window fix |
| 2.4 | **Dealers and price-group assignment** in Identity; the access token carries the dealer's `price_group` claim. Catalog validates tokens locally via JWKS and takes the price group **from the token**. | The same product list, anonymous vs logged-in dealer, shows list vs dealer price | Local JWT validation, JWKS caching and key rotation |
| 2.5 | **YARP gateway:** one origin, routes to Identity and Catalog, token passthrough, rate limiting, CORS. | One trace in Jaeger spanning gateway → Catalog → PostgreSQL | An in-process gateway; one more hop in a distributed trace |
| 2.6 | **Next.js shell:** App Router, route groups, Tailwind 4 + shadcn/ui, next-intl (TR default), storefront product listing rendered on the server through the gateway; ADR-0018 error codes translated on this side. | Product listing page with categories, in Turkish and English | RSC/server fetching; the error-code contract working end to end |
| 2.7 | **BFF-lite auth:** login via Next.js route handlers, HttpOnly cookies, single-flight refresh on the server. **The phase demo.** | Log in as a dealer → storefront prices change; no token visible in browser JS | Why tokens never reach the browser; single-flight refresh |
| 2.8 | **First gRPC call:** `GetDealerPermissions` (Identity) called from Catalog, cached in Redis with a 5-minute TTL, wrapped in a resilience pipeline. | A suspended dealer falls back to list prices within 5 minutes without logging out | gRPC contracts, deadlines, cache-aside; what happens when Identity is down |

Order note: 2.5 before 2.6 so the frontend only ever talks to the gateway. 2.8 last
because it needs a real permission to check, which 2.4 creates.

## Open decisions (owner)

Protected decisions are marked 🔒 — they need an explicit answer before the slice starts.

1. 🔒 **`?priceGroup=` on the public product list.** Today anyone can ask for any group's
   prices. From 2.4 the price group comes from the token. *Recommendation:* remove the
   parameter from the public route and keep it only behind an admin role. This changes a
   public API contract.
2. 🔒 **Where the markup chain values live.** Identity owns the sub-dealer tree (ADR-0012),
   so the percentages naturally belong there too; Catalog only resolves. *Recommendation:*
   Identity stores each dealer's markup; the token carries the chain for display; checkout
   (Faz 4) recomputes from Identity's data, never from the token.
3. **User store.** Full ASP.NET Core Identity (tables, UI, conventions) or own `users` table
   using only its `PasswordHasher<T>`. *Recommendation:* own table + `PasswordHasher<T>` —
   less magic, every column explainable, and the hashing is still not hand-rolled.
4. **Signing key in development.** Generated on first boot into a git-ignored file, or
   supplied through user-secrets. Never in source (AGENTS.md). *Recommendation:* generated
   file, with rotation (two keys in the JWKS) shown in 2.4.
5. **Cache invalidation for permissions.** ADR-0011 names a `DealerPermissionsChanged`
   event, but the broker enters in Faz 3. *Recommendation:* Faz 2 relies on the 5-minute
   TTL only; event invalidation is added in Faz 3 and recorded as such.
6. **Currency / exchange rates.** Deferred from 1.4. Needs its own ADR after reviewing the
   owner's B2B project (dealer account in USD → whole site in USD). Fits after 2.4, when a
   dealer exists to own a currency. Not scheduled until that review.

## Out of scope for Faz 2

Search (Faz 3), cart and stock (Faz 4), admin mutations, dealer portal screens beyond
login (Faz 6), Playwright e2e (Faz 7).

## Risks

- **Size.** Eight slices is the largest phase so far; ROADMAP gives it 5–6 weeks. If it
  slips, 2.8 moves to the start of Faz 3, which needs Redis-backed calls anyway.
- **Auth subtleties** (clock skew, key rotation, refresh races) are where the time goes;
  each gets a test before it gets a fix.
