---
name: appsettings_baked_into_image
description: RESOLVED (E7/T1 final re-scan 2026-03-27) — appsettings.Testing.json is now excluded from the Docker build context via **/*Testing* in .dockerignore. Do not re-flag.
type: project
---

**Resolution (final re-scan 2026-03-27):**

`SpaceOS.Kernel.Api/appsettings.Testing.json` — RESOLVED. The `.dockerignore` pattern `**/*Testing*` (line 9) was added and confirmed present. The filename contains `Testing` and is matched by this glob. The file is excluded from the Docker build context. The JWT test signing key no longer enters the image.

`SpaceOS.Kernel.Api/appsettings.json` — RESOLVED (prior scan). `Jwt.SigningKey` is `""`. No committed credential.

**First seen:** E7/T1 initial scan (2026-03-27)
**Resolved:** E7/T1 final re-scan (2026-03-27) via `**/*Testing*` added to `.dockerignore`

**Do not re-flag** this finding in future Docker-related scans unless:
- The `.dockerignore` pattern is removed, OR
- A new `appsettings.*.json` file with a non-empty signing key is added to `SpaceOS.Kernel.Api/` with a name that does not contain `Testing`.
