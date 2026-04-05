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
    /// <summary>
    /// Reconciles a skip-navigation (pure many-to-many) collection with the provided updated set of related entities.
    /// </summary>
    /// <remarks>
    /// Removes links for entities that are no longer present in <paramref name="updatedCollection"/>, updates properties of already-tracked related entities, and adds links for new or discovered entities (marking new entities as added so EF will insert them).</remarks>
    /// <param name="context">The DbContext used to access entity metadata, track entities, and query the store.</param>
    /// <param name="existingNavigation">The existing collection navigation entry (skip navigation) to modify.</param>
    /// <param name="updatedCollection">The desired related entities to be reflected in the navigation; each element represents a related entity instance or a DTO with matching key values.</param>
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
                ApplyValuesIfNotModified(context, existingMatch, updatedItem);
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
                        ApplyValuesIfNotModified(context, knownEntity, updatedItem);
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

    private static void ApplyValuesIfNotModified(DbContext context, object trackedEntity, object updatedEntity)
    {
        var trackedEntry = context.Entry(trackedEntity);
        if (trackedEntry.State is EntityState.Unchanged or EntityState.Detached)
        {
            trackedEntry.CurrentValues.SetValues(updatedEntity);
        }
    }

    /// <summary>
    /// Finds the first entity in a list of tracked items whose primary key values match the provided key values.
    /// </summary>
    /// <param name="context">The DbContext used to extract key values for each tracked item.</param>
    /// <param name="trackedItems">A list of currently tracked entity instances to search.</param>
    /// <param name="targetKeys">An array of key values to match against each tracked item's primary key values.</param>
    /// <returns>The matching tracked entity instance if found; otherwise <c>null</c>.</returns>
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

    /// <summary>
    /// Remove an item from a collection-valued navigation property.
    /// </summary>
    /// <param name="navigation">The collection navigation entry whose current value will be modified.</param>
    /// <param name="item">The related entity instance to remove from the navigation collection.</param>
    private static void RemoveFromCollection(CollectionEntry navigation, object item)
    {
        ExecuteCollectionOperation(navigation, item, "remove", static (list, value) =>
        {
            list.Remove(value);
        });
    }

    /// <summary>
    /// Add an entity instance to the given navigation collection (handles IList or compatible ICollection&lt;T&gt; implementations).
    /// </summary>
    /// <param name="navigation">The EF Core collection navigation entry to modify.</param>
    /// <param name="item">The entity instance to add to the navigation collection.</param>
    /// <exception cref="InvalidOperationException">Thrown when the navigation's CurrentValue is null or does not support adding the item's type.</exception>
    private static void AddToCollection(CollectionEntry navigation, object item)
    {
        ExecuteCollectionOperation(navigation, item, "add", static (list, value) =>
        {
            list.Add(value);
        });
    }

    /// <summary>
    /// Performs an add/remove operation against the runtime collection held by a navigation's CurrentValue.
    /// </summary>
    /// <param name="navigation">The collection navigation whose CurrentValue will be mutated.</param>
    /// <param name="item">The item to add or remove from the collection.</param>
    /// <param name="operation">A short operation name used in error messages (expected values: "add" or "remove").</param>
    /// <param name="listOperation">A fallback action that performs the operation when the collection implements <see cref="IList"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the navigation's CurrentValue is null; when the CurrentValue's runtime type does not expose a compatible generic <c>ICollection&lt;T&gt;</c> for the item's type; or when the discovered collection interface does not expose the expected Add/Remove method.
    /// </exception>
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
