using Ardalis.Result;

namespace SpaceOS.Cabinet.Machining;

/// <summary>
/// Represents a single machining operation on a cabinet part or connection.
/// Created exclusively via <see cref="Create"/> — never directly instantiated.
/// </summary>
public sealed record MachiningFeature
{
    /// <summary>Unique identifier of this feature.</summary>
    public Guid Id { get; init; }

    /// <summary>The geometry that this machining targets (face, edge, or connection).</summary>
    public MachiningSubject Subject { get; init; }

    /// <summary>The type of machining operation to perform.</summary>
    public MachiningOperation Operation { get; init; }

    /// <summary>Numeric and geometric parameters for the operation.</summary>
    public MachiningParameters Parameters { get; init; }

    /// <summary>Optional hardware item associated with this feature (e.g. the hinge being mounted).</summary>
    public HardwareReference? Hardware { get; init; }

    // Private parameterless constructor for the factory — prevents external instantiation.
    private MachiningFeature()
    {
        Subject = null!;
        Parameters = null!;
    }

    /// <summary>
    /// Creates a validated <see cref="MachiningFeature"/>.
    /// </summary>
    /// <param name="subject">The geometry target (required).</param>
    /// <param name="operation">The machining operation type.</param>
    /// <param name="parameters">Numeric parameters for the operation (required).</param>
    /// <param name="hardware">Optional hardware catalog reference.</param>
    /// <returns>Success with the new feature, or Invalid with validation errors.</returns>
    public static Result<MachiningFeature> Create(
        MachiningSubject subject,
        MachiningOperation operation,
        MachiningParameters parameters,
        HardwareReference? hardware = null)
    {
        if (subject is null)
            return Result<MachiningFeature>.Invalid(new ValidationError("Subject is required."));
        if (parameters is null)
            return Result<MachiningFeature>.Invalid(new ValidationError("Parameters are required."));
        if (hardware is not null && !hardware.IsValid())
            return Result<MachiningFeature>.Invalid(
                new ValidationError("Hardware reference has invalid CatalogId or CatalogType."));

        return Result<MachiningFeature>.Success(new MachiningFeature
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Operation = operation,
            Parameters = parameters,
            Hardware = hardware
        });
    }
}
