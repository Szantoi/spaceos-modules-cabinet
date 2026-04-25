---
name: internal-sealed-validators-not-scanned
description: All Application layer validators are internal sealed; AddValidatorsFromAssembly must use includeInternalTypes:true or validators are silently skipped
type: feedback
---

Rule A8 / G6 — `internal sealed` validators bypassed by default FluentValidation DI scan.

**Why recurring:** The CODE agent consistently declares validators as `internal sealed class` (correct encapsulation) but does not pass `includeInternalTypes: true` to `AddValidatorsFromAssembly`. The `ValidationBehavior<,>` then receives an empty `IEnumerable<IValidator<T>>` and calls `next()` directly, silently accepting invalid input.

**Standard fix:** In `SpaceOS.Kernel.Application/DependencyInjection.cs`, always use:
```csharp
services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
```

**First observed:** E2/T3, 2026-03-24. Affected all 5 list query validators (GetAllTenantsQueryValidator, GetFacilitiesByTenantQueryValidator, GetWorkStationsByFacilityQueryValidator, GetSpaceLayersByFacilityQueryValidator, GetFlowEpicsByFacilityQueryValidator).
