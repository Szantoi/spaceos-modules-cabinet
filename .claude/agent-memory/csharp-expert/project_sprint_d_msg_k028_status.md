---
name: MSG-K028 Sprint D Phase 3C Status
description: MSG-K028 BrandSkinId on Tenant + brand_skin JWT claim CLOSED_DONE — 915 tests passing (2026-04-07)
type: project
---

MSG-K028 (Phase 3C) is CLOSED_DONE as of 2026-04-07.

**Why:** Add per-tenant brand skin support so the issued JWT carries a `brand_skin` claim for UI theming. Also fixes the `Guid.Empty` tenantId bug in `RefreshTokenCommandHandler`.

**Changes:**
- `Tenant.cs` — `BrandSkinId` property + `SetBrandSkin(string?)` + private parameterless EF Core constructor
- `TenantConfiguration.cs` — `HasMaxLength(64).IsRequired(false)` for `BrandSkinId`
- `IJwtAccessTokenIssuer.cs` — `brandSkinId = null` optional 4th parameter
- `JwtAccessTokenIssuer.cs` — `brand_skin` claim (falls back to `"joinerytech"` when null)
- `RefreshTokenCommandHandler.cs` — resolves `IUserProfileRepository` + `ITenantRepository`, fixes `Guid.Empty` tenantId bug, passes `brandSkin` to issuer
- `ApiFactory.cs` (test stub) — signature updated to match new interface
- Migration `20260407190000_Migration_0024_TenantsBrandSkinId.cs` — `ADD COLUMN "BrandSkinId" character varying(64) NULL`
- `AppDbContextModelSnapshot.cs` — `BrandSkinId` added to Tenants entity
- `RefreshTokenCommandHandlerTests.cs` — 5 existing tests updated for new 5-arg constructor; 2 new brand-skin tests added

**How to apply:** 915 tests passing (746 unit + 101 integration + 68 API). Pre-existing xUnit1051 warnings in `SipVersionMiddlewareTests.cs` are unrelated.
