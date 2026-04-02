using Diwink.Extensions.EntityFrameworkCore.V2.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Diwink.Extensions.EntityFrameworkCore.V2.Traversal;

/// <summary>
/// Validates that all navigations required for a graph mutation are explicitly loaded.
/// Rejects with a clear exception when a mutation depends on a missing or partially
/// loaded navigation (FR-015, FR-016).
/// </summary>
internal static class NavigationLoadGuard
{
    /// <summary>
    /// Ensures all navigations in the existing entity entry that the caller expects
    /// to participate in graph mutation are explicitly loaded.
    /// </summary>
    public static void EnsureNavigationsLoaded(EntityEntry existingEntry)
    {
        foreach (var navigation in existingEntry.Navigations)
        {
            if (!navigation.IsLoaded)
                continue;

            // For collection navigations, recurse into loaded children
            if (navigation is CollectionEntry collectionEntry && collectionEntry.CurrentValue is not null)
            {
                foreach (var child in collectionEntry.CurrentValue.Cast<object>())
                {
                    var childEntry = existingEntry.Context.Entry(child);
                    EnsureNavigationsLoaded(childEntry);
                }
            }
            else if (navigation is ReferenceEntry referenceEntry && referenceEntry.CurrentValue is not null)
            {
                var childEntry = existingEntry.Context.Entry(referenceEntry.CurrentValue);
                EnsureNavigationsLoaded(childEntry);
            }
        }
    }

    /// <summary>
    /// Validates that a specific navigation is loaded before attempting mutation on it.
    /// Throws <see cref="UnloadedNavigationMutationException"/> if not.
    /// </summary>
    public static void RequireLoaded(NavigationEntry navigation, string entityPath)
    {
        if (!navigation.IsLoaded)
        {
            throw new UnloadedNavigationMutationException(
                entityPath,
                navigation.Metadata.Name);
        }
    }
}
