using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Diwink.Extensions.EntityFrameworkCore.GraphUpdate;

/// <summary>
/// Extracts and compares primary key values from entities using EF Core metadata.
/// </summary>
internal static class EntityKeyHelper
{
    /// <summary>
    /// Gets the primary key values for a tracked entity.
    /// <summary>
    /// Extracts the primary key component values from a tracked EF Core entity entry.
    /// </summary>
    /// <param name="entry">The tracked entity entry to read key values from.</param>
    /// <returns>An array of primary key component values in the primary key's property order.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity type does not define a primary key, or when any primary key component value on the tracked entity is null.
    /// </exception>
    public static object[] GetKeyValues(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity '{entry.Metadata.ClrType.Name}' does not define a primary key.");

        return primaryKey.Properties
            .Select(p => GetRequiredTrackedKeyValue(entry, p))
            .ToArray();
    }

    /// <summary>
    /// Gets the primary key values for a detached entity using model metadata.
    /// <summary>
    /// Extracts the primary key component values for a detached CLR entity using the provided DbContext's model metadata.
    /// </summary>
    /// <param name="context">The DbContext whose model is used to resolve the entity type and primary key.</param>
    /// <param name="entity">The CLR entity instance to read key values from.</param>
    /// <returns>An array containing the primary key component values in primary-key order, or an empty array if the entity type or its primary key metadata cannot be found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if any primary key component value is null or if a key property cannot be read from the CLR entity (for example, when neither a PropertyInfo nor FieldInfo is available).</exception>
    public static object[] GetKeyValues(DbContext context, object entity)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        var entityType = context.Model.FindEntityType(entity.GetType())
            ?? throw new InvalidOperationException(
                $"Entity type '{entity.GetType().FullName}' does not exist in the current DbContext model.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity '{entityType.ClrType.Name}' does not define a primary key.");

        return primaryKey.Properties
            .Select(p => GetRequiredDetachedKeyValue(entityType, p, entity))
            .ToArray();
    }

    /// <summary>
    /// Compares two key arrays for equality.
    /// <summary>
    /// Determines whether two arrays of primary-key component values are equal element-by-element in order.
    /// </summary>
    /// <param name="keys1">First array of key component values, in primary-key order.</param>
    /// <param name="keys2">Second array of key component values, in primary-key order.</param>
    /// <returns><c>true</c> if both arrays have the same length and each element equals the corresponding element in the other array; <c>false</c> otherwise.</returns>
    public static bool KeysEqual(object[] keys1, object[] keys2)
    {
        ArgumentNullException.ThrowIfNull(keys1);
        ArgumentNullException.ThrowIfNull(keys2);

        if (keys1.Length != keys2.Length)
            return false;

        for (var index = 0; index < keys1.Length; index++)
        {
            var left = keys1[index];
            var right = keys2[index];

            if (left is byte[] leftBytes && right is byte[] rightBytes)
            {
                if (!leftBytes.AsSpan().SequenceEqual(rightBytes))
                    return false;

                continue;
            }

            if (left is byte[] || right is byte[])
                return false;

            if (!Equals(left, right))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Finds a matching entity in a collection by primary key comparison.
    /// <summary>
    /// Finds the first entity in <paramref name="collection"/> whose primary key components, as determined from <paramref name="context"/>'s model, match <paramref name="targetKeys"/>.
    /// </summary>
    /// <param name="collection">Sequence of entities to search.</param>
    /// <param name="targetKeys">Primary key component values to match, in the same order as the entity type's primary key properties.</param>
    /// <returns>The first matching entity from <paramref name="collection"/>, or <c>null</c> if no match is found.</returns>
    public static T? FindByKey<T>(
        DbContext context,
        IEnumerable<T> collection,
        object[] targetKeys) where T : class
    {
        foreach (var item in collection)
        {
            var itemKeys = GetKeyValues(context, item);
            if (KeysEqual(itemKeys, targetKeys))
                return item;
        }
        return null;
    }

    /// <summary>
    /// Read the CLR value of the given model property or field from a detached entity instance.
    /// </summary>
    /// <param name="property">The EF Core model property describing the mapped CLR property or field.</param>
    /// <param name="entity">The CLR entity instance to read the value from.</param>
    /// <returns>The CLR value of the property or field, or <c>null</c> if the value is null.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <paramref name="property"/> does not expose a CLR PropertyInfo or FieldInfo.</exception>
    internal static object? ReadDetachedPropertyValue(IProperty property, object entity)
    {
        if (property.PropertyInfo is not null)
            return property.PropertyInfo.GetValue(entity);

        if (property.FieldInfo is not null)
            return property.FieldInfo.GetValue(entity);

        throw new InvalidOperationException(
            $"Property '{property.Name}' on entity '{entity.GetType().Name}' does not expose a CLR property or field.");
    }

    /// <summary>
    /// Get the non-null value of a primary key component for a tracked entity.
    /// </summary>
    /// <param name="entry">The tracked entity entry containing the current property values.</param>
    /// <param name="property">The primary key property metadata whose value to read.</param>
    /// <returns>The primary key component value (guaranteed non-null).</returns>
    /// <exception cref="InvalidOperationException">Thrown if the primary key component value is null on the tracked entity.</exception>
    private static object GetRequiredTrackedKeyValue(EntityEntry entry, IProperty property)
    {
        var value = entry.Property(property.Name).CurrentValue;
        return value ?? throw new InvalidOperationException(
            $"Primary key component '{property.Name}' on tracked entity '{entry.Metadata.ClrType.Name}' is null.");
    }

    /// <summary>
    /// Retrieves a primary key component value from a detached entity and ensures it is not null.
    /// </summary>
    /// <param name="entityType">The EF Core entity type metadata for the detached entity.</param>
    /// <param name="property">The primary key property metadata to read.</param>
    /// <param name="entity">The detached CLR entity instance.</param>
    /// <returns>The non-null value of the specified primary key component.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the key component value is null.</exception>
    private static object GetRequiredDetachedKeyValue(IEntityType entityType, IProperty property, object entity)
    {
        var value = ReadDetachedPropertyValue(property, entity);
        return value ?? throw new InvalidOperationException(
            $"Primary key component '{property.Name}' on detached entity '{entityType.ClrType.Name}' is null.");
    }
}
