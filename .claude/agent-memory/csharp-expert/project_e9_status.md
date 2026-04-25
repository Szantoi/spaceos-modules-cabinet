---
name: E9 Rate Limiting Status
description: E9 Rate Limiting / Throttling — CLOSED_DONE, both tasks complete, 350 tests passing (2026-03-28)
type: project
---

E9 Rate Limiting epic is CLOSED_DONE as of 2026-03-28. Both tasks complete. 350 tests passing.

**Why:** Protect API against abuse and DoS using ASP.NET Core built-in rate limiting (no external NuGet).

**How to apply:** E9 is complete. Rate limiting is active in production. Do not re-register policies unless changing limits.

Tasks:
- T1: AddRateLimiter policies + UseRateLimiter middleware — CLOSED_DONE
- T2: RateLimitTests integration tests (RateLimitTestFactory) — CLOSED_DONE

Key patterns established in E9:
- "fixed" policy (100 req/60s): applied to all GET endpoints via `.RequireRateLimiting("fixed")`
- "sliding" policy (20 req/60s): applied to all POST/PUT endpoints via `.RequireRateLimiting("sliding")`
- /healthz is exempt via `.DisableRateLimiting()`
- OnRejected writes RFC 7807 ProblemDetails + Retry-After: 60 header
- Test override uses file-scoped RateLimitTestFactory with 3 req/10s limits
- To override rate limiter in a test factory: remove all IConfigureOptions<RateLimiterOptions> descriptors first, then call AddRateLimiter again
- AddRateLimiter extension is in Microsoft.AspNetCore.Builder namespace (NOT Microsoft.Extensions.DependencyInjection)
- Test project needs <FrameworkReference Include="Microsoft.AspNetCore.App" /> to access AddRateLimiter from Microsoft.NET.Sdk (non-web) test project
