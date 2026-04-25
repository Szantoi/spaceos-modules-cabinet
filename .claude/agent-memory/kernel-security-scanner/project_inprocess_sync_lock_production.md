---
name: project_inprocess_sync_lock_production
description: RESOLVED (2026-04-05) — InProcessSyncSignalWriteLock replaced with PostgresAdvisorySyncSignalWriteLock in production DI branch. Do not re-flag.
type: project
---

`SpaceOS.Infrastructure/DependencyInjection.cs` production `else` branch.

**RESOLVED 2026-04-05:** `InProcessSyncSignalWriteLock` replaced with `PostgresAdvisorySyncSignalWriteLock` at line 141. New file created at `SpaceOS.Infrastructure/Sync/PostgresAdvisorySyncSignalWriteLock.cs`.

Lock key offset `0x5398_0000L` added to tenant hash to avoid collision with `PostgresAdvisoryAuditWriteLock` keys in the same PostgreSQL advisory lock namespace.

Build: 0 errors. Tests: 635 passing.

**First seen:** 2026-04-04 (Sprint C Phase 1-3 scan)
**Resolved:** 2026-04-05, full re-scan
**Do not re-flag** unless the production DI branch is reverted to `InProcessSyncSignalWriteLock`.
