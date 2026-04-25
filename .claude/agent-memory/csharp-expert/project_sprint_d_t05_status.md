---
name: T-05 Sprint D Hash Sink PostgreSQL + DbContextFactory
description: T-05 CLOSED_DONE — HashSinkDbContext + PostgresHashSink + ConnectionStringOptions + migration 0012 — 748 tests passing (2026-04-06)
type: project
---

T-05 Hash Sink PostgreSQL + DbContextFactory CLOSED_DONE.

**Why:** BE-P15-08/09/10 findings — Scoped DbContext fire-and-forget disposal risk, missing startup validation, explicit migration context.

**What was delivered:**
- `HashChainRecord` entity (bigserial PK, EventId UNIQUE)
- `HashSinkDbContext` — `AddDbContextFactory<>` only (not AddDbContext); SQLite-safe (no explicit column types in OnModelCreating)
- `ConnectionStringOptions` — [Required] DefaultConnection, optional AuditWriter + AuditSink, ValidateOnStart()
- `PostgresHashSink : IExternalAuditSink` — replaces AzureImmutableBlobAuditSink in production; DeriveEventId() deterministic dedup; swallows all exceptions; System.Diagnostics.Metrics counters
- Migration 0012 in `Migrations/HashSink/` with explicit `--context HashSinkDbContext`
- `scripts/db/init-audit-sink-roles.sql` — spaceos_sink_writer (INSERT) + spaceos_sink_verifier (SELECT)
- Escrow feature flag: OFF — documented in HashSinkDbContext XML remarks
- TenantSessionInterceptorTests CS8765 nullability fixes (ConnectionString, CommandText, SetParameter overrides)

**Test count:** 748 passing (583 unit + 101 integration + 64 API). Pre-existing failure: ErrorHandlingTests.DomainException_Returns400ProblemDetails (API test, not related to T-05).

**How to apply:**
- Production: register `AddDbContextFactory<HashSinkDbContext>` only when AuditSink connection string is present (non-dev)
- FileExternalAuditSink remains the dev sink
