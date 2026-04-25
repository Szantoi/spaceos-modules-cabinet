---
name: T-06 Sprint D IntentDataJson validation + Kestrel 64KB limit
description: T-06 Sprint D Phase 2 IntentDataJson parameters scalar validation + Kestrel body size limit CLOSED_DONE — 777 tests passing (2026-04-07)
type: project
---

T-06 IntentDataJson validation + Kestrel 64 KB limit CLOSED_DONE (2026-04-07).

**Why:** BE-P2-06 — ExternalAuthTokenRef partial index for federated SpaceLayer lookup performance + token-to-KV migration tooling as standalone console project.

**What was delivered:**
- Migration 0014 (`20260407090000_Migration_0014_ExternalAuthTokenPartialIndex.cs`) — already existed; Designer file created to match
- Designer file for 0014 with full BuildTargetModel snapshot (includes RefreshToken from 0013)
- `scripts/MigrateExternalAuthTokens/Shared/TokenEntry.cs` — shared record type
- `scripts/MigrateExternalAuthTokens/Phase1a/` — reads ExternalAuthTokenRef values from DB to tokens.json (KV write stub)
- `scripts/MigrateExternalAuthTokens/Phase1b/` — replaces DB token values with `kv://` references, deletes tokens.json
- `SpaceOS.Kernel.Tests/Infrastructure/Migration0014Tests.cs` — 2 unit tests

**Key design decision:** CONCURRENTLY index requires `suppressTransaction: true` in MigrationBuilder.Sql. Partial index covers only IS NOT NULL rows.

**Test count:** 754 passing (588 unit + 101 integration + 65 API), 0 failed.

**How to apply:**
- Console scripts are NOT in the solution file — build via `dotnet build scripts/MigrateExternalAuthTokens/Phase1a/Phase1a.csproj`
- Run Phase1a first (read-only), verify tokens.json, then run Phase1b (write + delete)
