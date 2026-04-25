---
name: project_jwt_packages_unapproved
description: Microsoft.IdentityModel.Tokens 7.6.0, System.IdentityModel.Tokens.Jwt 7.6.0, and Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11 added in MSG-K021 are not yet on the CLAUDE.md approved list.
type: project
---

The following packages were added to support ES256 JWT in Sprint D Phase 1.5 but are not listed in the approved package list in `CLAUDE.md`:

| Package | Version | Added in |
|---------|---------|---------|
| `Microsoft.IdentityModel.Tokens` | 7.6.0 | SpaceOS.Infrastructure |
| `System.IdentityModel.Tokens.Jwt` | 7.6.0 | SpaceOS.Infrastructure |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.11 | SpaceOS.Kernel.Api |

No known CVEs at these versions. All are first-party Microsoft packages.

**First seen:** MSG-K021 / Sprint D Phase 1.5 scan (2026-04-06)

**Mitigation:** Add all three to the approved package list in `/opt/spaceos/SpaceOS.Kerner/CLAUDE.md`. Re-flag as WARNING on every scan until the list is updated.
