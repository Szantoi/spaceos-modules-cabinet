---
name: D4 — new aggregates missing domain events
description: CODE agent introduces new AggregateRoot subclasses without any AddDomainEvent calls in Create() or mutation methods.
type: feedback
---

Rule D4 violated on every new aggregate in MSG-K020/K021 (NodeManifest, SyncSignal).
Both `Create()` (factory) and every mutating method (`UpdateHeartbeat`, `MarkSynced`) had zero `AddDomainEvent(...)` calls.

**Why recurring:** CODE agent focuses on persistence plumbing (EF configs, repos) and forgets the domain event contract when adding aggregates in new feature areas (Federation, Sync).

**Standard fix:** For every new `AggregateRoot` subclass, immediately verify:
1. `Create()` raises an `<Entity>CreatedEvent`.
2. Every method that modifies a property raises an `<Entity><Mutation>Event`.
Events are `readonly record struct : IDomainEvent`. Flag as UNFIXABLE — requires developer decision on handler wiring.
