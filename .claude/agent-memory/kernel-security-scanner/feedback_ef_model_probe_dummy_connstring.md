---
name: EF Core model-probe dummy connection string is accepted risk
description: UseNpgsql with dummy credentials in unit tests that only inspect IModel metadata is an accepted WARNING — no live connection is opened, credentials are inert.
type: feedback
---

Rule: When a test file calls `UseNpgsql("...Username=x;Password=x")` or equivalent dummy credentials solely to construct a `DbContext` for `context.Model` inspection (never executing a query), flag as WARNING but accept risk.

**Why:** EF Core builds the full model graph at `DbContext` construction time without opening a database connection. Tests that only call `context.Model.FindEntityType(...)` and similar `IModel` APIs never trigger a connection. The dummy credentials have no attack surface.

**How to apply:** Check whether the test calls any query method (`.ToListAsync`, `.FirstOrDefaultAsync`, `.FindAsync`, etc.). If the only operations are on `context.Model`, accept the risk. If an actual query is executed, escalate to ERROR — real credentials would be required and must not be hardcoded.

First seen: E6/T3 — `SpaceOS.Kernel.Tests/Infrastructure/SpaceLayerConfigurationTests.cs` line 24.
