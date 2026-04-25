---
name: microsoft_data_sqlite_tests
description: Microsoft.Data.Sqlite 8.0.11 in Api.Tests and IntegrationTests is not on the approved package list — test-only, no known CVE, flag in every supply chain scan until removed or approved.
type: project
---

`Microsoft.Data.Sqlite` Version 8.0.11 is present in:
- `SpaceOS.Kernel.Api.Tests/SpaceOS.Kernel.Api.Tests.csproj`
- `SpaceOS.Kernel.IntegrationTests/SpaceOS.Kernel.IntegrationTests.csproj`

It is not on the CLAUDE.md approved package list, but is a test infrastructure dependency that enables in-memory SQLite for `WebApplicationFactory`-based integration tests.

**First seen:** E2/T3 security scan — 2026-03-24
**Mitigation:** Test-only, zero production exposure, no known CVE. Flag as WARNING in every supply chain scan until the package is either added to the approved list in CLAUDE.md or replaced with an approved alternative.
