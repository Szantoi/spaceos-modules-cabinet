---
name: project_msg_k016_patterns
description: MSG-K016 Security Sprint 2 — handler signatures, validator gotcha, test patterns for CloseFlowEpic and UploadProof.
type: project
---

## CloseFlowEpicCommandHandler constructor signature (5 dependencies)

```csharp
new CloseFlowEpicCommandHandler(
    IFlowEpicRepository,
    IAggregateSnapshotRepository,
    IOutboxRepository,
    IUnitOfWork,
    IDomainEventDispatcher)
```

**Why:** Handler creates an AggregateSnapshot and an OutboxMessage atomically with the close operation.

**How to apply:** Always mock all 5 in test constructors; verify `_snapshotRepo.AddAsync` and `_outboxRepo.AddAsync` Times.Once on the happy path.

## UploadFlowEpicProofCommandHandler constructor signature (1 dependency)

```csharp
new UploadFlowEpicProofCommandHandler(IImmutableStorage)
```

**Why:** Upload handler is pure I/O delegation — no aggregate mutation, no UoW needed.

**How to apply:** Only mock `IImmutableStorage`; verify `StoreAsync(fileName, stream, ct)` Times.Once.

## URI validator gotcha — ftp:// is a valid absolute URI

`Uri.TryCreate("ftp://...", UriKind.Absolute, out _)` returns `true`.
Do NOT use ftp:// as a negative test case for `ProofUrl` validation.

**Why:** Caught during test run — the validator uses `UriKind.Absolute` which accepts any scheme.

**How to apply:** Use only non-absolute strings like `"not-a-url"` or `"just plain text"` as negative InlineData for ProofUrl.

## WorkflowPhase.ClosedDone = 3

Added in MSG-K016. `FlowEpic.Close()` requires Phase == Delivery; throws DomainException otherwise.
Test both Discovery and ClosedDone as wrong-phase cases.
