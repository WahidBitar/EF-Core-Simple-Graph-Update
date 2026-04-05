namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Thrown when a requested mutation depends on a navigation branch not explicitly loaded.
/// </summary>
public sealed class UnloadedNavigationMutationException : GraphUpdateException
{
    public string NavigationName { get; }

    /// <summary>
    /// Initializes a new exception indicating a mutation relied on a navigation that was not explicitly loaded.
    /// </summary>
    /// <param name="relationshipPath">The relationship path identifying where the mutation was requested.</param>
    /// <param name="navigationName">The name of the navigation property that was required but not loaded.</param>
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
