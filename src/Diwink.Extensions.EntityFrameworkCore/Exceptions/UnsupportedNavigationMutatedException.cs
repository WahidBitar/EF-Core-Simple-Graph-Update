namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Thrown when a loaded navigation of a currently-unsupported relationship type
/// (e.g., one-to-many) has mutations detected during graph diff.
/// Unchanged unsupported navigations are silently skipped.
/// </summary>
public sealed class UnsupportedNavigationMutatedException : GraphUpdateException
{
    public string RelationshipType { get; }

    public UnsupportedNavigationMutatedException(
        string relationshipPath,
        string relationshipType)
        : base(
            $"Mutation detected in unsupported navigation '{relationshipPath}' " +
            $"(relationship type: {relationshipType}). This relationship type is not " +
            "in the v2 supported contract. The entire operation was rejected. " +
            "Unchanged unsupported navigations would have been silently skipped.",
            relationshipPath)
    {
        RelationshipType = ValidateAndNormalize(relationshipType, nameof(relationshipType), "Relationship type");
    }
}
