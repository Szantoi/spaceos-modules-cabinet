---
name: Security Sprint 2 Status
description: MSG-K016 Security Sprint 2 CLOSED_DONE — P1-3, P1-4, P1-6, P1-8 complete, 397 tests passing (2026-04-03)
type: project
---

MSG-K016 Security Sprint 2 is CLOSED_DONE as of 2026-04-03. All 4 tasks complete, 397 tests passing.

**Tasks completed:**
- P1-3: AggregateSnapshot entity + table (Domain entity, IAggregateSnapshotRepository, EF config, repo, AppDbContext DbSet, DI registration)
- P1-4: Outbox Pattern + SnapshotService — OutboxMessage, IOutboxRepository, FlowEpicClosedEvent, WorkflowPhase.ClosedDone=3, FlowEpic.Close(), CloseFlowEpicCommand + handler + validator, OutboxBackgroundWorker, EF configs, DI registration
- P1-6: Identity-partitioned rate limiting — replaced AddFixedWindowLimiter/AddSlidingWindowLimiter with AddPolicy using GetRateLimitKey(context) returning "{sub}:{tid}" or IP. GetRateLimitKey is a static local function placed BEFORE the partial class Program declaration.
- P1-8: IImmutableStorage abstraction, FileImmutableStorage (dev), AzureImmutableBlobStorage (prod stub — Sprint 3), UploadFlowEpicProofCommand + handler + validator, ProofUploadDto, FlowEpicConfiguration ProofUrl/ProofHash columns, POST /{id}/proof endpoint.

**Key architectural notes:**
- IDomainEvent interface has `OccurredOn` property (not `OccurredAt`) — all events must use this name
- Static local functions in Program.cs top-level statements MUST precede `public partial class Program {}` type declaration
- OutboxBackgroundWorker uses IServiceScopeFactory + CreateAsyncScope for scoped services in a hosted service
- ApiFactory stubs for new interfaces are `file sealed class` (file-scoped) following the existing pattern
- IImmutableStorage is registered as Singleton (not Scoped) because FileImmutableStorage is stateless

**Why:** Sprint 2 adds snapshot integrity, transactional outbox, identity-aware rate limiting, and immutable proof storage as security hardening.
**How to apply:** Sprint 3 tasks include Redis rate limiting, Azure Blob full implementation, and real outbox dispatch to integration bus.
