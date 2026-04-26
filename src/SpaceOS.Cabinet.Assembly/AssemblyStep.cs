namespace SpaceOS.Cabinet.Assembly;

using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Machining;

/// <summary>
/// A single ordered step in the assembly sequence for a cabinet skeleton (A14).
/// Immutable once created; use <see cref="Create"/> to construct.
/// </summary>
public sealed class AssemblyStep
{
    /// <summary>Zero-based position of this step in the full assembly sequence.</summary>
    public int Order { get; }

    /// <summary>Short human-readable title for the step.</summary>
    public string Title { get; }

    /// <summary>Sanitized (SEC-CAB02-3) assembly instruction in markdown format.</summary>
    public string SanitizedInstruction { get; }

    /// <summary>The part primarily assembled in this step.</summary>
    public Guid PrimaryPartId { get; }

    /// <summary>Connection IDs that must be completed as part of this step.</summary>
    public IReadOnlyList<Guid> RequiredConnectionIds { get; }

    /// <summary>Optional hardware required for this step (e.g. hinges, dowels).</summary>
    public HardwareReference? Hardware { get; }

    /// <summary>Tools required to perform this step (e.g. "drill", "screwdriver").</summary>
    public IReadOnlyList<string> RequiredTools { get; }

    /// <summary>Estimated time to complete this step.</summary>
    public TimeSpan EstimatedDuration { get; }

    /// <summary>Optional skill level required (e.g. "Basic", "Advanced").</summary>
    public string? RequiredSkillLevel { get; }

    private AssemblyStep(
        int order,
        string title,
        string sanitizedInstruction,
        Guid primaryPartId,
        IReadOnlyList<Guid> requiredConnectionIds,
        HardwareReference? hardware,
        IReadOnlyList<string> requiredTools,
        TimeSpan estimatedDuration,
        string? requiredSkillLevel)
    {
        Order = order;
        Title = title;
        SanitizedInstruction = sanitizedInstruction;
        PrimaryPartId = primaryPartId;
        RequiredConnectionIds = requiredConnectionIds;
        Hardware = hardware;
        RequiredTools = requiredTools;
        EstimatedDuration = estimatedDuration;
        RequiredSkillLevel = requiredSkillLevel;
    }

    /// <summary>
    /// Creates a new <see cref="AssemblyStep"/>, sanitizing the instruction markdown.
    /// </summary>
    /// <param name="order">Zero-based step order. Must be &gt;= 0.</param>
    /// <param name="title">Step title. Required.</param>
    /// <param name="rawInstruction">Raw markdown instruction. Required. Will be sanitized via <paramref name="sanitizer"/>.</param>
    /// <param name="primaryPartId">Part being assembled in this step. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="requiredConnectionIds">Connections completed in this step, or <c>null</c> for empty.</param>
    /// <param name="hardware">Optional hardware item for this step.</param>
    /// <param name="requiredTools">Tools needed, or <c>null</c> for empty.</param>
    /// <param name="estimatedDuration">Estimated step duration, or <c>null</c> for <see cref="TimeSpan.Zero"/>.</param>
    /// <param name="requiredSkillLevel">Skill level string, or <c>null</c>.</param>
    /// <param name="sanitizer">Markdown sanitizer; defaults to <see cref="MarkdownSanitizer"/> when <c>null</c>.</param>
    public static Result<AssemblyStep> Create(
        int order,
        string title,
        string rawInstruction,
        Guid primaryPartId,
        IReadOnlyList<Guid>? requiredConnectionIds = null,
        HardwareReference? hardware = null,
        IReadOnlyList<string>? requiredTools = null,
        TimeSpan? estimatedDuration = null,
        string? requiredSkillLevel = null,
        IMarkdownSanitizer? sanitizer = null)
    {
        if (order < 0)
            return Result<AssemblyStep>.Invalid(new ValidationError("Order must be >= 0."));

        if (string.IsNullOrWhiteSpace(title))
            return Result<AssemblyStep>.Invalid(new ValidationError("Title is required."));

        if (string.IsNullOrWhiteSpace(rawInstruction))
            return Result<AssemblyStep>.Invalid(new ValidationError("Instruction is required."));

        if (primaryPartId == Guid.Empty)
            return Result<AssemblyStep>.Invalid(new ValidationError("PrimaryPartId must not be empty."));

        var effectiveSanitizer = sanitizer ?? new MarkdownSanitizer();
        var sanitized = effectiveSanitizer.Sanitize(rawInstruction);

        return Result<AssemblyStep>.Success(new AssemblyStep(
            order,
            title,
            sanitized,
            primaryPartId,
            requiredConnectionIds ?? Array.Empty<Guid>(),
            hardware,
            requiredTools ?? Array.Empty<string>(),
            estimatedDuration ?? TimeSpan.Zero,
            requiredSkillLevel));
    }
}
