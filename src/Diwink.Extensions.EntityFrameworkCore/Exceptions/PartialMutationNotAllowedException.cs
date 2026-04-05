namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Thrown when a graph contains both supported and unsupported requested mutations.
/// The entire operation is rejected.
/// </summary>
public sealed class PartialMutationNotAllowedException : GraphUpdateException
{
    public string UnsupportedBranch { get; }

    /// <summary>
    /// Creates an exception that indicates a graph update was rejected because a requested mutation contained an unsupported branch.
    /// </summary>
    /// <param name="relationshipPath">The relationship path in the graph where the update was attempted.</param>
    /// <param name="unsupportedBranch">The identifier or path of the branch that contains unsupported mutations.</param>
    public PartialMutationNotAllowedException(
        string relationshipPath,
        string unsupportedBranch)
        : base(
            $"Graph operation rejected: unsupported mutation detected at '{unsupportedBranch}'. " +
            "The entire operation was rejected — partial mutation is not allowed.",
            relationshipPath)
    {
        UnsupportedBranch = ValidateAndNormalize(unsupportedBranch, nameof(unsupportedBranch), "Unsupported branch");
    }
}
