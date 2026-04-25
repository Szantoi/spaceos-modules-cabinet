---
name: project_test_infrastructure
description: Test project layout, ApiFactory pattern, and DatabaseSeedHelper usage for SpaceOS.Kernel.
type: project
---

## Test projects

- `SpaceOS.Kernel.Tests` — xUnit v3, Moq, unit tests only. No HTTP, no EF Core.
- `SpaceOS.Kernel.Api.Tests` — integration tests using `ApiFactory` (WebApplicationFactory + in-memory SQLite). Seeding via `_factory.SeedAsync(db => { ... })`.
- `SpaceOS.Kernel.IntegrationTests` — smoke/integration tests using `SpaceOsApiFactory` + `ApiTestBase` + `DatabaseSeedHelper`.

## ApiFactory (SpaceOS.Kernel.Api.Tests)

**Location:** `SpaceOS.Kernel.Api.Tests/Infrastructure/ApiFactory.cs`

**How to use:**
```csharp
public sealed class MyTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public MyTests() { _factory = new ApiFactory(); _client = _factory.CreateClient(); }
    public async ValueTask InitializeAsync() => await _factory.SeedAsync();
    public async ValueTask DisposeAsync() { _client.Dispose(); await _factory.DisposeAsync(); }
}
```

Seed data per test:
```csharp
await _factory.SeedAsync(db => {
    db.Tenants.Add(Tenant.Create("Name"));
    return Task.CompletedTask;
});
```

## DatabaseSeedHelper (SpaceOS.Kernel.IntegrationTests)

**Location:** `SpaceOS.Kernel.IntegrationTests/Infrastructure/DatabaseSeedHelper.cs`

Typed helpers: `SeedTenantAsync`, `SeedFacilityAsync`, `SeedWorkStationAsync`, `SeedSpaceLayerAsync`, `SeedFlowEpicAsync`.

## Assertions

Project uses `xUnit Assert.*` (not FluentAssertions). Existing tests use `Assert.Equal`, `Assert.NotNull`, `Assert.True`. Do not introduce FluentAssertions.

## PagedList<T>

Deserialize GET list responses as `PagedList<T>` from `SpaceOS.Kernel.Application.Common`. Fields: `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`.

## TradeType enum values

`Joinery=1, Plumbing=2, Electrical=3, Architecture=4, Mep=5` — no `Mechanical` value exists.
