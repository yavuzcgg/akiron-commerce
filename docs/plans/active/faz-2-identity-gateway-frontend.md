# Faz 2 — Identity, gateway, frontend shell

**Status:** Approved 2026-10-02 — decisions below answered by the owner. 2.1 done 2026-10-02; next: 2.2.
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
| 2.4b | **Dealer currency + exchange rates** (decision 6): dealer currency in Identity and the token, `exchange_rates` in Catalog, conversion after resolution. | A USD dealer sees the same catalogue in dollars | Where conversion and rounding belong in a price pipeline |
| 2.5 | **YARP gateway:** one origin, routes to Identity and Catalog, token passthrough, rate limiting, CORS. | One trace in Jaeger spanning gateway → Catalog → PostgreSQL | An in-process gateway; one more hop in a distributed trace |
| 2.6 | **Next.js shell:** App Router, route groups, Tailwind 4 + shadcn/ui, next-intl (TR default), storefront product listing rendered on the server through the gateway; ADR-0018 error codes translated on this side. | Product listing page with categories, in Turkish and English | RSC/server fetching; the error-code contract working end to end |
| 2.7 | **BFF-lite auth:** login via Next.js route handlers, HttpOnly cookies, single-flight refresh on the server. **The phase demo.** | Log in as a dealer → storefront prices change; no token visible in browser JS | Why tokens never reach the browser; single-flight refresh |
| 2.8 | **First gRPC call:** `GetDealerPermissions` (Identity) called from Catalog, cached in Redis with a 5-minute TTL, wrapped in a resilience pipeline. | A suspended dealer falls back to list prices within 5 minutes without logging out | gRPC contracts, deadlines, cache-aside; what happens when Identity is down |

Order note: 2.5 before 2.6 so the frontend only ever talks to the gateway. 2.8 last
because it needs a real permission to check, which 2.4 creates.

## Decisions (answered by the owner, 2026-10-02)

1. 🔒 **`?priceGroup=` leaves the public product list** (2.4). The price group comes from
   the token; the parameter survives only behind an admin role.
2. 🔒 **Markup percentages live in Identity**, next to the sub-dealer tree. Catalog only
   resolves. The token carries the chain for display; checkout (Faz 4) recomputes from
   Identity's data, never from the token. `DATA_OWNERSHIP.md` updated.
3. **Own `users` table + `PasswordHasher<T>`** — ADR-0019.
4. **Signing key in development:** generated on first boot into a git-ignored file;
   production must supply one or the service refuses to start. Rotation (two keys in the
   JWKS) is shown in 2.4.
5. **Permission cache invalidation:** TTL only in Faz 2; the `DealerPermissionsChanged`
   event arrives with the broker in Faz 3.
6. **Currency — designed our way, after the owner's Logo pattern.** In Logo a dealer
   account and a stock price card both carry a *ticari işlem grubu* (e.g. `BAYI`); the
   price is the one whose group matches the dealer's. A dealer account can also be in
   USD, in which case the TRY price is converted at the day's rate. Here that maps to:
   the **price group code is the matching key** (already true in Catalog), the dealer's
   **currency lives in Identity** and rides in the token, and Catalog converts the
   resolved TRY price with a stored **exchange rate** (Catalog-owned table, manual entry
   first, a rate feed later). Conversion happens once, after resolution and markups,
   followed by the single rounding. Gets its own slice (**2.4b**) and ADR when it starts.

## Out of scope for Faz 2

Search (Faz 3), cart and stock (Faz 4), admin mutations, dealer portal screens beyond
login (Faz 6), Playwright e2e (Faz 7).

## Risks

- **Size.** Eight slices is the largest phase so far; ROADMAP gives it 5–6 weeks. If it
  slips, 2.8 moves to the start of Faz 3, which needs Redis-backed calls anyway.
- **Auth subtleties** (clock skew, key rotation, refresh races) are where the time goes;
  each gets a test before it gets a fix.
