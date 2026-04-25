---
name: Security Sprint 4 Status
description: MSG-K018 Security Sprint 4 CLOSED_DONE — P2-3, P2-6 complete, 495 tests passing (2026-04-03)
type: project
---

MSG-K018 Security Sprint 4 CLOSED_DONE — all 2 tasks complete, 495 tests passing (2026-04-03).

**Why:** GDPR pseudonymization (P2-3) and cryptographic algorithm migration plan (P2-6).

**How to apply:** Both tasks are complete. Future work: actual SHA3-256 migration utility (chain rewrite) deferred to a future sprint per design decision.

## Tasks completed

### P2-3 — GDPR Pseudonymization + PII Separation
- `UserProfile` entity: `SpaceOS.Kernel.Domain/UserProfiles/UserProfile.cs` — maps JWT sub → pseudonym GUID, `Erase()` is idempotent
- `IUserProfileRepository`: Domain interface
- `IPseudonymizer` + `Pseudonymizer`: Application layer — get-or-create per (externalUserId, tenantId)
- `EraseUserCommand` + handler + validator: GDPR erasure flow
- `UserProfileConfiguration`: EF Core config — unique index on (ExternalUserId, TenantId)
- `UserProfileRepository`: Infrastructure EF Core impl
- `AppDbContext`: `UserProfiles` DbSet added
- `AuditEventDispatcher`: now injects `IPseudonymizer`, pseudonymizes ActorId per tenant group
- `POST /api/gdpr/erase-user`: AdminPolicy, 204/404
- Application DI: `IHashProvider` singleton + `IPseudonymizer` scoped
- Infrastructure DI: `IUserProfileRepository` scoped

### P2-6 — Cryptographic Algorithm Migration Plan
- `HashAlgorithmType` enum: SHA256=1, SHA3_256=2 in Domain
- `AuditEvent.HashAlgorithm` property added (default SHA256), constructor updated with `hashAlgorithm` param
- `IHashProvider` + `Sha256HashProvider`: Application layer abstraction over hash algorithm
- `AuditEventDispatcher`: uses `IHashProvider` instead of inline SHA-256
- `ReHashChainCommand` + handler + validator: dry-run returns count of records to re-hash, no data modified
- `AuditEventConfiguration`: `HashAlgorithm` stored as string, max 20, default "SHA256"
- `POST /api/audit-events/re-hash`: AdminPolicy, parses algorithm from string
- Application DI: `IHashProvider` registered as singleton `Sha256HashProvider`

## Test counts
- SpaceOS.Kernel.Tests: 357 (was 441 before sprint 4 additions)
- SpaceOS.Kernel.IntegrationTests: 92
- SpaceOS.Kernel.Api.Tests: 46
- **Total: 495**

## Key design decisions
- `AuditEvent.Create()` signature extended with optional `hashAlgorithm` param (default SHA256) — backward-compatible
- `Pseudonymizer.SaveChangesAsync` called inside `GetOrCreatePseudonymAsync` to ensure pseudonym is committed before the audit dispatcher's own `SaveChangesAsync` — avoids FK issues if UserProfiles table has a FK relationship in future
- Re-hash is dry-run only — actual chain rewrite too dangerous to auto-execute; deferred to future sprint
