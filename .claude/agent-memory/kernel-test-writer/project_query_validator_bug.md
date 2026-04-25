---
name: query_validator_bug
description: All 5 list query validators are declared 'internal sealed' and are never registered by AddValidatorsFromAssembly, so 422 validation is silently bypassed for GET list endpoints.
type: project
---

All query pagination validators (`GetAllTenantsQueryValidator`, `GetFacilitiesByTenantQueryValidator`, `GetWorkStationsByFacilityQueryValidator`, `GetSpaceLayersByFacilityQueryValidator`, `GetFlowEpicsByFacilityQueryValidator`) are declared `internal sealed class` in `SpaceOS.Kernel.Application`.

`AddValidatorsFromAssembly` in `DependencyInjection.cs` only registers **public** types by default. These validators are never injected into `ValidationBehavior`, so invalid pagination params (page=0, pageSize=101) silently pass through and return 200 OK instead of 422.

**Why:** Discovered 2026-03-24 while writing T3 endpoint tests. Unit tests for the behavior pass because they construct the validator manually. End-to-end integration tests fail.

**Fix (production code change required):** Either change validators to `public`, or add `includeInternalTypes: true` to `AddValidatorsFromAssembly` call in `DependencyInjection.cs`.

**How to apply:** When writing 422 tests for GET list endpoints, expect these to fail until the bug is fixed. Skip with `[Fact(Skip = "Production code bug: internal query validators not registered...")]` and reference this note.
