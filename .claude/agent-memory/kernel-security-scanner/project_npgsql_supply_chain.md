---
name: project_npgsql_supply_chain
description: Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11 added in E6/T1 — not on original approved package list; no CVE; flag until REVIEW_CHECKLIST.md is updated.
type: project
---

`Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11` was introduced in `SpaceOS.Infrastructure.csproj` as part of E6/T1 (Switch Infrastructure to Npgsql).

**First seen:** E6/T1 (2026-03-27)

**Why flagged:** Not on the original approved package list in CLAUDE.md (`MediatR · FluentValidation · Ardalis.Result · Ardalis.Specification · EF Core 8 · xUnit v3 · Moq · Swashbuckle.AspNetCore · Microsoft.AspNetCore.Mvc.Testing · FluentAssertions`). T1 AC explicitly calls for adding it to `REVIEW_CHECKLIST.md`.

**CVE status:** No known CVE as of 2026-03-27 for version 8.0.11.

**Mitigation:** Flag as WARNING in every supply chain scan until the package appears on the approved list. Once REVIEW_CHECKLIST.md is updated and approved, stop flagging.

**Also note:** `Microsoft.EntityFrameworkCore.Sqlite 8.0.11` remains in `SpaceOS.Kernel.Api.Tests` and `SpaceOS.Kernel.IntegrationTests` as the test-layer SQLite override — this is intentional per T1 design (test projects keep SQLite in-memory for integration test isolation).
