namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Thrown when required vs optional ownership semantics cannot be resolved
/// for a one-to-one mutation path.
/// </summary>
public sealed class AmbiguousOwnershipSemanticsException : GraphUpdateException
{
    public string MissingDetail { get; }

    /// <summary>
    /// Creates a new AmbiguousOwnershipSemanticsException for a one-to-one mutation path when requiredness or ownership metadata is ambiguous or missing.
    /// </summary>
    /// <param name="relationshipPath">The relationship path where the ambiguous ownership semantics were detected.</param>
    /// <param name="missingDetail">A specific detail describing the missing metadata that caused the ambiguity.</param>
    public AmbiguousOwnershipSemanticsException(
        string relationshipPath,
        string missingDetail)
        : base(
            $"Ambiguous ownership semantics at '{relationshipPath}': {missingDetail}. " +
            "The contract requires explicit requiredness/ownership metadata.",
            relationshipPath)
    {
        MissingDetail = ValidateAndNormalize(missingDetail, nameof(missingDetail), "Missing detail");
    }
}
