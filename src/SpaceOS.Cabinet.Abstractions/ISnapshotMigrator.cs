using Ardalis.Result;

namespace SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Migrates a serialised Skeleton snapshot from one schema version to another.
/// Implement one migrator per version step (e.g. "0.1" → "0.2").
/// </summary>
public interface ISnapshotMigrator
{
    /// <summary>
    /// Returns <c>true</c> if this migrator can handle the given version pair.
    /// </summary>
    /// <param name="fromVersion">Source schema version string (e.g. "0.1").</param>
    /// <param name="toVersion">Target schema version string (e.g. "0.2").</param>
    bool CanMigrate(string fromVersion, string toVersion);

    /// <summary>
    /// Migrates the JSON snapshot from <paramref name="fromVersion"/> to <paramref name="toVersion"/>.
    /// </summary>
    /// <param name="snapshotJson">Raw JSON string of the snapshot to migrate.</param>
    /// <param name="fromVersion">Source schema version.</param>
    /// <param name="toVersion">Target schema version.</param>
    /// <returns>Success with the migrated JSON, or an error result if migration fails.</returns>
    Result<string> Migrate(string snapshotJson, string fromVersion, string toVersion);
}
