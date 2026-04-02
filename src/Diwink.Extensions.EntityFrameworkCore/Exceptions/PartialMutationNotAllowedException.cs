namespace Diwink.Extensions.EntityFrameworkCore.V2.Exceptions;

/// <summary>
/// Thrown when a graph contains both supported and unsupported requested mutations.
/// The entire operation is rejected.
/// </summary>
public sealed class PartialMutationNotAllowedException : GraphUpdateException
{
    public string UnsupportedBranch { get; }

    public PartialMutationNotAllowedException(
        string relationshipPath,
        string unsupportedBranch)
        : base(
            $"Graph operation rejected: unsupported mutation detected at '{unsupportedBranch}'. " +
            "The entire operation was rejected — partial mutation is not allowed.",
            relationshipPath)
    {
        UnsupportedBranch = unsupportedBranch;
    }
}
