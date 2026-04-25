---
name: Sprint D T-01 Query Endpoints Status
description: T-01 Phase 2 Tool Registry query endpoints — TaskExtensions, GetTenantSummary, 4 list endpoints, migration 0015, RLS script — CLOSED_DONE
type: project
---

T-01 Sprint D Phase 2 (Query Endpoints) CLOSED_DONE — all deliverables complete, 766 tests passing (2026-04-07).

**Why:** Tool Registry LLM integration requires tenant-scoped paged query endpoints for FlowEpics, WorkStations, Facilities, and a summary count endpoint.

**How to apply:** Application handlers use repository+specification pattern (not direct AppDbContext); PagedList<T> lives in Common; ToApiResult() is the extension method name; internal sealed handlers; tenant-scoped specs created in Domain/Specifications.
