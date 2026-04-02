namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Thrown when a requested mutation uses a relationship pattern not in the v2 contract.
/// </summary>
public sealed class UnsupportedRelationshipPatternException : GraphUpdateException
{
    public string PatternIdentifier { get; }

    public UnsupportedRelationshipPatternException(
        string relationshipPath,
        string patternIdentifier)
        : base(
            $"Unsupported relationship pattern '{patternIdentifier}' at '{relationshipPath}'. " +
            "See the v2 contract documentation for supported patterns.",
            relationshipPath)
    {
        PatternIdentifier = patternIdentifier;
    }
}
