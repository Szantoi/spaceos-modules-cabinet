---
name: project_db_init_sql_hardcoded_password
description: WARNING (downgraded from ERROR 2026-04-05) — scripts/db-init.sql creates spaceos_audit_writer role without PASSWORD clause. Runtime injection documented in comments. Re-flag as WARNING every scan.
type: project
---

`scripts/db-init.sql` line 11:
```sql
CREATE ROLE spaceos_audit_writer LOGIN;
```

No `PASSWORD` clause is present. A comment block documents the expected runtime injection pattern:
```
-- Post-create: ALTER ROLE spaceos_audit_writer PASSWORD :'AUDIT_WRITER_PASSWORD';
```

## Status as of 2026-04-05

**Severity downgraded from ERROR to WARNING** because:
1. The file has never contained a plaintext password — the role was always created passwordless by design.
2. The runtime injection pattern is documented in the file itself.
3. The deployment runbook (not in this repository) is responsible for running `ALTER ROLE` with a real password before granting access.

**Remaining risk:** Under permissive `pg_hba.conf` settings (e.g. `trust` or `peer` auth), the role can be accessed without credentials until `ALTER ROLE` is executed. This is a deployment process gap, not a code gap.

**Recommended fix (developer action, not security agent):** Change the SQL to accept the password at init time:
```sql
CREATE ROLE spaceos_audit_writer LOGIN PASSWORD :'AUDIT_WRITER_PASSWORD';
```
Supply `AUDIT_WRITER_PASSWORD` as a Docker secret or environment variable at container init.

**First seen:** 2026-04-05, full codebase scan
**Severity:** WARNING (was ERROR)
Re-flag as WARNING every scan until the `PASSWORD` clause is added or approval is documented.
