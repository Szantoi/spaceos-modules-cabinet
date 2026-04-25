---
name: ct_naming_drift_query_event_handlers
description: Query handlers and event handlers in Application layer still use 'cancellationToken' parameter name instead of project convention 'ct' — identified during E6/T4 scan.
type: project
---

The following files in `SpaceOS.Kernel.Application/` use `CancellationToken cancellationToken` instead of the project convention `CancellationToken ct` (per root CLAUDE.md):

- `Tenants/Queries/GetTenantByIdQueryHandler.cs`
- `SpaceLayers/Queries/GetSpaceLayerByIdQueryHandler.cs`
- `SpaceLayers/Events/SpaceLayerIntentUpdatedEventHandler.cs`
- `SpaceLayers/Events/SpaceLayerRegisteredEventHandler.cs`
- `WorkStations/Events/WorkStationRenamedEventHandler.cs`
- `WorkStations/Events/WorkStationRegisteredEventHandler.cs`
- `WorkStations/Events/WorkStationStatusChangedEventHandler.cs`
- `WorkStations/Events/WorkStationReassignedEventHandler.cs`
- `Facilities/Events/FacilityRenamedEventHandler.cs`
- `Facilities/Events/FacilityCreatedEventHandler.cs`
- `Tenants/Events/TenantCreatedEventHandler.cs`
- `Tenants/Events/TenantRenamedEventHandler.cs`
- `FlowEpics/Events/FlowEpicDelegatedEventHandler.cs`
- `FlowEpics/Events/FlowEpicExecutionStartedEventHandler.cs`
- `FlowEpics/Events/FlowEpicTitleUpdatedEventHandler.cs`
- `FlowEpics/Events/FlowEpicCreatedEventHandler.cs`
- `Events/DomainEventDispatcher.cs`

**First seen:** E6/T4 security scan — 2026-03-27

**Why:** T4 renamed command handlers to `ct` but query handlers and event handlers were not in T4 scope. The tokens are correctly propagated (no `CancellationToken.None` passed anywhere) — this is a naming-convention deviation only, not a functional or security defect.

**Mitigation:** Flag as WARNING in future code reviews. No security risk. Cleanup is a low-priority convention task.
