using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Application;
using SpaceOS.Cabinet.Application.Extensions;
using SpaceOS.Cabinet.Assembly;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Catalog.Infrastructure;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Application;

/// <summary>
/// Tests for <see cref="CabinetServiceCollectionExtensions"/> DI registration.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddCabinetCatalog_RegistersIStaffAuditLogger()
    {
        var services = new ServiceCollection();
        services.AddCabinetCatalog();
        var sp = services.BuildServiceProvider();

        var logger = sp.GetService<IStaffAuditLogger>();

        Assert.NotNull(logger);
    }

    [Fact]
    public void AddCabinetCatalog_RegistersICatalogPayloadValidator()
    {
        var services = new ServiceCollection();
        services.AddCabinetCatalog();
        var sp = services.BuildServiceProvider();

        var validator = sp.GetService<ICatalogPayloadValidator>();

        Assert.NotNull(validator);
    }

    [Fact]
    public void AddCabinetCatalog_IStaffAuditLogger_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddCabinetCatalog();
        var sp = services.BuildServiceProvider();

        var a = sp.GetRequiredService<IStaffAuditLogger>();
        var b = sp.GetRequiredService<IStaffAuditLogger>();

        Assert.Same(a, b);
    }

    [Fact]
    public void AddCabinetAssembly_RegistersAssemblyDocumentationService()
    {
        var services = new ServiceCollection();
        services.AddCabinetAssembly();
        var sp = services.BuildServiceProvider();

        var svc = sp.GetService<AssemblyDocumentationService>();

        Assert.NotNull(svc);
    }

    [Fact]
    public void AddCabinetAssembly_RegistersIMarkdownSanitizer()
    {
        var services = new ServiceCollection();
        services.AddCabinetAssembly();
        var sp = services.BuildServiceProvider();

        var sanitizer = sp.GetService<IMarkdownSanitizer>();

        Assert.NotNull(sanitizer);
    }

    [Fact]
    public void AddCabinetAssembly_AssemblyDocumentationService_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddCabinetAssembly();
        var sp = services.BuildServiceProvider();

        var a = sp.GetRequiredService<AssemblyDocumentationService>();
        var b = sp.GetRequiredService<AssemblyDocumentationService>();

        Assert.Same(a, b);
    }

    [Fact]
    public void AddCabinetCatalog_And_AddCabinetAssembly_NoDuplicates()
    {
        var services = new ServiceCollection();
        services.AddCabinetCatalog();
        services.AddCabinetAssembly();
        var sp = services.BuildServiceProvider();

        // Both registrations should resolve without exception
        var logger = sp.GetRequiredService<IStaffAuditLogger>();
        var svc = sp.GetRequiredService<AssemblyDocumentationService>();

        Assert.NotNull(logger);
        Assert.NotNull(svc);
    }

    [Fact]
    public void NullCatalogResolver_ReturnsEmpty()
    {
        var resolver = NullCatalogResolver.Instance;

        var found = resolver.TryGetPinnedEntry(
            Guid.NewGuid(), Guid.NewGuid(), CatalogType.HorizontalRole, out var entryId);

        Assert.False(found);
        Assert.Equal(Guid.Empty, entryId);
    }

    [Fact]
    public void NullStaffAuditLogger_IsSingleton()
    {
        var a = NullStaffAuditLogger.Instance;
        var b = NullStaffAuditLogger.Instance;

        Assert.Same(a, b);
    }

    [Fact]
    public void AddCabinetCatalog_MultipleRegistrations_NoException()
    {
        var services = new ServiceCollection();
        services.AddCabinetCatalog();
        services.AddCabinetCatalog(); // second call should not throw

        var sp = services.BuildServiceProvider();
        // Last-in-wins for duplicates — should resolve without exception
        var logger = sp.GetService<IStaffAuditLogger>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void AddCabinetAssembly_IMarkdownSanitizer_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddCabinetAssembly();
        var sp = services.BuildServiceProvider();

        var a = sp.GetRequiredService<IMarkdownSanitizer>();
        var b = sp.GetRequiredService<IMarkdownSanitizer>();

        Assert.Same(a, b);
    }

    [Fact]
    public void NullCatalogPayloadValidator_AcceptsAnyPayload()
    {
        var validator = NullCatalogPayloadValidator.Instance;

        var result = validator.Validate(CatalogType.HorizontalRole, "horizontal_role/v1", """{"x":1}""");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void NullCatalogPayloadValidator_IsSingleton()
    {
        var a = NullCatalogPayloadValidator.Instance;
        var b = NullCatalogPayloadValidator.Instance;

        Assert.Same(a, b);
    }

    [Fact]
    public void AddCabinetCatalog_RegistersICatalogPayloadValidator_AsSingleton()
    {
        var services = new ServiceCollection();
        services.AddCabinetCatalog();
        var sp = services.BuildServiceProvider();

        var a = sp.GetRequiredService<ICatalogPayloadValidator>();
        var b = sp.GetRequiredService<ICatalogPayloadValidator>();

        Assert.Same(a, b);
    }

    [Fact]
    public void NullCatalogResolver_Instance_IsReusable()
    {
        var resolver = NullCatalogResolver.Instance;
        // Call twice — should not throw, always returns false
        resolver.TryGetPinnedEntry(Guid.NewGuid(), Guid.NewGuid(), CatalogType.JointType, out _);
        var result = resolver.TryGetPinnedEntry(Guid.NewGuid(), Guid.NewGuid(), CatalogType.HardwareSet, out _);
        Assert.False(result);
    }

    [Fact]
    public void AddCabinetAssembly_MultipleRegistrations_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddCabinetAssembly();
        services.AddCabinetAssembly(); // duplicate

        var sp = services.BuildServiceProvider();
        var svc = sp.GetService<AssemblyDocumentationService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void AddCabinetCatalog_UsesNullStaffAuditLoggerInstance()
    {
        var services = new ServiceCollection();
        services.AddCabinetCatalog();
        var sp = services.BuildServiceProvider();

        var logger = sp.GetRequiredService<IStaffAuditLogger>();

        Assert.IsType<NullStaffAuditLogger>(logger);
    }

    [Fact]
    public void AddCabinetCatalog_UsesNullCatalogPayloadValidatorInstance()
    {
        var services = new ServiceCollection();
        services.AddCabinetCatalog();
        var sp = services.BuildServiceProvider();

        var validator = sp.GetRequiredService<ICatalogPayloadValidator>();

        Assert.IsType<NullCatalogPayloadValidator>(validator);
    }

    [Fact]
    public void AddCabinetAssembly_UsesMarkdownSanitizer()
    {
        var services = new ServiceCollection();
        services.AddCabinetAssembly();
        var sp = services.BuildServiceProvider();

        var sanitizer = sp.GetRequiredService<IMarkdownSanitizer>();

        Assert.IsType<MarkdownSanitizer>(sanitizer);
    }

    // ── AddCabinetFederation ───────────────────────────────────────────────────

    [Fact]
    public void AddCabinetFederation_RegistersICatalogFingerprintExtractor()
    {
        var services = new ServiceCollection();
        services.AddCabinetFederation();
        var sp = services.BuildServiceProvider();

        var extractor = sp.GetService<ICatalogFingerprintExtractor>();

        Assert.NotNull(extractor);
    }

    [Fact]
    public void AddCabinetFederation_ICatalogFingerprintExtractor_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddCabinetFederation();
        var sp = services.BuildServiceProvider();

        var a = sp.GetRequiredService<ICatalogFingerprintExtractor>();
        var b = sp.GetRequiredService<ICatalogFingerprintExtractor>();

        Assert.Same(a, b);
    }

    [Fact]
    public void AddCabinetFederation_UsesDefaultCatalogFingerprintExtractor()
    {
        var services = new ServiceCollection();
        services.AddCabinetFederation();
        var sp = services.BuildServiceProvider();

        var extractor = sp.GetRequiredService<ICatalogFingerprintExtractor>();

        Assert.IsType<DefaultCatalogFingerprintExtractor>(extractor);
    }
}
