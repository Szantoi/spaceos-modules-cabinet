---
name: project_joinery_v2_patterns
description: Joinery v2 FSM and snapshot test patterns, SaveCalculationResult handler gap, 172 total tests after.
type: project
---

## DoorOrder FSM test patterns

- `DoorOrderFsmTests.cs` covers all transitions from `MarkCalculating` onward (not Submit — already in `DoorOrderTests.cs`).
- `CreateDraftWithItem()` → `Submit()` → `MarkCalculating()` → `MarkCalculated()` chain used to reach target states.
- Version invariant: starts at 1, each transition increments by 1. Full path Draft→Submitted→Calculating→Calculated→Draft = version 5.
- `MarkCalculationFailed` truncates reason to 2000 chars (`MaxErrorLength` constant). Test with 2500-char string.
- `RevertToDraft` is valid from `Calculated` or `CalculationFailed` only. `Submitted` status also returns Invalid (not just InProduction).
- `PopDomainEvents()` must be called before the act step when testing which event a specific transition raises.

## CuttingListSnapshot test patterns

- Factory throws `ArgumentException` (not a domain-specific exception) for empty/null lines and lines > 200.
- Exact exception message fragments: `"at least one line"` and `"200 lines"`.
- `IsLatest` defaults to `true` on creation; `MarkNotLatest()` sets it to `false`.
- SEC-06 content hash: two snapshots with the same dimensions but different `TenantId` produce different `ContentHash` values. Same TenantId + same inputs = same hash (determinism).
- `ContentHash` is a lowercase hex SHA-256 string — non-empty after creation.

## SaveCalculationResultCommandHandler — not implemented

- `SaveCalculationResultCommand` (record) exists in Application layer but has NO handler class.
- No `ICuttingListSnapshotRepository` interface exists either — the snapshot repository is not yet abstracted.
- Tests for this handler must be written when the handler is implemented.
- Placeholder file: `SpaceOS.Modules.Joinery.Tests/Handlers/SaveCalculationResultHandlerTests.cs`

## Test project conventions (Joinery)

- xUnit v2.5.3 (not v3), no `[Skip]` attribute — use empty class with XML doc comment to defer.
- FluentAssertions 6.12.2 for all assertions.
- `dotnet test` works fine in this project (VSTest runner present). No need for `dotnet exec` workaround.
- Test count after this session: **172 passing**.

**Why:** dotnet test runs fine here — this is the Joinery module, not the Kernel project where the VSTest assembly issue was observed.
**How to apply:** Use `dotnet test` directly for this repo.
