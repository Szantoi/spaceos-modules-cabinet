---
name: SpaceOS.Kernel project overview
description: Key architectural patterns, conventions, and domain facts for the SpaceOS.Kernel solution
type: project
---

SpaceOS.Kernel is a Clean Architecture + DDD + CQRS solution on .NET 8 LTS.

**Layer boundaries (hard):** Domain ← Application ← Infrastructure. Domain has zero NuGet deps.

**Five aggregates:** Tenant, Facility, WorkStation, SpaceLayer, FlowEpic — all except Tenant carry `TenantId { get; init; }` (set in private constructor via factory method). Global EF Core query filters on all 4 tenant-scoped aggregates; `null` resolver return = Admin bypass.

**Domain event dispatch pattern:** handlers call `aggregate.PopDomainEvents()` then `IDomainEventDispatcher.DispatchAsync(events, ct)` after `IUnitOfWork.SaveChangesAsync`. The dispatcher (`DomainEventDispatcher`) uses MediatR `IPublisher`. Registered as Scoped in Application DI.

**Test projects:**
- `SpaceOS.Kernel.Tests` — xUnit v3, Moq, unit tests
- `SpaceOS.Kernel.IntegrationTests` — xUnit v3, `WebApplicationFactory<Program>`, in-memory SQLite
- `SpaceOS.Kernel.Api.Tests` — xUnit v3, endpoint-level tests (has many pre-existing xUnit1030/xUnit1051 analyzer warnings — do not treat as regressions)

**Integration test infrastructure (E1/T5 + E3/T1):**
- `SpaceOsApiFactory` — singleton `EventCaptureService` replaces scoped `IDomainEventDispatcher`; exposes `Capture` property (type `IEventCapture`)
- `ApiTestBase` — `IAsyncLifetime`, creates `SpaceOsApiFactory` + `HttpClient` per test class
- `DatabaseSeedHelper` — static, writes directly to `AppDbContext` (bypasses repositories by design for speed)
- `RepositoryTestBase` — abstract, `IAsyncLifetime`, per-test in-memory SQLite `AppDbContext`, no HTTP host; used by T2a/T2b/T2c

**API request types:** Endpoints accept `*Request` records (e.g. `CreateTenantRequest`), not commands directly.

**CancellationToken convention:** always named `ct`.

**Test runner fix (applied 2026-03-25, E3/T2b):** All three test projects were missing `Microsoft.NET.Test.Sdk` and used `xunit.runner.visualstudio 2.8.2` (v2 adapter, incompatible with xunit.v3). Fixed by adding `Microsoft.NET.Test.Sdk 17.8.0` and upgrading to `xunit.runner.visualstudio 3.0.1` in all three projects. After the fix `dotnet test` works normally. Do NOT revert these package changes.

**Current test counts (2026-03-27, after E6/T2):** Unit: 35, API: 90, Integration: 194. Total: 319. 0 failed.

**SpaceLayer factory methods:** `CreateLocalLayer(intentDataJson, facilityId, tradeType, tenantId)` and `CreateExternalLayer(externalSourceUrl, facilityId, tradeType, tenantId)`. The task spec calls the second one `CreateFederatedLayer` — the real name is `CreateExternalLayer`.

**ITenantRepository has no ExistsByNameAsync.** Only `IFacilityRepository` has `ExistsByNameAsync(TenantId tenantId, string name, ct)`. The task spec mentions this for Tenant but it does not exist on the interface — do not attempt to add it.

**FlowEpic domain facts:** Mutation methods: `Create(title, facilityId, tenantId)`, `UpdateTitle(newTitle)`, `StartExecution()`, `DelegateTo(guestTenantId)`. Property for facility is `TargetFacilityId` (not `FacilityId`). Handshake property is `Handshake` (not `B2BHandshake`). `DelegateTo` only works in `WorkflowPhase.Discovery`; `StartExecution` throws if already in `Delivery`.

**B2BHandshake VO:** `sealed record`, not `readonly record struct`. Properties: `GuestTenantId` (TenantId) and `DelegatedOn` (DateTimeOffset). EF Core maps it as owned entity under `FlowEpic` via `OwnsOne` with nullable navigation.

**AggregateRoot.DomainEvents:** The base class has NO `DomainEvents` public property — events are accessed via `GetDomainEvents()` (internal) and `PopDomainEvents()` (public). `builder.Ignore(t => t.DomainEvents)` would not compile. CLAUDE.md I5 rule references a pattern that does not apply to this codebase's aggregate base class.

