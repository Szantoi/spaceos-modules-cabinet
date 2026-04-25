---
name: Repository internal visibility blocked by integration test direct instantiation
description: Making AuditEventRepository `internal sealed` breaks integration tests that directly instantiate the concrete class.
type: project
---

`AuditEventRepository` should be `internal sealed class` per the I6 visibility rule but cannot be made internal without breaking `SpaceOS.Kernel.IntegrationTests/AuditLog/AuditEventRepositoryTests.cs`, which directly instantiates the concrete class rather than injecting via the `IAuditEventRepository` interface.

**Why:** Integration tests bypass DI and new up repositories directly — common in integration test layers that want to test the repository in isolation. Making the class internal requires either:
1. Adding `[assembly: InternalsVisibleTo("SpaceOS.Kernel.IntegrationTests")]` to `SpaceOS.Infrastructure.csproj`, or
2. Refactoring the integration test to resolve `IAuditEventRepository` from the DI container instead.

**Approved by:** Pending — flagged as UNFIXABLE in REVIEW_REPORT_MSG_K020_SPRINTD_PHASE1.md

**Scope:** `SpaceOS.Infrastructure/Data/Repositories/AuditEventRepository.cs` — first seen MSG-K020 Sprint D Phase 1 (2026-04-06).
