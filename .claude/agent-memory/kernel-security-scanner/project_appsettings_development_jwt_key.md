---
name: project_appsettings_development_jwt_key
description: RESOLVED (2026-04-03) — appsettings.Development.json JWT key finding is closed. File now contains only SQLite DSN and is excluded from git tracking.
type: project
---

Previous CRITICAL (first seen E28, 2026-03-31): `appsettings.Development.json` contained a hardcoded JWT signing key `"AIzaSyAmVVu7TvjuZiAII2kYZkgVKhGHmhUH1Xk"`.

**Resolved:** 2026-04-03 (MSG-K014..K018 full-scope re-scan).

Current state verified:
- File content: only `"DefaultConnection": "Data Source=SpaceOS.dev.db"` (SQLite DSN, no JWT key)
- `git ls-files` returns empty — file is NOT tracked in source control
- `.gitignore` explicitly lists `appsettings.Development.json`

**Do not re-flag this specific finding.** If a new JWT key or credential appears in this file in a future scan, create a new entry.
