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
    /// </summary>
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
