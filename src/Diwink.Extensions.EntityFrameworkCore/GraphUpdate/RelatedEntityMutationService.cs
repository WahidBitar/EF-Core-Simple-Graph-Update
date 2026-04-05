using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Diwink.Extensions.EntityFrameworkCore.GraphUpdate;

/// <summary>
/// Handles creation and update of related entities through supported
/// many-to-many relationship paths (FR-004, FR-005).
/// </summary>
internal static class RelatedEntityMutationService
{
    /// <summary>
    /// Updates scalar properties of a tracked entity from a detached source entity.
    /// <summary>
    /// Copy scalar property values from a detached updated entity onto a tracked entity entry.
    /// </summary>
    /// <param name="existingEntry">The tracked EntityEntry whose current scalar values will be updated.</param>
    /// <param name="updatedEntity">The detached entity instance providing the new scalar values.</param>
    public static void UpdateScalarProperties(EntityEntry existingEntry, object updatedEntity)
    {
        existingEntry.CurrentValues.SetValues(updatedEntity);
    }

    /// <summary>
    /// Recursively processes navigations on a tracked entity, applying graph updates
    /// for supported child navigations.
    /// <summary>
    /// Processes and applies navigation (relationship) updates from a detached entity onto a tracked entity within the given DbContext.
    /// </summary>
    /// <param name="updatedEntity">The detached entity containing the desired navigation state to apply.</param>
    /// <param name="aggregateType">The root aggregate CLR type used to resolve relationship paths when processing navigations.</param>
    public static void ProcessNavigations(
        DbContext context,
        EntityEntry existingEntry,
        object updatedEntity,
        Type aggregateType)
    {
        GraphUpdateOrchestrator.ApplyNavigations(context, existingEntry, updatedEntity, aggregateType);
    }
}
