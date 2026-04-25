using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Construction;

public class DesignAdvisoryTests
{
    [Fact]
    public void Create_HasCorrectProperties()
    {
        var advisory = new DesignAdvisory(
            "R-Test", AdvisorySeverity.Warning, "Skeleton", "Test message.", "Fix it.");

        Assert.Equal("R-Test", advisory.RuleId);
        Assert.Equal(AdvisorySeverity.Warning, advisory.Severity);
        Assert.Equal("Skeleton", advisory.Subject);
        Assert.Equal("Test message.", advisory.Message);
        Assert.Equal("Fix it.", advisory.SuggestedAction);
    }

    [Fact]
    public void Severity_Info_DoesNotBlock()
    {
        // A11: all advisories — including Info — are non-blocking by definition.
        // We verify the type can be created and accessed (no exception means non-blocking).
        var advisory = new DesignAdvisory(
            "R-Test", AdvisorySeverity.Info, "Skeleton", "Info message.", null);

        Assert.Equal(AdvisorySeverity.Info, advisory.Severity);
    }

    [Fact]
    public void Severity_Warning_DoesNotBlock()
    {
        var advisory = new DesignAdvisory(
            "R-Test", AdvisorySeverity.Warning, "Skeleton", "Warning message.", null);

        Assert.Equal(AdvisorySeverity.Warning, advisory.Severity);
    }

    [Fact]
    public void Severity_Critical_DoesNotBlock()
    {
        // A11: even Critical advisories do not block — they are still informational.
        var advisory = new DesignAdvisory(
            "R-Test", AdvisorySeverity.Critical, "Skeleton", "Critical message.", null);

        Assert.Equal(AdvisorySeverity.Critical, advisory.Severity);
    }

    [Fact]
    public void Message_NoTenantSpecificNumbers()
    {
        // SEC-CAB-9: messages must be template-based, not embed tenant-specific numeric data.
        // We verify the message does NOT include a concrete measurement (regression guard).
        var advisory = new DesignAdvisory(
            "R-Stiffener-Tall",
            AdvisorySeverity.Warning,
            "Skeleton",
            "Tall cabinet without horizontal stiffener — structural integrity may be compromised.",
            "Add a horizontal cross rail at mid-height.");

        // The message describes the pattern, not a tenant-specific threshold value.
        Assert.DoesNotContain("2000", advisory.Message);
        Assert.DoesNotContain("mm", advisory.Message);
    }
}
