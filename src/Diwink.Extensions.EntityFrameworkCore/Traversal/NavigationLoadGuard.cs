using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace Diwink.Extensions.EntityFrameworkCore.Traversal;

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
    /// <summary>
    /// Validates loaded navigations on the tracked entity and its reachable object graph, ensuring required navigations are present before mutation.
    /// </summary>
    /// <param name="existingEntry">The tracked entity entry whose navigations and reachable entities will be validated.</param>
    /// <exception cref="UnloadedNavigationMutationException">Thrown when a required navigation encountered during validation is not loaded.</exception>
    public static void EnsureNavigationsLoaded(EntityEntry existingEntry)
    {
        EnsureNavigationsLoaded(
            existingEntry,
            new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    /// <summary>
    /// Traverse the tracked entity graph from the provided entry, following only navigations that are already loaded and recursing into their tracked child entries.
    /// </summary>
    /// <param name="existingEntry">The tracked <see cref="EntityEntry"/> that serves as the root of the traversal.</param>
    /// <param name="visited">A reference-equality <see cref="HashSet{Object}"/> used to record entities already visited and prevent infinite recursion through cycles.</param>
    private static void EnsureNavigationsLoaded(
        EntityEntry existingEntry,
        HashSet<object> visited)
    {
        if (!visited.Add(existingEntry.Entity))
            return;

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
                    EnsureNavigationsLoaded(childEntry, visited);
                }
            }
            else if (navigation is ReferenceEntry referenceEntry && referenceEntry.CurrentValue is not null)
            {
                var childEntry = existingEntry.Context.Entry(referenceEntry.CurrentValue);
                EnsureNavigationsLoaded(childEntry, visited);
            }
        }
    }

    /// <summary>
    /// Validates that a specific navigation is loaded before attempting mutation on it.
    /// Throws <see cref="UnloadedNavigationMutationException"/> if not.
    /// <summary>
    /// Ensures the specified navigation is loaded before performing a mutation.
    /// </summary>
    /// <param name="navigation">The navigation entry to validate is loaded.</param>
    /// <param name="entityPath">A slash-delimited path that identifies the entity location used in the exception message.</param>
    /// <exception cref="UnloadedNavigationMutationException">Thrown when <paramref name="navigation"/> is not loaded.</exception>
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
