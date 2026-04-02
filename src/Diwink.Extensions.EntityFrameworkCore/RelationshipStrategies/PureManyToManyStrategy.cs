using Diwink.Extensions.EntityFrameworkCore.GraphUpdate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Diwink.Extensions.EntityFrameworkCore.RelationshipStrategies;

/// <summary>
/// Handles pure many-to-many (skip navigation) mutations.
/// Adds missing links, removes excess links, creates new related entities,
/// and updates existing related entities (FR-002, FR-003, FR-004).
/// </summary>
internal static class PureManyToManyStrategy
{
    public static void Apply(
        DbContext context,
        CollectionEntry existingNavigation,
        IEnumerable<object> updatedCollection)
    {
        var existingItems = existingNavigation.CurrentValue?.Cast<object>().ToList() ?? [];
        var updatedItems = updatedCollection.ToList();

        // Remove links not present in the updated collection
        foreach (var existingItem in existingItems)
        {
            var existingKeys = EntityKeyHelper.GetKeyValues(context.Entry(existingItem));
            var match = EntityKeyHelper.FindByKey(context, updatedItems, existingKeys);
            if (match is null)
            {
                // Unlink only — remove from the collection navigation
                // EF Core will handle removing the join table row
                RemoveFromCollection(existingNavigation, existingItem);
            }
        }

        // Add new links or update existing related entities
        foreach (var updatedItem in updatedItems)
        {
            var updatedKeys = EntityKeyHelper.GetKeyValues(context, updatedItem);
            var existingMatch = FindInTracked(context, existingItems, updatedKeys);

            if (existingMatch is not null)
            {
                // Update existing related entity properties
                context.Entry(existingMatch).CurrentValues.SetValues(updatedItem);
            }
            else
            {
                // Entity not in this collection — resolve via tracker or store
                var entityType = context.Model.FindEntityType(updatedItem.GetType());
                var pk = entityType?.FindPrimaryKey();
                if (pk is not null)
                {
                    var keyValues = pk.Properties
                        .Select(p => p.PropertyInfo!.GetValue(updatedItem)!)
                        .ToArray();

                    // Find checks tracker first, then queries store
                    var knownEntity = context.Find(entityType!.ClrType, keyValues);

                    if (knownEntity is not null)
                    {
                        // Entity exists — update properties and create link
                        context.Entry(knownEntity).CurrentValues.SetValues(updatedItem);
                        AddToCollection(existingNavigation, knownEntity);
                    }
                    else
                    {
                        // New entity — explicitly track as Added so EF inserts it
                        context.Add(updatedItem);
                        AddToCollection(existingNavigation, updatedItem);
                    }
                }
                else
                {
                    AddToCollection(existingNavigation, updatedItem);
                }
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

    private static void RemoveFromCollection(CollectionEntry navigation, object item)
    {
        var removeMethod = navigation.CurrentValue!.GetType().GetMethod("Remove");
        removeMethod?.Invoke(navigation.CurrentValue, [item]);
    }

    private static void AddToCollection(CollectionEntry navigation, object item)
    {
        var addMethod = navigation.CurrentValue!.GetType().GetMethod("Add");
        addMethod?.Invoke(navigation.CurrentValue, [item]);
    }
}
