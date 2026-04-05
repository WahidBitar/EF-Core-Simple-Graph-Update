using System.Collections;
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
                    // Find checks tracker first, then queries store
                    var knownEntity = context.Find(entityType!.ClrType, updatedKeys);

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
        ExecuteCollectionOperation(navigation, item, "remove", static (list, value) =>
        {
            list.Remove(value);
        });
    }

    private static void AddToCollection(CollectionEntry navigation, object item)
    {
        ExecuteCollectionOperation(navigation, item, "add", static (list, value) =>
        {
            list.Add(value);
        });
    }

    private static void ExecuteCollectionOperation(
        CollectionEntry navigation,
        object item,
        string operation,
        Action<IList, object> listOperation)
    {
        var currentValue = navigation.CurrentValue ?? throw new InvalidOperationException(
            $"Collection navigation '{navigation.Metadata.DeclaringEntityType.ClrType.Name}.{navigation.Metadata.Name}' has null CurrentValue; cannot {operation} item '{item}'.");

        if (currentValue is IList list)
        {
            listOperation(list, item);
            return;
        }

        var collectionInterface = currentValue.GetType().GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(ICollection<>) &&
                i.GenericTypeArguments[0].IsAssignableFrom(item.GetType()));

        if (collectionInterface is null)
        {
            throw new InvalidOperationException(
                $"Collection navigation '{navigation.Metadata.DeclaringEntityType.ClrType.Name}.{navigation.Metadata.Name}' with current value type '{currentValue.GetType().FullName}' does not support {operation} for item type '{item.GetType().FullName}'.");
        }

        var methodName = operation == "add" ? nameof(ICollection<object>.Add) : nameof(ICollection<object>.Remove);
        var method = collectionInterface.GetMethod(methodName) ?? throw new InvalidOperationException(
            $"Collection interface '{collectionInterface.FullName}' for navigation '{navigation.Metadata.DeclaringEntityType.ClrType.Name}.{navigation.Metadata.Name}' does not expose '{methodName}'.");

        method.Invoke(currentValue, [item]);
    }
}
