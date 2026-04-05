using System.Collections;
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
    /// <summary>
    /// Synchronizes a payload-based many-to-many collection navigation so its association entities match the provided target collection.
    /// </summary>
    /// <remarks>
    /// Removes association entities that are not present in <paramref name="updatedCollection"/>, updates payload values on matching tracked associations, and adds new association entities to the navigation when no tracked match exists. Deleting an association entity only removes the association row; related non-association entities are not deleted.
    /// </remarks>
    /// <param name="context">The DbContext used to inspect entity keys and apply changes.</param>
    /// <param name="existingNavigation">The collection navigation that currently holds the association entities (may be null/empty).</param>
    /// <param name="updatedCollection">The desired collection of association entity instances to reconcile the navigation with.</param>
    /// <exception cref="System.InvalidOperationException">Thrown if the navigation's current collection value is null or does not expose a public Add method when attempting to add a new association entity.</exception>
    public static void Apply(
        DbContext context,
        CollectionEntry existingNavigation,
        IEnumerable<object> updatedCollection)
    {
        var existingItems = existingNavigation.CurrentValue?.Cast<object>().ToList() ?? [];
        var updatedItems = updatedCollection.ToList();

        // Remove association entities not present in updated collection
        foreach (var existingItem in existingItems)
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

    /// <summary>
    /// Finds the first object in the trackedItems whose entity key values match the provided targetKeys.
    /// </summary>
    /// <param name="trackedItems">List of currently tracked entity instances to search for a key match.</param>
    /// <param name="targetKeys">Array of key values to match against each tracked item's entity key.</param>
    /// <returns>The first tracked item whose key values equal <paramref name="targetKeys"/>, or <c>null</c> if no match is found.</returns>
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
    /// Adds an association entity instance to the specified collection navigation.
    /// </summary>
    /// <param name="navigation">The collection navigation whose underlying collection will receive the item.</param>
    /// <param name="item">The association entity instance to add to the collection.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the navigation's current collection value is null or if the collection type does not expose a public Add method.
    /// </exception>
    private static void AddToCollection(CollectionEntry navigation, object item)
    {
        var currentValue = navigation.CurrentValue ?? throw new InvalidOperationException(
            $"Collection navigation '{navigation.Metadata.DeclaringEntityType.ClrType.Name}.{navigation.Metadata.Name}' is null; cannot add item type '{item.GetType().FullName}'.");

        if (currentValue is IList list)
        {
            list.Add(item);
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
                $"Collection type '{currentValue.GetType().FullName}' for navigation '{navigation.Metadata.DeclaringEntityType.ClrType.Name}.{navigation.Metadata.Name}' does not expose a public Add method for item type '{item.GetType().FullName}'.");
        }

        var addMethod = collectionInterface.GetMethod(nameof(ICollection<object>.Add)) ?? throw new InvalidOperationException(
            $"Collection type '{currentValue.GetType().FullName}' for navigation '{navigation.Metadata.DeclaringEntityType.ClrType.Name}.{navigation.Metadata.Name}' does not expose a public Add method for item type '{item.GetType().FullName}'.");

        addMethod.Invoke(currentValue, [item]);
    }
}