**dotnet test note:** `dotnet test SpaceOS.Kerner.sln` works when `DOTNET_ROOT=/home/gabor/.dotnet` is set and `/home/gabor/.dotnet` is on `PATH`. The `dotnet` binary lives at `/home/gabor/.dotnet/dotnet`. The `dotnet-ef` tool lives at `/home/gabor/.dotnet/tools/dotnet-ef` and requires `DOTNET_ROOT` to be set. All 319 tests pass (confirmed 2026-03-27, E6/T2).

**E3/T2a status:** CLOSED_DONE. `FacilityRepositoryTests.cs` created with 8 tests. `TenantRepositoryTests.cs` was pre-existing with 5 tests.

**E3/T2b status:** CLOSED_DONE. `WorkStationRepositoryTests.cs` (7 tests) and `SpaceLayerRepositoryTests.cs` (7 tests) created.

**E3/T2c status:** CLOSED_DONE. `FlowEpicRepositoryTests.cs` created with 9 tests covering all CRUD, B2BHandshake owned-entity round-trip, execution state, spec-filtered list, and AsNoTracking verification.

**E4/T1 status:** CLOSED_DONE. `ITenantResolver` interface created at `SpaceOS.Kernel.Domain/Auth/ITenantResolver.cs`, namespace `SpaceOS.Kernel.Domain.Auth`. `TenantId` namespace is `SpaceOS.Kernel.Domain.ValueObjects` (not `Tenants/`). Zero csproj changes. 308 tests pass.

**E4/T5 status:** CLOSED_DONE. JWT test infrastructure complete. `JwtTokenHelper` (IntegrationTests) and `JwtTestHelper` (Api.Tests) read signing key from `appsettings.Testing.json`. `AuthApiTestBase` extends `ApiTestBase` with `GenerateToken`/`CreateClientForRole`. `ClaimsStubTenantResolver` reads `tid` claim from JWT. `PostConfigure<JwtBearerOptions>` overrides validation in both factories. 5 new auth tests in `Auth/AuthorizationTests.cs`. All 318 tests pass.

**E4/T4 status:** CLOSED_DONE. AppDbContext global query filters + EF Core migration generated. Key facts: (1) Domain aggregates already had TenantId from prior work; (2) TenantId flows via command property, not re-resolved in handlers; (3) `ApiFactory` (Api.Tests) needed `NullTenantResolver` stub — created `SpaceOS.Kernel.Api.Tests/Infrastructure/NullTenantResolver.cs`; (4) `Microsoft.EntityFrameworkCore.Design` added as PrivateAssets=all to `SpaceOS.Kernel.Api.csproj` for migration tooling; (5) Migration `20260327090458_AddTenantIdToWorkStationSpaceLayerFlowEpic` is a first migration (no prior ones existed) — creates all tables from scratch.

**E6/T2 status:** CLOSED_DONE. `InitialCreate` Npgsql migration generated at `SpaceOS.Infrastructure/Migrations/20260327194934_InitialCreate.cs`. All 5 tables present, all IDs are `uuid`, Name columns are `character varying`, no DomainEvents column. `AggregateRoot` has no public `DomainEvents` property — the task spec's `builder.Ignore(t => t.DomainEvents)` pattern does not apply; the private `_domainEvents` field is invisible to EF Core by design. `README.md` created at solution root with Database Setup section. 319 tests passing.

**E3/T3 status:** CLOSED_DONE. 5 pipeline test classes created (Tenant, Facility, WorkStation, SpaceLayer, FlowEpic), 84 integration tests total. Key routing facts: WorkStation is registered via `POST /api/facilities/{facilityId}/work-stations`; SpaceLayer via `POST /api/facilities/{facilityId}/space-layers`; FlowEpic via `POST /api/facilities/{facilityId}/flow-epics`; Facility via `POST /api/tenants/{tenantId}/facilities`. SpaceLayer GET/PUT use `/api/space-layers/`. FlowEpic GET/PUT use `/api/flow-epics/`.

**Pre-existing bug in ResultExtensions (not fixed in T3):** `ToCreatedResult` and `ToApiResult` use `ToDictionary` on `ValidationErrors` — throws `ArgumentException` if a field has two validator rules that both fire (e.g. NotEmpty + Must on same property with null input). Workaround: in tests, send non-empty invalid values so only one rule fires per field. Should be fixed (use `GroupBy` + `SelectMany`) in a separate task.

**Why:** Project is in active development under epic E3 (integration tests). Test infrastructure was extended 2026-03-24; T2a completed 2026-03-25.

**How to apply:** Always check `TenantEndpoints.cs` for actual request shape before writing HTTP test clients. Use `RepositoryTestBase` for repository-layer tests, `ApiTestBase` for full HTTP pipeline tests.
