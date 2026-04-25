---
name: Sprint D T-04 Status
description: T-04 TenantSessionInterceptor (is_local=false) CLOSED_DONE — 738 tests passing (2026-04-06)
type: project
---

T-04 TenantSessionInterceptor (BE-P15-03) is CLOSED_DONE.

**Why:** `set_config(is_local=true)` in TenantContextMiddleware only lived per-transaction. Cross-context (AppDbContext + AuditDbContext) requests lost tenant context → RLS bypass.

**Solution implemented:**
- `SpaceOS.Infrastructure/Persistence/TenantSessionInterceptor.cs` — `DbConnectionInterceptor` subclass
  - `ConnectionOpenedAsync`: sets `app.current_tenant_id` at SESSION level (`is_local=false`) using parameterised `set_config($1, $2, false)` — prevents SQL injection via claim manipulation
  - `ConnectionClosingAsync`: resets to empty string on pool return — pool leak prevention
  - Reads `tid` JWT claim via `IHttpContextAccessor`; validates GUID format; no-op if absent or malformed
- `DependencyInjection.cs`: interceptor registered as `Singleton` (production only, not `IsDevelopment()`); both `AppDbContext` and `AuditDbContext` use `.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>())`
- `Program.cs`: `app.UseMiddleware<TenantContextMiddleware>()` removed; replaced with comment explaining the interceptor

**TenantContextMiddleware:** NOT deleted (only unregistered). The source file remains for potential reference.

**Tests added (+13):**
- `SpaceOS.Kernel.Tests/Infrastructure/Persistence/TenantSessionInterceptorTests.cs` — 8 unit tests
  - Constructor null guard
  - ConnectionOpenedAsync: valid tid / no tid / malformed tid / Guid.Empty / no HttpContext
  - ConnectionClosingAsync: always resets / resets even with valid tid
- `SpaceOS.Kernel.Api.Tests/Endpoints/TenantClaimSecurityTests.cs` — 5 API tests
  - X-Tenant-Id header ignored (attacker-supplied)
  - X-Tenant-Id Guid.Empty header ignored
  - JWT without tid claim → 200 (test infra fallback; production enforces via RLS nil-UUID)
  - No Authorization header → 401
  - Invalid JWT → 401

**How to apply:** The interceptor is the authoritative tenant session setter for all production connections. Tests use SQLite so the interceptor is never registered in test environments.

**Final count:** 738 tests passing (575 unit + 98 integration + 65 API). Prior: 725.
