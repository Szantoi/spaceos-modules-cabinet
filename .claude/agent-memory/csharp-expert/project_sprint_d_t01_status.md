---
name: T-01 Sprint D Phase 1.5 Race Condition Load Test + Advisory Lock
description: T-01 CLOSED_DONE — race condition load test, advisory lock XML docs, ADR-005, critical dispatcher bug fix — 688 tests passing (2026-04-06)
type: project
---

T-01 Sprint D Phase 1.5 is CLOSED_DONE. All deliverables complete, 688 tests passing.

**Why:** Verify that InProcessAuditWriteLock correctly serialises concurrent audit chain writes (no forked PreviousHash).

**Deliverables completed:**

1. `SpaceOS.Kernel.IntegrationTests/AuditLog/AuditEventRaceConditionTests.cs` — 2 new tests, 50 concurrent dispatchers each.
2. `SpaceOS.Infrastructure/AuditLog/InProcessAuditWriteLock.cs` — comprehensive XML docs added (single-instance constraint, deployment guidance).
3. `SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs` — comprehensive XML docs (mechanism, spin strategy, limitations).
4. `docs/adr/ADR-005-advisory-lock-audit-chain.md` — full ADR with context, decision, alternatives considered.
5. `SpaceOS.Kernel.Application/SpaceOS.Kernel.Application.csproj` — added `InternalsVisibleTo(SpaceOS.Kernel.IntegrationTests)`.

**Critical bug fixed in AuditEventDispatcher.DispatchAsync:**
`SaveChangesAsync` was called OUTSIDE the lock scope, creating a window where a concurrent reader could read the stale tail hash and produce a forked chain. Fixed by moving `SaveChangesAsync` INSIDE the `foreach (tenantGroup)` block (while the lock is still held). Applies equally to InProcessAuditWriteLock (semaphore) and PostgresAdvisoryAuditWriteLock (xact-scoped advisory lock).

**Test architecture note:**
Race condition tests use a single shared `SqliteConnection` with separate `AppDbContext` per task (via `UseSqlite(_connection)`). This prevents EF Core concurrency-detection errors (each task has its own context) while ensuring all contexts see each other's committed rows (shared physical connection). All 50 tasks share the same `InProcessAuditWriteLock` instance keyed by the same tenant GUID — verifying the semaphore's per-tenant isolation.

**How to apply:**
Any future changes to `AuditEventDispatcher.DispatchAsync` must ensure `SaveChangesAsync` remains inside the lock scope (before the `await using var _` goes out of scope). Moving it outside breaks the chain invariant under concurrent load.
