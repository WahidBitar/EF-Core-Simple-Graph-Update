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
        foreach (var navigation in existingEntry.Navigations
            .Where(n => n.IsLoaded && n.Metadata.ClrType.FullName != aggregateType.FullName))
        {
            if (navigation is CollectionEntry collectionEntry)
            {
                ProcessCollectionNavigation(context, collectionEntry, updatedEntity, aggregateType);
            }
            else if (navigation is ReferenceEntry referenceEntry)
            {
                ProcessReferenceNavigation(context, referenceEntry, updatedEntity, aggregateType);
            }
        }
    }

    private static void ProcessCollectionNavigation(
        DbContext context,
        CollectionEntry existingNavigation,
        object updatedParent,
        Type aggregateType)
    {
        var navProperty = updatedParent.GetType().GetProperty(existingNavigation.Metadata.Name);
        if (navProperty is null)
            return;

        var updatedValue = navProperty.GetValue(updatedParent);
        if (updatedValue is not IEnumerable<object> updatedCollection)
            return;

        var existingItems = existingNavigation.CurrentValue?.Cast<object>().ToList() ?? [];
        var updatedItems = updatedCollection.ToList();

        // Match existing to updated by primary key
        foreach (var existingItem in existingItems.ToList())
        {
            var existingKeys = EntityKeyHelper.GetKeyValues(context.Entry(existingItem));
            var match = EntityKeyHelper.FindByKey(context, updatedItems, existingKeys);

            if (match is not null)
            {
                // Recursive update
                var childEntry = context.Entry(existingItem);
                UpdateScalarProperties(childEntry, match);
                ProcessNavigations(context, childEntry, match, aggregateType);
            }
            else
            {
                // Remove
                context.Remove(existingItem);
            }
        }

        // Add new items
        foreach (var updatedItem in updatedItems)
        {
            var updatedKeys = EntityKeyHelper.GetKeyValues(context, updatedItem);
            var existingMatch = existingItems.FirstOrDefault(e =>
                EntityKeyHelper.KeysEqual(
                    EntityKeyHelper.GetKeyValues(context.Entry(e)),
                    updatedKeys));

            if (existingMatch is null)
            {
                var addMethod = existingNavigation.CurrentValue!.GetType().GetMethod("Add");
                addMethod?.Invoke(existingNavigation.CurrentValue, [updatedItem]);
            }
        }
    }

    private static void ProcessReferenceNavigation(
        DbContext context,
        ReferenceEntry existingNavigation,
        object updatedParent,
        Type aggregateType)
    {
        var navProperty = updatedParent.GetType().GetProperty(existingNavigation.Metadata.Name);
        if (navProperty is null)
            return;

        var updatedValue = navProperty.GetValue(updatedParent);
        var existingValue = existingNavigation.CurrentValue;

        if (updatedValue is null && existingValue is not null)
        {
            // Reference removed — handled by specific one-to-one strategies later
            return;
        }

        if (updatedValue is not null && existingValue is not null)
        {
            // Update existing reference
            var childEntry = context.Entry(existingValue);
            UpdateScalarProperties(childEntry, updatedValue);
            ProcessNavigations(context, childEntry, updatedValue, aggregateType);
        }
        else if (updatedValue is not null && existingValue is null)
        {
            // New reference
            existingNavigation.CurrentValue = updatedValue;
        }
    }
}
