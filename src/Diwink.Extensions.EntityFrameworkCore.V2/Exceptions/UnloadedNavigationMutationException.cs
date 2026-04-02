namespace Diwink.Extensions.EntityFrameworkCore.V2.Exceptions;

/// <summary>
/// Thrown when a requested mutation depends on a navigation branch not explicitly loaded.
/// </summary>
public sealed class UnloadedNavigationMutationException : GraphUpdateException
{
    public string NavigationName { get; }

    public UnloadedNavigationMutationException(
        string relationshipPath,
        string navigationName)
        : base(
            $"Mutation at '{relationshipPath}' depends on navigation '{navigationName}' " +
            "which was not explicitly loaded. The entire operation was rejected without partial apply.",
            relationshipPath)
    {
        NavigationName = navigationName;
    }
}
