# ADR-0019: Own user table, ASP.NET Core Identity's password hasher only

## Status

Accepted — 2026-10-02

## Context

Identity needs users, credentials and, from Faz 2.3 on, refresh-token families. Full
ASP.NET Core Identity brings a fixed schema (`AspNetUsers`, roles, claims, logins,
tokens — seven tables before the first feature), its own `UserManager` abstractions
and conventions that this project would have to explain without having chosen. The
owner wants every column explainable and a model shaped around dealers, not around a
generic membership system.

## Decision

- Identity owns a plain `users` table modelled in its own domain (`User`, `Email`, `UserId`).
- Password hashing uses **only** `PasswordHasher<TUser>` from `Microsoft.Extensions.Identity.Core` (PBKDF2-HMAC-SHA512, 100k iterations in v3 format, salted, versioned). Hashing is never hand-rolled.
- `VerifyHashedPassword` returning `SuccessRehashNeeded` re-hashes on login, so raising the work factor later upgrades users as they sign in.
- No `UserManager`, `SignInManager`, `IdentityDbContext` or Identity UI.

## Alternatives considered

- **Full ASP.NET Core Identity** — battle-tested flows (lockout, 2FA, confirmation), but a schema and abstraction layer this project would carry without needing most of it.
- **Duende IdentityServer / OpenIddict** — a full OAuth server; commercial licence (Duende) or a large surface (OpenIddict) for a single first-party client.
- **Hand-rolled hashing** — rejected outright; the one part that must not be original.

## Consequences

### Positive

- Every table and column is ours and documented; the dealer model can grow naturally.
- The hash format is the standard one, so a later move to full Identity can read existing hashes.

### Negative

- Lockout, email confirmation, password reset and 2FA are ours to build when needed; until then the gateway's rate limiting (2.5) is the brute-force defence.

## Exit strategy

Because the hash format is ASP.NET Core Identity's own, switching to full Identity is a
data migration of the `users` columns into `AspNetUsers` with no password resets. The
trigger would be needing 2FA or external logins sooner than they can be built well.
