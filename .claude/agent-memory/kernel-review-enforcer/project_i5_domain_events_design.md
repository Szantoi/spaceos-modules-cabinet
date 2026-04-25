---
name: i5-domain-events-no-property
description: Rule I5 builder.Ignore(DomainEvents) is inapplicable in this codebase — AggregateRoot has no DomainEvents property
type: project
---

Rule I5 states `builder.Ignore(t => t.DomainEvents)` must appear in every aggregate EF Core configuration. In this codebase the rule is satisfied by design without that call.

**Why:** `AggregateRoot` stores domain events in `private readonly List<IDomainEvent> _domainEvents`. There is no public or protected property named `DomainEvents`. EF Core cannot map a private field by convention, so no `DomainEvents` column is ever generated. The strongly-typed lambda `builder.Ignore(t => t.DomainEvents)` is a compile error (CS1061).

**Approved by:** Verified during E6/T2 review (2026-03-27) — `Migration_DoesNotContainDomainEventsColumn` test passes as evidence.

**Scope:** All five aggregate configurations: `TenantConfiguration`, `FacilityConfiguration`, `WorkStationConfiguration`, `SpaceLayerConfiguration`, `FlowEpicConfiguration`.

**How to apply:** Do not attempt to add `builder.Ignore(t => t.DomainEvents)` to any configuration. Log I5 as PASS-BY-DESIGN in the review report and cite this memory entry.
