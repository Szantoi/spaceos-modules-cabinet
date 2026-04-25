---
name: project_msg_k021_patterns
description: MSG-K021 Sprint C Phase 2 — NodeManifest, SyncSignal, B2BHandshake test patterns. 401 total unit tests after.
type: project
---

**NodeManifest** (`Domain/Federation/NodeManifest.cs`):
- `Create(TenantId, string serverUrl)` factory — throws `ArgumentException` (not `DomainException`) for empty/whitespace URL (uses `ArgumentException.ThrowIfNullOrWhiteSpace`)
- Default `Version` = 1; `LastHeartbeatAt` = null after Create
- `UpdateHeartbeat()` sets `LastHeartbeatAt`, increments `Version`, updates `UpdatedAt`
- Test file: `SpaceOS.Kernel.Tests/Entities/NodeManifestTests.cs`

**SyncSignal** (`Domain/Sync/SyncSignal.cs`):
- `Create(FlowEpicId, TenantId, newState, stateHash, previousHash, clientSignalId)`
- Throws `ArgumentException` (not `DomainException`) for empty/whitespace `newState`, `stateHash`, `previousHash`
- `IsSyncedToKernel` defaults to `false`; `ExpiresAt = now.AddDays(SyncConstants.OfflineQueueTtlDays)` where `OfflineQueueTtlDays = 30`
- `MarkSynced()` idempotent — sets `IsSyncedToKernel = true`
- `SyncConstants.GenesisHash = "GENESIS"` — valid value for `previousHash`
- Test file: `SpaceOS.Kernel.Tests/Entities/SyncSignalTests.cs`

**B2BHandshake** (`Domain/ValueObjects/B2BHandshake.cs`):
- `sealed record` with positional constructor `(TenantId guestTenantId, DateTimeOffset delegatedOn)`
- Sprint C added 4 nullable `init`-only properties: `InitiatorAnchorJson`, `ResponsibleAnchorJson`, `VisibilityScope`, `ContractHash` — all default to null
- Record equality includes all properties including nullable Sprint C fields
- Test file: `SpaceOS.Kernel.Tests/ValueObjects/B2BHandshakeTests.cs`

**Total unit tests after MSG-K021:** 401
**dotnet test result:** 401 passing, 0 failed
