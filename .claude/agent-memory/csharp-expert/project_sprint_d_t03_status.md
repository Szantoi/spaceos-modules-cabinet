---
name: T-03 Sprint D JWT ES256 + RefreshToken + OutputCache
description: T-03 CLOSED_DONE — ES256 migration, RefreshToken CQRS vertikum, OutputCache — 725 tests passing (2026-04-06)
type: project
---

T-03 CLOSED_DONE — all DoD items complete, 725 tests passing (567 unit + 98 integration + 60 API).

**Why:** SEC-P15-06 ES256; BE-P15-01 BuildServiceProvider removal; BE-P15-02/04/05/11/12 RefreshToken full stack; BE-P15-06 OutputCache.

**How to apply:** When touching auth, note the architecture decisions below.

## Key Decisions

- `ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>` lives in **SpaceOS.Kernel.Api** (not Infrastructure) — JwtBearer package only in Api.
- `ISigningKeyProvider` + `LocalEcKeyProvider` in `SpaceOS.Infrastructure/Auth/` — registered as Singleton in Program.cs.
- `RefreshToken` entity lives in **Domain** (`SpaceOS.Kernel.Domain/Auth/RefreshToken.cs`) — same pattern as AuditEvent; EF config in Infrastructure.
- `IRefreshTokenRepository` lives in **Domain** — handlers reference it via Domain layer.
- `RefreshTokenHasher` (GenerateOpaqueToken/HashToken/VerifyToken) lives in **Application** — pure BCL, no NuGet deps, accessible to handlers.
- `IJwtAccessTokenIssuer` interface in Application; `JwtAccessTokenIssuer` implementation in Infrastructure.
- `AppDbContext.RefreshTokens` DbSet added; `RefreshTokenConfiguration` in `Data/Configurations/`.
- Migration 0013 written manually (PostgreSQL types) — ef migrations tool generates SQLite types in dev which would corrupt the existing PostgreSQL snapshot.
- OutputCache registered in Program.cs, `UseOutputCache()` after `UseRateLimiter()`.
- JWKS endpoint at `/.well-known/jwks.json` uses `JsonWebKeyConverter.ConvertFromECDsaSecurityKey`.
- `ApiFactory` stubs: `NoOpRefreshTokenRepository`, `TestEcKeyProvider`, `TestJwtAccessTokenIssuer`.
- `PostConfigure<JwtBearerOptions>` in ApiFactory still overrides to RS256 for tests — the production ES256 config is bypassed safely.
- appsettings.json updated: `RsaPublicKeyPem` removed, `PrivateKeyPath`/`PublicKeyPath` added.
