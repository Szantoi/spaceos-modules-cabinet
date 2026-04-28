namespace SpaceOS.Cabinet.Tests.Infrastructure;

using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Application;
using SpaceOS.Cabinet.Domain;

/// <summary>
/// In-memory test double implementing both the read-side <see cref="ITenantStandardRepository"/>
/// and the write-side <see cref="ITenantStandardWriteRepository"/>.
/// </summary>
internal sealed class InMemoryTenantStandardRepository
    : ITenantStandardRepository, ITenantStandardWriteRepository
{
    private readonly Dictionary<Guid, TenantStandard> _store = new();

    public Task AddAsync(TenantStandard standard, CancellationToken ct = default)
    {
        _store[standard.Id] = standard;
        return Task.CompletedTask;
    }

    public Task<TenantStandard?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var s) ? s : null);

    public Task UpdateAsync(TenantStandard standard, CancellationToken ct = default)
    {
        _store[standard.Id] = standard;
        return Task.CompletedTask;
    }

    public Task<TenantStandardSnapshot?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        var standard = _store.Values.FirstOrDefault(s => s.TenantId == tenantId);
        return Task.FromResult(standard is null ? null : ToSnapshot(standard));
    }

    public Task<IReadOnlyList<TenantStandardSnapshot>> ListByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        var list = _store.Values
            .Where(s => s.TenantId == tenantId)
            .Select(ToSnapshot)
            .ToList();
        return Task.FromResult<IReadOnlyList<TenantStandardSnapshot>>(list);
    }

    private static TenantStandardSnapshot ToSnapshot(TenantStandard s) =>
        new(
            s.Id,
            s.TenantId,
            s.Materials.CarcassMaterial,
            s.Materials.CarcassThicknessMm,
            s.Materials.BackPanelMaterial,
            s.Materials.BackPanelThicknessMm,
            s.LineBore.Enabled,
            s.LineBore.FirstHoleOffsetMm,
            s.LineBore.SpacingMm,
            s.LineBore.DiameterMm,
            s.Thresholds.TallCabinetHeightMm,
            s.Thresholds.LongShelfMm,
            s.RuleSeverityOverrides.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString()));
}
