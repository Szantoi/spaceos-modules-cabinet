using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Assembly;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Assembly;

public class AssemblyStepTests
{
    private static Guid ValidPartId() => Guid.NewGuid();

    // ── Create — success ─────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_Succeeds()
    {
        var result = AssemblyStep.Create(0, "Title", "Instruction", ValidPartId());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_SanitizesMarkdownInstruction()
    {
        var rawInstruction = "<b>Bold</b> [link](http://evil.com) text";

        var result = AssemblyStep.Create(0, "Title", rawInstruction, ValidPartId());

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("<b>", result.Value.SanitizedInstruction);
        Assert.DoesNotContain("http://evil.com", result.Value.SanitizedInstruction);
        Assert.Contains("Bold", result.Value.SanitizedInstruction);
        Assert.Contains("link", result.Value.SanitizedInstruction);
    }

    [Fact]
    public void Create_WithHardware_SetsHardware()
    {
        var hardware = new HardwareReference("BLUM_001", "Blum");

        var result = AssemblyStep.Create(0, "Title", "Instruction", ValidPartId(), hardware: hardware);

        Assert.True(result.IsSuccess);
        Assert.Equal(hardware, result.Value.Hardware);
    }

    [Fact]
    public void Create_WithRequiredTools_SetsTools()
    {
        var tools = new[] { "drill", "screwdriver" };

        var result = AssemblyStep.Create(0, "Title", "Instruction", ValidPartId(), requiredTools: tools);

        Assert.True(result.IsSuccess);
        Assert.Equal(tools, result.Value.RequiredTools);
    }

    [Fact]
    public void Create_WithEstimatedDuration_SetsDuration()
    {
        var duration = TimeSpan.FromMinutes(5);

        var result = AssemblyStep.Create(0, "Title", "Instruction", ValidPartId(), estimatedDuration: duration);

        Assert.True(result.IsSuccess);
        Assert.Equal(duration, result.Value.EstimatedDuration);
    }

    [Fact]
    public void Create_DefaultSanitizer_UsedWhenNull()
    {
        var result = AssemblyStep.Create(0, "Title", "<script>alert(1)</script>Plain", ValidPartId(), sanitizer: null);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("<script>", result.Value.SanitizedInstruction);
        Assert.Contains("Plain", result.Value.SanitizedInstruction);
    }

    // ── Create — validation ──────────────────────────────────────────────────

    [Fact]
    public void Create_WithNullTitle_ReturnsInvalid()
    {
        var result = AssemblyStep.Create(0, null!, "Instruction", ValidPartId());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_WithEmptyInstruction_ReturnsInvalid()
    {
        var result = AssemblyStep.Create(0, "Title", "   ", ValidPartId());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_WithNegativeOrder_ReturnsInvalid()
    {
        var result = AssemblyStep.Create(-1, "Title", "Instruction", ValidPartId());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_WithEmptyPrimaryPartId_ReturnsInvalid()
    {
        var result = AssemblyStep.Create(0, "Title", "Instruction", Guid.Empty);

        Assert.False(result.IsSuccess);
    }

    // ── MarkdownSanitizer ────────────────────────────────────────────────────

    [Fact]
    public void MarkdownSanitizer_RemovesHtmlTags()
    {
        var sanitizer = new MarkdownSanitizer();

        var result = sanitizer.Sanitize("<b>text</b> and <em>more</em>");

        Assert.DoesNotContain("<b>", result);
        Assert.DoesNotContain("<em>", result);
        Assert.Contains("text", result);
        Assert.Contains("more", result);
    }

    [Fact]
    public void MarkdownSanitizer_RemovesLinks_KeepsLabel()
    {
        var sanitizer = new MarkdownSanitizer();

        var result = sanitizer.Sanitize("[click here](http://example.com)");

        Assert.DoesNotContain("http://example.com", result);
        Assert.Contains("click here", result);
    }

    [Fact]
    public void MarkdownSanitizer_RemovesImages_KeepsAlt()
    {
        var sanitizer = new MarkdownSanitizer();

        var result = sanitizer.Sanitize("![alt text](http://example.com/img.png)");

        Assert.DoesNotContain("http://example.com", result);
        Assert.Contains("alt text", result);
    }

    [Fact]
    public void MarkdownSanitizer_RemovesScript()
    {
        var sanitizer = new MarkdownSanitizer();

        var result = sanitizer.Sanitize("javascript:alert(1)");

        Assert.DoesNotContain("javascript:", result);
    }

    [Fact]
    public void MarkdownSanitizer_AllowsHeaders()
    {
        var sanitizer = new MarkdownSanitizer();

        var result = sanitizer.Sanitize("# Heading\n## Sub\n### Sub2");

        Assert.Contains("# Heading", result);
        Assert.Contains("## Sub", result);
    }
}
