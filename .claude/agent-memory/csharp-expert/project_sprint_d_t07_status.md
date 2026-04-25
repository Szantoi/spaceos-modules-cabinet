---
name: T-07 Sprint D Redis RL Hardening
description: T-07 Sprint D Phase 2 Redis RL Hardening CLOSED_DONE — BE-P2-01, BE-P2-07, BE-P2-08 complete
type: project
---

T-07 Sprint D Phase 2 Redis RL Hardening — CLOSED_DONE (2026-04-07)

**Why:** Three security findings — IConnectionMultiplexer registration anti-pattern (BE-P2-01), missing UseForwardedHeaders (BE-P2-08), undocumented in-memory RL limitation (BE-P2-07).

**How to apply:** Reference as completed. Tests at 749 baseline + 5 new = 754 net new passing.

## What was done

- Added `StackExchange.Redis 2.8.*` and `Microsoft.Extensions.Caching.StackExchangeRedis 8.*` to `SpaceOS.Infrastructure`
- Created `SpaceOS.Infrastructure/Extensions/RedisExtensions.cs` — `AddSpaceOsRedis()` extension, singleton multiplexer, in-memory fallback, no `BuildServiceProvider()` anti-pattern
- Updated `SpaceOS.Infrastructure/DependencyInjection.cs` — calls `AddSpaceOsRedis(configuration)` near end of `AddInfrastructureServices`
- Updated `SpaceOS.Kernel.Api/Program.cs` — added `UseForwardedHeaders()` before `UseAuthentication()`, SHA-256 RL partition key, `ForwardedHeadersOptions` with loopback-only KnownProxies
- Created `config/redis-spaceos.conf` — production Redis hardening config
- Created `docs/adr/ADR-007-rl-backing-store.md` — documents in-memory RL backing store limitation
- Created `SpaceOS.Kernel.Tests/Infrastructure/RedisExtensionsTests.cs` — 3 unit tests for fallback behavior
- Created `SpaceOS.Kernel.Api.Tests/Infrastructure/ForwardedHeadersTests.cs` — 2 unit tests for options config

## Key technical note

`ForwardedHeadersOptions` from `Microsoft.AspNetCore.HttpOverrides` is accessible in `Microsoft.NET.Sdk` test projects with `FrameworkReference Include="Microsoft.AspNetCore.App"` — no additional package needed. The `ExponentialRetry` constructor in SE.Redis 2.8 takes positional parameters (not named). `ConfigurationOptions.AbortOnConnectFail` is the correct property name (not `AbortConnect`).

## Test counts (final)

- Unit tests: 586 passing
- Api.Tests: 63 passing (2 pre-existing failures unrelated to T-07)
- IntegrationTests: 98 passing (3 pre-existing failures unrelated to T-07)
