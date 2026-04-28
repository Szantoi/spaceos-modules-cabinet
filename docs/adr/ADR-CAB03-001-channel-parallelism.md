# ADR-CAB03-001 — Channel<T> Parallelism in ConstructionRuleEngine

**Date:** 2026-04-28
**Status:** Accepted
**Context:** Cabinet 0.3 BE-01 requirement: ≥30% throughput improvement on multi-rule skeletons.

## Decision
Replace sequential `foreach` in `ApplyAll()` with `Channel<T>` producer-consumer pattern in `ApplyAllAsync()`. Rules execute concurrently (producer via `Task.WhenAll`); result merging is sequential (consumer via `ReadAllAsync`). SEC-CAB-4 timeouts are preserved per-rule and at engine level.

## Consequences
- **+**: Throughput improves proportionally to rule count and CPU cores available.
- **+**: Producer-consumer decoupling prevents lock contention in result aggregation.
- **-**: Rule output ordering is non-deterministic; consumers must not rely on rule execution order.
- **-**: Slightly higher overhead for single-rule or zero-rule engines.

## Alternatives considered
- `Parallel.ForEach` with locking: rejected (lock contention, harder to timeout).
- `Task.WhenAll` with shared list: rejected (shared state mutation requires locks).
