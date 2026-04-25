using SpaceOS.Cabinet.Abstractions;

namespace SpaceOS.Cabinet.Construction;

/// <summary>
/// Feedback emitted by a construction rule.
/// A11: advisories NEVER block operations — they are always informational only.
/// SEC-CAB-9: <see cref="Message"/> must be template-based; no tenant-specific numeric data.
/// </summary>
/// <param name="RuleId">Identifier of the rule that produced this advisory.</param>
/// <param name="Severity">Indicates urgency; Critical is still non-blocking (A11).</param>
/// <param name="Subject">
/// Scope of the advisory: <c>"Skeleton"</c>, <c>"Part:{guid}"</c>,
/// or <c>"Connection:{guid}"</c>.
/// </param>
/// <param name="Message">
/// Template-based human-readable message (SEC-CAB-9: must not embed tenant-specific numbers).
/// </param>
/// <param name="SuggestedAction">Optional guidance on how to resolve the advisory.</param>
public sealed record DesignAdvisory(
    string RuleId,
    AdvisorySeverity Severity,
    string Subject,
    string Message,
    string? SuggestedAction);
