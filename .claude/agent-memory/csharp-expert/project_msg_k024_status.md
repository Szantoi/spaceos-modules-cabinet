---
name: MSG-K024 Sprint C Phase 5 status
description: MSG-K024 NodeManifest API + SIP middleware + EF Core config — CLOSED_DONE, 590 tests passing (2026-04-04)
type: project
---

MSG-K024 Sprint C Phase 5 CLOSED_DONE — all deliverables complete, 590 tests passing.

**Why:** Adds the federation node-management API, sync-signal ingestion endpoint, and SIP versioning enforcement.

**How to apply:** Future work on node federation or sync should follow the patterns established here (ISyncSignalHasher interface, SipVersionMiddleware, node rate-limit policies).

## Completed

- Application/Nodes/Commands/RegisterNode — RegisterNodeCommand + Validator + Handler
- Application/Nodes/Commands/Heartbeat — HeartbeatCommand + Validator + Handler
- Application/Nodes/Queries — GetManifestQuery + Validator + Handler
- Application/Nodes/NodeManifestDto.cs
- Application/Sync/ISyncSignalHasher.cs (interface extracted from Infrastructure)
- Application/Sync/Commands/ReceiveSignal — ReceiveSyncSignalCommand + Validator + Handler (BE-02 + BE-03)
- Infrastructure/Crypto/SyncSignalHasher — now implements ISyncSignalHasher
- Infrastructure/DependencyInjection — registers ISyncSignalHasher
- Api/Endpoints/NodeEndpoints.cs — POST /api/nodes/register (AdminPolicy, node-register RL), PUT /api/nodes/heartbeat (WritePolicy, node-heartbeat RL), GET /api/nodes/{tenantId}/manifest (ReadPolicy, fixed RL)
- Api/Endpoints/SyncEndpoints.cs — POST /api/sync/signal (WritePolicy, sync-signal RL)
- Api/Middleware/SipVersionMiddleware.cs — BE-06, enforces SpaceOS-SIP-Version header on /api/sync/* and /api/nodes/*
- Program.cs — node-register (10/min) + node-heartbeat (120/min) rate-limit policies, SipVersionMiddleware registration, MapNodeEndpoints + MapSyncEndpoints
- Api.Tests/Infrastructure/ApiFactory.cs — NoOpSyncSignalHasher stub
- SpaceOS.Kernel.Application.csproj — added ProjectReference to SpaceOS.Modules.Abstractions (needed for INodeUrlValidator + INodeAuthService in Application handler)
