---
name: project_inode_url_validator_missing
description: INodeUrlValidator (SSRF guard for NodeManifest.ServerUrl) has no concrete implementation — ERROR finding introduced in Sprint C Phase 1 (MSG-K020). Re-flag every scan until implemented and registered.
type: project
---

`INodeUrlValidator` is declared in `SpaceOS.Modules.Abstractions/Actors/INodeUrlValidator.cs` as the primary SSRF-prevention boundary for node federation URLs.

**First seen:** 2026-04-04 (Sprint C Phase 1-3 scan, MSG-K020/K021/K022)

**Location:** Interface only — no concrete implementation exists anywhere in the solution. Not registered in `SpaceOS.Infrastructure/DependencyInjection.cs`.

**Risk:** Any command handler calling `NodeManifest.Create(tenantId, serverUrl)` with an API-supplied URL has no SSRF guard. When the federation execution layer begins making outbound HTTP calls using `NodeManifest.ServerUrl`, this becomes directly exploitable.

**Required mitigation:**
- Implement `INodeUrlValidator` (enforce HTTPS, block RFC-1918, loopback, link-local ranges)
- Register in DI
- Call from all NodeManifest creation/update command validators before `NodeManifest.Create()`

**How to apply:** Re-flag as ERROR in every subsequent scan until a concrete implementation is found in the solution. Check via `grep -r "INodeUrlValidator" --include="*.cs"` — the finding is resolved only when a non-interface `class` implementing it exists and is registered.
