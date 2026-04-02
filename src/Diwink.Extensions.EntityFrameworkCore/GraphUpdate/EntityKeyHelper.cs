using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Diwink.Extensions.EntityFrameworkCore.V2.GraphUpdate;

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
        var keyProperties = entry.Metadata.FindPrimaryKey()!.Properties;
        return keyProperties.Select(p => entry.Property(p.Name).CurrentValue!).ToArray();
    }

    /// <summary>
    /// Gets the primary key values for a detached entity using model metadata.
    /// </summary>
    public static object[] GetKeyValues(DbContext context, object entity)
    {
        var entityType = context.Model.FindEntityType(entity.GetType());
        if (entityType is null)
            return [];

        var keyProperties = entityType.FindPrimaryKey()!.Properties;
        return keyProperties.Select(p => entityType.FindProperty(p.Name)!)
            .Select(p => p.PropertyInfo!.GetValue(entity)!)
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
}
