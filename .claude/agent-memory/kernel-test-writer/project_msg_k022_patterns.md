---
name: project_msg_k022_patterns
description: MSG-K022 Sprint C Phase 3 DB migrations — FlowTask/Milestone/Project/Program/OfflineSyncQueueItem test patterns. 452 total unit tests after.
type: project
---

**Module location:** `SpaceOS.Modules.FlowManagement` (project prefix is `SpaceOS.`, not bare `Modules.`).

**Test project:** Tests live in `SpaceOS.Kernel.Tests/Entities/Modules/` — a new `Modules/` sub-folder added to the existing `Entities/` folder. A `<ProjectReference>` to `SpaceOS.Modules.FlowManagement` was added to `SpaceOS.Kernel.Tests.csproj`.

**All five domain types throw `ArgumentException` (not `DomainException`) for empty/whitespace guard clauses** — consistent with NodeManifest/SyncSignal precedent from K021.

**FlowTask:**
- `Create(epicKernelId, name, tenantId, milestoneId?)` — optional `milestoneId` defaults to null
- `Status` defaults to `"Open"` after Create; `Complete()` sets `"Completed"`; `Reopen()` sets `"Open"` (idempotent on already-open task)

**FlowMilestone:**
- `Create(name, projectId, tenantId)` — `Status = "Open"`, `TargetDate = null`

**FlowProject:**
- `Create(name, tenantId, programId?)` — `ProgramId`, `Description`, `StartDate`, `EndDate` all null after Create

**FlowProgram:**
- `Create(name, tenantId, description?)` — `Description` null if omitted, set if passed

**OfflineSyncQueueItem:**
- `Create(tenantId, payload, clientSignalId)` — `ExpiresAt = UtcNow.AddDays(30)`, `CreatedAt = UtcNow`
- TTL tolerance: `ExpiresAt - CreatedAt` is 30 days within 1-second clock tick tolerance (two separate `DateTimeOffset.UtcNow` calls inside `Create()`)

**Total unit tests after MSG-K022:** 452
**dotnet test result:** 452 passing, 0 failed
