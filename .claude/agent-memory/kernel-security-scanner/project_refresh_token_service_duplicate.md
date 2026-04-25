---
name: project_refresh_token_service_duplicate
description: RefreshTokenService in Infrastructure duplicates RefreshTokenHasher in Application — divergence risk; Infrastructure copy appears unused after MSG-K021.
type: project
---

Two near-identical implementations of the same utility exist:

- `SpaceOS.Infrastructure/Auth/RefreshTokenService.cs` — `GenerateOpaqueToken`, `HashToken`, `VerifyToken`
- `SpaceOS.Kernel.Application/Auth/RefreshTokenHasher.cs` — same three methods, same logic

CQRS handlers reference `RefreshTokenHasher` (Application layer). `RefreshTokenService` (Infrastructure) is no longer referenced in any production path after MSG-K021.

**First seen:** MSG-K021 / Sprint D Phase 1.5 scan (2026-04-06)

**Mitigation:** Remove `SpaceOS.Infrastructure/Auth/RefreshTokenService.cs`. The Application layer version is the authoritative implementation. If Infrastructure needs the helpers for any future use, it should reference `RefreshTokenHasher` directly (Application is already visible to Infrastructure per layer dependency rules).
