using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Diwink.Extensions.EntityFrameworkCore.RelationshipStrategies;

/// <summary>
/// Handles optional one-to-one dependent removal.
/// When the reference is set to null in the updated graph, the FK on the
/// dependent is nulled (detached) — the dependent entity is preserved (FR-009, FR-010, FR-011).
/// </summary>
internal static class OptionalOneToOneStrategy
{
    /// <summary>
    /// Detaches the optional dependent by nulling its FK properties.
    /// The dependent entity is preserved in the database with a null FK.
    /// </summary>
    public static void DetachDependent(DbContext context, ReferenceEntry existingNavigation)
    {
        var existingValue = existingNavigation.CurrentValue;
        if (existingValue is null)
            return;

        // Clear the navigation — EF Core will null the FK on the dependent
        // because the FK has DeleteBehavior.SetNull configured
        existingNavigation.CurrentValue = null;
    }

    /// <summary>
    /// Attaches a new optional dependent to the navigation and ensures EF Core
    /// treats it as an insert when it is not already tracked.
    /// </summary>
    public static void AttachDependent(DbContext context, ReferenceEntry existingNavigation, object dependent)
    {
        var dependentEntry = context.Entry(dependent);
        if (dependentEntry.State == EntityState.Detached)
        {
            dependentEntry.State = EntityState.Added;
        }

        existingNavigation.CurrentValue = dependent;
    }

    /// <summary>
    /// Replaces the current optional dependent by detaching the old row and
    /// linking a new dependent instance.
    /// </summary>
    public static void ReplaceDependent(DbContext context, ReferenceEntry existingNavigation, object dependent)
    {
        DetachDependent(context, existingNavigation);
        AttachDependent(context, existingNavigation, dependent);
    }
}
