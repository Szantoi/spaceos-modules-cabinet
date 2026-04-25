---
name: Infrastructure config and DbContext visibility
description: CODE agent leaves IEntityTypeConfiguration implementations as `public class` and DbContext as non-sealed.
type: feedback
---

Recurring across multiple tasks: EF Core configuration classes (`IEntityTypeConfiguration<T>`) are introduced as `public class` instead of `internal sealed class`. `DbContext` subclasses are not sealed. Additional persistence-layer types (interceptors, options, value-object records) are introduced as `public`.

**Why recurring:** Code agent follows EF Core samples that use `public` visibility. CLAUDE.md requires `internal sealed` for all Infrastructure configuration and persistence types.

**Standard fix:**
- `IEntityTypeConfiguration<T>` implementations → always `internal sealed class`.
- New `DbContext` subclasses → `public sealed class` if used by test base classes with `protected` members (C# accessibility rules require the type to be at least as accessible as the member); otherwise `internal sealed class`.
- Persistence-layer supporting classes (interceptors, options, entity records) → always `internal sealed class` unless consumed by the API project directly (not via DI).
- When making Infrastructure types `internal`, always add `InternalsVisibleTo` for ALL three test assemblies: `SpaceOS.Kernel.Tests`, `SpaceOS.Kernel.IntegrationTests`, `SpaceOS.Kernel.Api.Tests`.
- When Moq mocks an interface with an `internal` type parameter (e.g. `IDbContextFactory<InternalType>`), Castle DynamicProxy also needs `InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000...")` in the Infrastructure csproj.
Check with: `grep -rn "^public class\|^public sealed class" SpaceOS.Infrastructure/Persistence/ SpaceOS.Infrastructure/Data/Configurations/`
