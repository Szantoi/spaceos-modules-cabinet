---
name: Security Sprint 3 Status
description: MSG-K017 Security Sprint 3 CLOSED_DONE — P2-1 chain verify, P2-2 snapshot API, P2-4 anomaly detection, P2-5 genesis hash (2026-04-03)
type: project
---

MSG-K017 Security Sprint 3 CLOSED_DONE — 441 tests passing (2026-04-03).

Four tasks completed:

**P2-5 — IGenesisHashProvider:** New interface in Application/Common. Two infra impls:
- `ConfigurationGenesisHashProvider` (dev) — reads `AuditChain:GenesisHash` from config, generates ephemeral if absent
- `KeyVaultGenesisHashProvider` (prod) — scoped-resolves ISecretProvider via IServiceScopeFactory to avoid captive dependency
- `AuditEventDispatcher` now replaces "GENESIS" sentinel with provider value

**P2-1 — Chain Integrity Verifier:** `GET /api/audit-events/verify-chain?tenantId=&from=&to=`
- `ExternalAuditHashRecord` record + `ReadHashesAsync` added to `IExternalAuditSink`
- `GetChainAsync` added to `IAuditEventRepository`
- `FileExternalAuditSink.ReadHashesAsync` parses `{occurredAt:O}|{tenantId}|{eventType}|{prevHash}→{stateHash}` log lines
- `VerifyChainQuery/Handler/Validator` in Application/AuditLog/Queries
- Requires AdminPolicy; returns `ChainVerificationResultDto`

**P2-2 — Snapshot Query API:** `GET /api/snapshots/{aggregateId}?at=` and `/versions`
- `GetAtTimestampAsync` added to `IAggregateSnapshotRepository`
- `SnapshotDto`, `GetSnapshotAtQuery/Handler/Validator`, `GetSnapshotVersionsQuery/Handler/Validator` in Application/Snapshots/Queries
- `SnapshotEndpoints.cs` registered in Program.cs

**P2-4 — Anomaly Detection:**
- `IAlertService` in Application/Common
- `AuditAnomalyDetector` (scoped service) in Application/AuditLog/Anomaly — checks AuditGap (10min), BurstClosedDone (>10 in 5min), ChainBreak
- `LoggingAlertService` (dev), `WebhookAlertService` (prod, reads `Alerting:WebhookUrl`) in Infrastructure/Alerting
- `AnomalyDetectionBackgroundWorker` polls every 60s, iterates all tenants via `AllTenantsSpec`
- `AddHttpClient(nameof(WebhookAlertService))` registered for prod webhook delivery

**Why:** captive dependency fix for KeyVaultGenesisHashProvider — registered as Singleton but ISecretProvider is Scoped, so uses IServiceScopeFactory.
**How to apply:** All new Singleton providers that depend on Scoped services must resolve via IServiceScopeFactory.
