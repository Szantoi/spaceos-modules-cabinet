namespace SpaceOS.Cabinet.Application.Extensions;

using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Assembly;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Catalog.Infrastructure;

/// <summary>
/// Extension methods for registering SpaceOS Cabinet services with <see cref="IServiceCollection"/>.
/// </summary>
public static class CabinetServiceCollectionExtensions
{
    /// <summary>
    /// Registers catalog-related services:
    /// <list type="bullet">
    ///   <item><see cref="NullStaffAuditLogger"/> as singleton <see cref="IStaffAuditLogger"/>.</item>
    ///   <item><see cref="NullCatalogPayloadValidator"/> as singleton <see cref="ICatalogPayloadValidator"/>.</item>
    /// </list>
    /// Consumer must register <see cref="ICatalogEntryRepository"/> and optionally override
    /// <see cref="IStaffAuditLogger"/> with a real implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCabinetCatalog(this IServiceCollection services)
    {
        services.AddSingleton<IStaffAuditLogger>(NullStaffAuditLogger.Instance);
        services.AddSingleton<ICatalogPayloadValidator>(NullCatalogPayloadValidator.Instance);
        return services;
    }

    /// <summary>
    /// Registers assembly documentation services:
    /// <list type="bullet">
    ///   <item><see cref="MarkdownSanitizer"/> as singleton <see cref="IMarkdownSanitizer"/>.</item>
    ///   <item><see cref="AssemblyDocumentationService"/> as singleton.</item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCabinetAssembly(this IServiceCollection services)
    {
        services.AddSingleton<IMarkdownSanitizer, MarkdownSanitizer>();
        services.AddSingleton<AssemblyDocumentationService>();
        return services;
    }

    /// <summary>
    /// Registers federation-related services:
    /// <list type="bullet">
    ///   <item><see cref="DefaultCatalogFingerprintExtractor"/> as singleton <see cref="ICatalogFingerprintExtractor"/>.</item>
    /// </list>
    /// Consumer must register <see cref="ITenantStandardWriteRepository"/> and <see cref="ITenantStandardRepository"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCabinetFederation(this IServiceCollection services)
    {
        services.AddSingleton<ICatalogFingerprintExtractor, DefaultCatalogFingerprintExtractor>();
        return services;
    }
}
