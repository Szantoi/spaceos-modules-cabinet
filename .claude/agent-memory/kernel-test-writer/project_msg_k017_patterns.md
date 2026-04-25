---
name: project_msg_k017_patterns
description: MSG-K017 test patterns: VerifyChain, AuditAnomalyDetector, Snapshot handlers — mock setup and reflection trick for AuditEvent.OccurredAt.
type: project
---

**MSG-K017 Security Sprint 3 — test patterns established 2026-04-03.**

## AuditEvent.OccurredAt is set by `DateTimeOffset.UtcNow` inside `AuditEvent.Create()`.

To build deterministic chains in tests, force the value via reflection:

```csharp
var prop = typeof(AuditEvent).GetProperty(nameof(AuditEvent.OccurredAt));
prop!.SetValue(auditEvent, desiredTimestamp);
```

**Why:** OccurredAt has a private setter; no factory overload accepts it. The property has no backing field separate from the auto-property, so reflection on the property works.

## IReadOnlyList mock setup pattern

`List<T>.AsReadOnly()` returns a `ReadOnlyCollection<T>` which implements `IReadOnlyList<T>`.
The `as` cast is safe and avoids an explicit typed local:

```csharp
.ReturnsAsync(new List<AuditEvent> { ev }.AsReadOnly() as IReadOnlyList<AuditEvent>);
```

## AuditAnomalyDetector — CountAsync is called twice per DetectAnomaliesAsync

First call uses `AuditEventsByTenantFilterSpec(tenantId, null, windowStart, now)` — the AuditGap check.
Second call uses `AuditEventsByTenantFilterSpec(tenantId, "FlowEpicClosedEvent", windowStart, now)` — BurstClosedDone check.

Both are mocked via `It.IsAny<ISpecification<AuditEvent>>()`. Use a call-index counter in the mock callback to return different values per call when testing only one alert:

```csharp
var callIndex = 0;
_repository
    .Setup(r => r.CountAsync(It.IsAny<ISpecification<AuditEvent>>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(() => { callIndex++; return callIndex == 1 ? 1 : BurstClosedDoneThreshold + 1; });
```

## VerifyChainQueryValidator — DateRange error uses WithName("DateRange")

The rule-level error is on the synthetic property `"DateRange"`, not on `x.From` or `x.To`.
In FluentValidation TestHelper: `result.ShouldHaveValidationErrorFor("DateRange")`.

## New test files written in this sprint

- `SpaceOS.Kernel.Tests/AuditLog/VerifyChainQueryHandlerTests.cs` — 5 tests
- `SpaceOS.Kernel.Tests/Validators/VerifyChainQueryValidatorTests.cs` — 4 tests
- `SpaceOS.Kernel.Tests/Application/GetSnapshotAtQueryHandlerTests.cs` — 2 tests
- `SpaceOS.Kernel.Tests/Application/GetSnapshotVersionsQueryHandlerTests.cs` — 3 tests
- `SpaceOS.Kernel.Tests/AuditLog/AuditAnomalyDetectorTests.cs` — 6 tests

Total new: 20 tests. Solution total after: 462 (324 unit + 92 integration + 46 API).
