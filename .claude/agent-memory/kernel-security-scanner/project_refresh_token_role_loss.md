---
name: project_refresh_token_role_loss
description: RefreshTokenCommandHandler issues rotated access token with hardcoded "User" role and Guid.Empty tenantId — original role and tenant not preserved across token rotation.
type: project
---

`SpaceOS.Kernel.Application/Auth/Commands/RefreshTokenCommandHandler.cs` line 61:

```csharp
var at = _tokenIssuer.GenerateAccessToken(stored.UserId, "User", Guid.Empty);
```

The rotated access token always carries `"User"` role and `Guid.Empty` as tenantId, regardless of what role and tenant the original refresh token was issued for.

**First seen:** MSG-K021 / Sprint D Phase 1.5 scan (2026-04-06)

**Mitigation:** Store `Role` and `TenantId` on the `RefreshToken` entity (or on the user profile), and pass them through when generating the rotated access token. Until fixed, any user who rotates their token loses their original role claim — Admin/Designer users are silently downgraded to "User".
