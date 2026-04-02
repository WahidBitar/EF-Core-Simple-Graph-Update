using Diwink.Extensions.EntityFrameworkCore.GraphUpdate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Diwink.Extensions.EntityFrameworkCore.RelationshipStrategies;

/// <summary>
/// Handles many-to-many with payload (association entity) mutations.
/// Creates, updates, and removes association entities with payload.
/// Related entities are preserved on removal (FR-002, FR-005, FR-006).
/// </summary>
internal static class PayloadManyToManyStrategy
{
    public static void Apply(
        DbContext context,
        CollectionEntry existingNavigation,
        IEnumerable<object> updatedCollection)
    {
        var existingItems = existingNavigation.CurrentValue?.Cast<object>().ToList() ?? [];
        var updatedItems = updatedCollection.ToList();

        // Remove association entities not present in updated collection
        foreach (var existingItem in existingItems.ToList())
        {
            var existingKeys = EntityKeyHelper.GetKeyValues(context.Entry(existingItem));
            var match = EntityKeyHelper.FindByKey(context, updatedItems, existingKeys);
            if (match is null)
            {
                // Remove the association entity — EF Core will delete the row
                // Related entities are NOT deleted (FR-003 for payload associations)
                context.Remove(existingItem);
            }
        }

        // Add new or update existing association entities
        foreach (var updatedItem in updatedItems)
        {
            var updatedKeys = EntityKeyHelper.GetKeyValues(context, updatedItem);
            var existingMatch = FindInTracked(context, existingItems, updatedKeys);

            if (existingMatch is not null)
            {
                // Update payload fields on existing association entity
                context.Entry(existingMatch).CurrentValues.SetValues(updatedItem);
            }
            else
            {
                // New association entity — add to collection
                AddToCollection(existingNavigation, updatedItem);
            }
        }
    }

    private static object? FindInTracked(
        DbContext context,
        List<object> trackedItems,
        object[] targetKeys)
    {
        foreach (var item in trackedItems)
        {
            var itemKeys = EntityKeyHelper.GetKeyValues(context.Entry(item));
            if (EntityKeyHelper.KeysEqual(itemKeys, targetKeys))
                return item;
        }
        return null;
    }

    private static void AddToCollection(CollectionEntry navigation, object item)
    {
        var addMethod = navigation.CurrentValue!.GetType().GetMethod("Add");
        addMethod?.Invoke(navigation.CurrentValue, [item]);
    }
}
