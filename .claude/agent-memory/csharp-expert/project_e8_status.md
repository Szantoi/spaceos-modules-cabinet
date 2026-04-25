---
name: E8 Audit Log Status
description: E8 Audit Log / Domain Event Persistence — CLOSED_DONE, all 4 tasks complete, 346 tests passing (2026-03-28)
type: project
---

E8 Audit Log epic is CLOSED_DONE as of 2026-03-28. All 4 tasks complete. 346 tests passing.

**Why:** Append-only audit log for every domain event mutation, with SHA-256 tamper detection and a paged read API.

**How to apply:** E8 is complete. Do not re-implement AuditEvent, AuditEventDispatcher, AuditEventRepository, or the audit endpoint unless explicitly asked to modify them.

Tasks:
- T1: AuditEvent domain entity + IAuditEventRepository — CLOSED_DONE
- T2: AuditEventDispatcher + SHA-256 hashing — CLOSED_DONE
- T3: EF Core migration + AuditEventRepository implementation — CLOSED_DONE
- T4: GET /api/audit-events endpoint + GetAuditEventsQuery/Handler/Validator — CLOSED_DONE

Key patterns established in E8:
- Validator unit tests use a local mirror validator + ValidationBehavior<,> (internal validators are not directly instantiable from test projects)
- AuditEventsByTenantPagedSpec extends PagedSpecification<T> with optional date range params
- AuditEventsByTenantFilterSpec is a bare Specification<AuditEvent> used for CountAsync (no Skip/Take)
- AuditEventDto deliberately excludes Payload — security requirement, never revert
