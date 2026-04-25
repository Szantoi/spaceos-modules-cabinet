---
name: MSG-K025 Sprint C Phase 6 Status
description: MSG-K025 FlowManagement F-01..F-06 CLOSED_DONE — 452 tests passing (2026-04-04)
type: project
---

MSG-K025 Sprint C Phase 6 (Modules.FlowManagement F-01..F-06) is CLOSED_DONE as of 2026-04-04.

All tasks completed:
- F-01: FlowTask enhanced — IFlowNode + ISyncable, Phase property, Complete/Reopen/Assign methods with invariants
- F-02: FlowMilestone enhanced — IFlowNode, Phase property, Complete/UpdateTargetDate
- F-03: FlowProject enhanced — IFlowNode, Phase property, UpdateDates
- F-04: FlowProgram enhanced — IFlowNode, Phase property
- F-05: FlowNodeResolver service (Task → Milestone → Project → Program lookup)
- F-06: OfflineQueueService (EnqueueAsync, GetPendingAsync, RemoveAsync, static GetBackoffDelay)
- Repository interfaces: IFlowTaskRepository, IFlowMilestoneRepository, IFlowProjectRepository, IFlowProgramRepository
- EF configs updated for all four entities (Phase as string conversion, IsSyncedToKernel, LastSyncAt on FlowTask)
- Existing test Reopen_OnOpenTask_StatusRemainsOpen updated to Reopen_OnOpenTask_ThrowsInvalidOperationException (spec change)

Test count: 452 passing, 0 failing, 0 skipped.

**Why:** FlowManagement module Phase 6 adds domain behaviour and sync infrastructure to the hierarchy entities.
**How to apply:** Phase property always defaults to Discovery on Create. Reopen() enforces guard — only from "Completed".
