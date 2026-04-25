---
name: joinery_jwt_audience_disabled
description: ValidateAudience=false in Joinery Program.cs — cross-service token replay possible
type: project
---

`SpaceOS.Modules.Joinery.Api/Program.cs:31` has `ValidateAudience = false` with comment "internal-only API; issuer+sig validated". Audience value `kernel-api` is configured but not enforced.

**First seen:** 2026-04-15, Joinery full security audit
**Mitigation:** Enable `ValidateAudience = true` once confirmed that Kernel JWT issuance includes the `aud` claim. Coordinate with Kernel terminal. Re-flag as MEDIUM every scan until enabled.
