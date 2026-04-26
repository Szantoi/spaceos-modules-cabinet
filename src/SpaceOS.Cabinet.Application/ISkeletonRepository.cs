namespace SpaceOS.Cabinet.Application;

using SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>Repository contract for <see cref="Skeleton"/> persistence (implemented by the consumer).</summary>
public interface ISkeletonRepository
{
    /// <summary>Retrieves a <see cref="Skeleton"/> by its unique identifier, or <c>null</c> if not found.</summary>
    Task<Skeleton?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Persists mutations to an existing <see cref="Skeleton"/>.</summary>
    Task UpdateAsync(Skeleton skeleton, CancellationToken ct = default);
}
