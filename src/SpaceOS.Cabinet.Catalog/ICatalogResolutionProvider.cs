namespace SpaceOS.Cabinet.Catalog;

using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Resolves the best-matching <see cref="CatalogEntry"/> for a given tenant and catalog type
/// using the 6-level precedence chain (spec §4.1.5).
/// </summary>
public interface ICatalogResolutionProvider
{
    /// <summary>
    /// Returns the highest-priority <see cref="CatalogEntry"/> that matches
    /// <paramref name="type"/> for <paramref name="tenantId"/>.
    /// </summary>
    /// <param name="tenantId">The resolving tenant's identifier.</param>
    /// <param name="type">The catalog type to resolve.</param>
    /// <param name="context">Optional context for skeleton-pin / template-default levels.</param>
    /// <returns>The resolved entry, or an error if none is found.</returns>
    Result<CatalogEntry> Resolve(Guid tenantId, CatalogType type, CatalogResolutionContext context);
}
