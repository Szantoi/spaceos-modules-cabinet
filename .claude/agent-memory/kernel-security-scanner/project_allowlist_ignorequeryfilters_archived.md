---
name: project_allowlist_ignorequeryfilters_archived
description: IgnoreQueryFilters() on Tenants join in TenantHandshakeAllowlistRepository leaks archived tenant names into allowed_hosts JWT claim (found MSG-K030)
type: project
---

`TenantHandshakeAllowlistRepository.GetAllowedHostsAsync` at line 26 calls `_context.Tenants.IgnoreQueryFilters()` to join host tenant names. This bypasses the `IsArchived = false` global query filter, so archived hosts appear in the `allowed_hosts` JWT claim.

**First seen:** 2026-04-09 (MSG-K030 scan)

**Mitigation:** Replace `IgnoreQueryFilters()` with `.Where(t => !t.IsArchived)`. No migration needed — query-layer fix only. Re-flag as WARNING until fixed.
