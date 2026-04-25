---
name: MSG-K023 Sprint C Phase 4 Security Deliverables
description: K023 S-01..S-13 security deliverables CLOSED_DONE — 590 tests passing (2026-04-04)
type: project
---

MSG-K023 Sprint C Phase 4 — Security Deliverables S-01..S-13 CLOSED_DONE as of 2026-04-04.

**Implemented:**
- S-01: NodeUrlValidator (SSRF prevention) — `SpaceOS.Infrastructure/Validation/NodeUrlValidator.cs`
- S-02: AesGcmColumnEncryptionService (AES-256-GCM) — `SpaceOS.Infrastructure/Crypto/AesGcmColumnEncryptionService.cs`
- S-02 dep: ConfigKeyVaultService (dev IKeyVaultService) — `SpaceOS.Infrastructure/Crypto/ConfigKeyVaultService.cs`
- S-04: TenantContextMiddleware (parameterised set_config) — `SpaceOS.Infrastructure/Data/TenantContextMiddleware.cs`
- S-05: SyncSignalHasher (HMAC-SHA256 chain) — `SpaceOS.Infrastructure/Crypto/SyncSignalHasher.cs`
- S-08: NodeAuthService (inter-node JWT, RS256 via DevRsaKeyManager) — `SpaceOS.Infrastructure/Auth/NodeAuthService.cs`
- S-09: OfflineQueuePurgeWorker (hourly TTL purge) — `SpaceOS.Infrastructure/Sync/OfflineQueuePurgeWorker.cs`
- S-11: sync-signal rate limit policy (50/min sliding) — added in `Program.cs`
- S-12, S-13: verified satisfied by prior work

**Infrastructure.csproj changes:**
- Added ProjectReference to SpaceOS.Modules.Abstractions
- Added ProjectReference to SpaceOS.Modules.FlowManagement
- Added Microsoft.IdentityModel.Tokens 7.6.0
- Added System.IdentityModel.Tokens.Jwt 7.6.0

**ApiFactory stubs added:** NoOpNodeUrlValidator, TestKeyVaultService, NoOpColumnEncryptionService, NoOpNodeAuthService

**Test count:** 590 passing (452 unit + 92 API integration + 46 integration)

**Why:** Security deliverables for SIP sync layer — SSRF mitigation, column encryption, tenant PG session vars, HMAC hash chain, node JWTs, queue TTL purge.

**How to apply:** Infrastructure now has Abstractions and FlowManagement as direct references — this is intentional for the composition layer. The JWT packages (7.6.0) are pinned at this version to avoid CVE GHSA-59j7-ghrg-fj52 present in 7.0.3.
