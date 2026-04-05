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
    /// </summary>
    public static void UpdateScalarProperties(EntityEntry existingEntry, object updatedEntity)
    {
        existingEntry.CurrentValues.SetValues(updatedEntity);
    }

    /// <summary>
    /// Recursively processes navigations on a tracked entity, applying graph updates
    /// for supported child navigations.
    /// </summary>
    public static void ProcessNavigations(
        DbContext context,
        EntityEntry existingEntry,
        object updatedEntity,
        Type aggregateType)
    {
        GraphUpdateOrchestrator.ApplyNavigations(context, existingEntry, updatedEntity, aggregateType);
    }
}
