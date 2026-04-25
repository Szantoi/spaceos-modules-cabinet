---
name: project_jwt_authority_silent_fallback
description: ConfigureJwtBearerOptions silently falls back to hardcoded issuer when JWT_AUTHORITY env var and Jwt:Issuer config key are both absent (found MSG-K031)
type: project
---

`ConfigureJwtBearerOptions.cs` lines 44–50 fall back to `"https://spaceos-kernel"` when neither `JWT_AUTHORITY` nor `Jwt:Issuer` is configured. Unlike connection strings (which throw on startup), JWT authority misconfiguration is silent. A missing `JWT_AUTHORITY` in production would silently use the fallback issuer string.

**First seen:** 2026-04-09 (MSG-K031 scan)

**Mitigation:** Add a startup guard in non-Development environments — throw `InvalidOperationException` if both config sources are absent. Consistent with `BE-P15-10` fail-fast pattern for connection strings. Re-flag as WARNING until fixed.
