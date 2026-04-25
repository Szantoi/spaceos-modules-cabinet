---
name: MSG-K018 Security Sprint 4 test patterns
description: UserProfile.Erase() idempotency, Pseudonymizer null guard, ReHashChain empty chain, AuditEventDispatcher HashAlgorithm property. 495 total tests after.
type: project
---

MSG-K018 Security Sprint 4 (P2-3 + P2-6) — implemented by csharp-expert agent, audited by kernel-test-writer.

**Why:** GDPR right-to-erasure (EraseUser) and hash-algorithm upgrade path (ReHashChain) required new domain entity, new application services, and dispatcher changes.

**Key patterns discovered:**

- `UserProfile.Erase()` is explicitly idempotent — `if (IsErased) return;`. Test: `Erase_CalledTwice_IsIdempotent` in `UserProfileTests.cs`.
- `Pseudonymizer.GetOrCreatePseudonymAsync` guards against null/whitespace via `ArgumentException` — **not** a domain exception. `[InlineData(""), InlineData("   ")]` — null is NOT tested because the parameter is `string` (non-nullable).
- `ReHashChainCommandHandler` is a **dry-run read-only operation** — it never calls `AddAsync` or `UpdateAsync`. Empty chain returns `RecordsAffected = 0` correctly.
- `AuditEventDispatcher` constructor now takes 8 parameters: `repository, unitOfWork, requestContext, writeLock, sink, genesisHashProvider, hashProvider, pseudonymizer`.
- `AuditEvent.HashAlgorithm` property is set from `IHashProvider.AlgorithmType` — tested via `AuditEventDispatcher_StoresHashAlgorithmType_FromHashProvider`.
- Solution file is `SpaceOS.Kerner.sln` (not `SpaceOS.Kernel.sln`) — the `Kerner` typo is intentional in the folder/solution name.

**Test counts after MSG-K018:**
- SpaceOS.Kernel.Tests: 357
- SpaceOS.Kernel.Api.Tests: 46
- SpaceOS.Kernel.IntegrationTests: 92
- Total: 495

**How to apply:** When writing future GDPR or audit-chain tests, reuse `UserProfileTests` idempotency pattern and `ReHashChainCommandHandlerTests.BuildChain()` helper for constructing test chains.
