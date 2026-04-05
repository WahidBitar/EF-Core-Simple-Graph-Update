namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Thrown when required vs optional ownership semantics cannot be resolved
/// for a one-to-one mutation path.
/// </summary>
public sealed class AmbiguousOwnershipSemanticsException : GraphUpdateException
{
    public string MissingDetail { get; }

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
