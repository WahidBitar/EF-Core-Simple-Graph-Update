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
    /// </summary>
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
    /// </summary>
    public static object[] GetKeyValues(DbContext context, object entity)
    {
        var entityType = context.Model.FindEntityType(entity.GetType());
        if (entityType is null)
            return [];

        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey is null)
            return [];

        return primaryKey.Properties
            .Select(p => GetRequiredDetachedKeyValue(entityType, p, entity))
            .ToArray();
    }

    /// <summary>
    /// Compares two key arrays for equality.
    /// </summary>
    public static bool KeysEqual(object[] keys1, object[] keys2)
    {
        return keys1.SequenceEqual(keys2);
    }

    /// <summary>
    /// Finds a matching entity in a collection by primary key comparison.
    /// </summary>
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

    internal static object? ReadDetachedPropertyValue(IProperty property, object entity)
    {
        if (property.PropertyInfo is not null)
            return property.PropertyInfo.GetValue(entity);

        if (property.FieldInfo is not null)
            return property.FieldInfo.GetValue(entity);

        throw new InvalidOperationException(
            $"Property '{property.Name}' on entity '{entity.GetType().Name}' does not expose a CLR property or field.");
    }

    private static object GetRequiredTrackedKeyValue(EntityEntry entry, IProperty property)
    {
        var value = entry.Property(property.Name).CurrentValue;
        return value ?? throw new InvalidOperationException(
            $"Primary key component '{property.Name}' on tracked entity '{entry.Metadata.ClrType.Name}' is null.");
    }

    private static object GetRequiredDetachedKeyValue(IEntityType entityType, IProperty property, object entity)
    {
        var value = ReadDetachedPropertyValue(property, entity);
        return value ?? throw new InvalidOperationException(
            $"Primary key component '{property.Name}' on detached entity '{entityType.ClrType.Name}' is null.");
    }
}
