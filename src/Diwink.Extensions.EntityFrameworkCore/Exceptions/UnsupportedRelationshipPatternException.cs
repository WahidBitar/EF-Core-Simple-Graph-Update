namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Thrown when a requested mutation uses a relationship pattern not in the v2 contract.
/// </summary>
public sealed class UnsupportedRelationshipPatternException : GraphUpdateException
{
    public string PatternIdentifier { get; }

    /// <summary>
    /// Initializes a new <see cref="UnsupportedRelationshipPatternException"/> for a mutation that relies on a relationship pattern not supported by the v2 contract.
    /// </summary>
    /// <param name="relationshipPath">The relationship path where the unsupported pattern was encountered.</param>
    /// <param name="patternIdentifier">The identifier of the unsupported relationship pattern.</param>
    public UnsupportedRelationshipPatternException(
        string relationshipPath,
        string patternIdentifier)
        : base(
            $"Unsupported relationship pattern '{patternIdentifier}' at '{relationshipPath}'. " +
            "See the v2 contract documentation for supported patterns.",
            relationshipPath)
    {
        PatternIdentifier = ValidateAndNormalize(patternIdentifier, nameof(patternIdentifier), "Pattern identifier");
    }
}
