# Identity Service

## Owns

Users and their credentials, access-token signing and the published JWKS. From Faz 2.3:
refresh-token families. From 2.4: dealers, the sub-dealer tree, each dealer's price-group
assignment, markup percentage and currency, and permission flags.

## Does not own

Price groups, price lists or how a price is resolved (Catalog) — Identity stores only
*which* group a dealer is in. Full table: `docs/DATA_OWNERSHIP.md`.

## Invariants

- **Only this service holds the private signing key.** Everyone else verifies with the JWKS. Never add a shared secret, and never load the private key anywhere but `SigningKeyStore`.
- **No key in source, no key in an image.** Development generates one into `dev-keys/` (git- and docker-ignored); any other environment must supply `Identity:SigningKeyPath` or the service refuses to start.
- **Login never says which half was wrong** — not in the status, not in the code, not in the wording, and not in the response time. A missing user still costs one password verification (`LoginHandler`); a test pins the identical responses, the devlog holds the timing measurement.
- **Passwords are hashed by `PasswordHasher<T>` only** (ADR-0019). Never log a request body on an auth route, and never put a credential in an exception message.
- **Token validation names exactly one algorithm** (`ValidAlgorithms = [RS256]`) and keeps the library defaults that require a signature. `TokenTests` fails if either is weakened.
- **Claims are hints, not authority** (ADR-0011/0012). Anything a service must trust — permissions, prices — is checked server-side.
- **No MediatR here**, explicit registrations in `Program.cs`, `IIdentityDbContext` never gains a method — the same rules as Catalog.

## Persistence

PostgreSQL `akiron_identity`, snake_case. Emails are stored lower-cased by the `Email`
value object, so the unique index `ix_users_email` is case-insensitive in effect.

New migration:

```powershell
dotnet ef migrations add <Name> `
  --project src/Services/Identity/Akiron.Identity.Infrastructure/Akiron.Identity.Infrastructure.csproj `
  --output-dir Persistence/Migrations
```

## Duplicated from Catalog, on purpose

Serilog/OTel/ProblemDetails wiring, the exception handler, `ValidationEndpointFilter`
and the domain exception types. Slice 2.2 extracts what both services share; until
then, change both copies together.
