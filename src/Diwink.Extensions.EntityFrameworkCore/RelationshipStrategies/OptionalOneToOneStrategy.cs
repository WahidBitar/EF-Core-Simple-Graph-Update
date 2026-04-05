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
    /// <summary>
    /// Clears the given optional dependent navigation on the tracked principal so the dependent's foreign key is set to null while leaving the dependent entity intact.
    /// </summary>
    /// <param name="context">The DbContext whose change tracker manages the principal and dependent entities.</param>
    /// <param name="existingNavigation">The navigation entry on the principal that references the optional dependent to detach.</param>
    public static void DetachDependent(DbContext context, ReferenceEntry existingNavigation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(existingNavigation);

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
    /// <summary>
    /// Ensures the provided dependent entity is tracked for insertion if it is detached, then assigns it to the specified navigation.
    /// </summary>
    /// <param name="existingNavigation">The reference navigation on the principal entity to set to the dependent.</param>
    /// <param name="dependent">The dependent entity to attach (if needed) and assign to the navigation.</param>
    public static void AttachDependent(DbContext context, ReferenceEntry existingNavigation, object dependent)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(existingNavigation);
        ArgumentNullException.ThrowIfNull(dependent);

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
    /// <summary>
    /// Replaces the optional dependent referenced by an existing navigation with a new dependent instance.
    /// </summary>
    /// <param name="context">The DbContext used to obtain and modify entity tracking state.</param>
    /// <param name="existingNavigation">The reference navigation entry that currently points to the dependent to be replaced.</param>
    /// <param name="dependent">The new dependent instance to assign; if it is not tracked it will be marked for insertion.</param>
    public static void ReplaceDependent(DbContext context, ReferenceEntry existingNavigation, object dependent)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(existingNavigation);
        ArgumentNullException.ThrowIfNull(dependent);

        DetachDependent(context, existingNavigation);
        AttachDependent(context, existingNavigation, dependent);
    }
}
