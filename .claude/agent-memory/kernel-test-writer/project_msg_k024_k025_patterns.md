---
name: project_msg_k024_k025_patterns
description: MSG-K024/K025 handler dependencies, test patterns, and WorkflowPhase import requirement. 491 unit tests after.
type: project
---

## MSG-K024 handler dependencies

**RegisterNodeCommandHandler** — 4 deps: INodeManifestRepository, IUnitOfWork, INodeUrlValidator (from SpaceOS.Modules.Abstractions.Actors), INodeAuthService (from SpaceOS.Modules.Abstractions.Sync).
- SSRF rejection: `INodeUrlValidator.Validate()` returns non-null string → Result.Invalid
- Duplicate: `GetByTenantIdAsync` returns non-null → Result.Conflict
- JWT: `IssueNodeJwtAsync(tenantId, serverUrl, ct)` → included in NodeManifestDto.NodeJwt

**HeartbeatCommandHandler** — 2 deps: INodeManifestRepository, IUnitOfWork.
- Calls `manifest.UpdateHeartbeat()` then `UpdateAsync` + `SaveChangesAsync`

**GetManifestQueryHandler** — 1 dep: INodeManifestRepository.
- NodeJwt is null in query results (only populated on registration)

**ReceiveSyncSignalCommandHandler** — 5 deps: ISyncSignalRepository, IUnitOfWork, ISyncSignalWriteLock, ITransactionManager, ISyncSignalHasher.
- Idempotency check (GetByClientSignalIdAsync) happens BEFORE write-lock acquisition
- ISyncSignalWriteLock.AcquireAsync(Guid tenantId, ct) returns IAsyncDisposable
- ITransactionManager.BeginTransactionAsync(ct) returns IAsyncDisposable
- Both need Mock<IAsyncDisposable> returning ValueTask.CompletedTask for DisposeAsync

## MSG-K025 FlowTask/FlowMilestone/FlowProject patterns

- `WorkflowPhase` lives in `SpaceOS.Modules.Abstractions` — must add `using SpaceOS.Modules.Abstractions;` to test files using it
- FlowTask.Assign(Guid) sets AssigneeId; calling twice overwrites
- FlowMilestone.Complete() sets Status="Completed" AND Phase=WorkflowPhase.ClosedDone
- FlowProject.UpdateDates(start, end) accepts nulls for either parameter

## OfflineQueueService.GetBackoffDelay

Static method: `min(2^retryCount, 60)` seconds.
- retryCount=0 → 1s, 1→2s, 2→4s, 6→60s (2^6=64, capped), 100→60s

## Test count

491 unit tests, 92 integration tests, 46 API tests = 629 total after K024+K025.
