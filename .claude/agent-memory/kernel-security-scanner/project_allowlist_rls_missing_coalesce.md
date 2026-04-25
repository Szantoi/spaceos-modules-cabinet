---
name: project_allowlist_rls_missing_coalesce
description: TenantHandshakeAllowlist RLS policy throws on empty current_tenant_id — needs COALESCE sentinel pattern (found MSG-K030)
type: project
---

RLS policy on `TenantHandshakeAllowlist` (Migration 0026) does not use the `COALESCE(NULLIF(..., ''), sentinel_uuid)` pattern. When `app.current_tenant_id` is empty (background jobs, health checks, admin paths), casting `''::uuid` raises a PostgreSQL error instead of returning an empty result.

**First seen:** 2026-04-09 (MSG-K030 scan)

**Mitigation:** Migration 0028 needed — drop and recreate the policy using:
```sql
USING (
    "GuestTenantId" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), '')::uuid, '00000000-0000-0000-0000-000000000001'::uuid)
    OR "HostTenantId" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), '')::uuid, '00000000-0000-0000-0000-000000000001'::uuid)
)
```
All other RLS policies in the codebase use this pattern. Re-flag as ERROR until Migration 0028 is applied.
