using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Diwink.Extensions.EntityFrameworkCore.RelationshipStrategies;

/// <summary>
/// Handles required one-to-one dependent removal.
/// When the reference is set to null in the updated graph, the existing
/// dependent entity is deleted (FR-007, FR-008).
/// </summary>
internal static class RequiredOneToOneStrategy
{
    /// <summary>
    /// Removes the required dependent by marking it for deletion.
    /// EF Core will delete the row on SaveChanges.
    /// <summary>
    /// Marks the dependent entity referenced by <paramref name="existingNavigation"/> for deletion if it is currently tracked.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> used to mark the dependent entity for removal.</param>
    /// <param name="existingNavigation">The tracked reference entry whose <c>CurrentValue</c> is the dependent entity to remove; no action is taken if its value is <c>null</c>.</param>
    public static void RemoveDependent(DbContext context, ReferenceEntry existingNavigation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(existingNavigation);

        var existingValue = existingNavigation.CurrentValue;
        if (existingValue is null)
            return;

        context.Remove(existingValue);
    }
}
