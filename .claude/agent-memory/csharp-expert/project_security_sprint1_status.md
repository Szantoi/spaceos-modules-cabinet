---
name: Security Sprint 1 Status
description: MSG-K015 Security Sprint 1 — P0-2, P1-1, P1-2, P1-5 complete, 362 tests passing (2026-04-03)
type: project
---

MSG-K015 Security Sprint 1 CLOSED_DONE — all 4 tasks complete, 362 tests passing as of 2026-04-03.

Tasks completed:
- P0-2: IExternalAuditSink + FileExternalAuditSink (dev) + AzureImmutableBlobAuditSink (prod stub) + AuditEventDispatcher wired + ApiFactory NoOp stubs
- P1-1: HttpTenantResolver renamed to ClaimsTenantResolver; old file deleted; DI updated; tests updated + 5 new ClaimsTenantResolverTests
- P1-2: ExternalAuthToken → ExternalAuthTokenRef on SpaceLayer domain entity + EF config + ISecretProvider + InMemorySecretProvider (dev) + KeyVaultSecretProvider (prod stub)
- P1-5: IntentDataSchemaValidator (per-TradeType structural JSON validation) + UpdateSpaceLayerIntentDataCommand gains TradeType? param + validator updated + endpoint request updated

**Why:** Security sprint mandated by architectural decisions in project_security_decisions.md
**How to apply:** Sprint 2 will implement real Azure Blob and Azure Key Vault backends for the stubs added here.
