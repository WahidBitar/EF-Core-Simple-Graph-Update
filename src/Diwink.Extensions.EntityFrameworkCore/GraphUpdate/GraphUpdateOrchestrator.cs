using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using Diwink.Extensions.EntityFrameworkCore.RelationshipStrategies;
using Diwink.Extensions.EntityFrameworkCore.Traversal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Diwink.Extensions.EntityFrameworkCore.GraphUpdate;

/// <summary>
/// Core orchestrator that diffs a detached updated entity graph against a tracked
/// existing entity graph and applies mutations using the appropriate relationship
/// strategy for each navigation.
///
/// Enforces FR-001a (detached graph input), FR-015 (explicit loading),
/// FR-017 (all-or-nothing), FR-018/FR-019 (unsupported type handling).
/// </summary>
internal static class GraphUpdateOrchestrator
{
    /// <summary>
    /// Entry point for graph update orchestration.
    /// </summary>
    public static T UpdateGraph<T>(DbContext context, T updatedEntity, T existingEntity)
        where T : class
    {
        var existingEntry = context.Entry(existingEntity);
        var aggregateType = typeof(T);

        // Phase 1: Validate — collect all errors before any mutation
        var guard = new OperationGuard();
        ValidateNavigations(context, existingEntry, updatedEntity, aggregateType, guard);
        guard.ThrowIfErrors();

        // Phase 2: Apply — update scalar properties then process navigations
        RelatedEntityMutationService.UpdateScalarProperties(existingEntry, updatedEntity);
        ApplyNavigations(context, existingEntry, updatedEntity, aggregateType);

        return existingEntity;
    }

    private static void ValidateNavigations(
        DbContext context,
        EntityEntry existingEntry,
        object updatedEntity,
        Type aggregateType,
        OperationGuard guard,
        HashSet<object>? recursionPath = null)
    {
        recursionPath ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!recursionPath.Add(existingEntry.Entity))
            return;

        try
        {
        // Check loaded navigations for unsupported mutations
        foreach (var navigation in existingEntry.Navigations
            .Where(n => n.IsLoaded))
        {
            var navMetadata = navigation.Metadata;
            if (IsNavigationBackToAggregateRoot(navMetadata, aggregateType))
                continue;

            var entityPath = $"{existingEntry.Metadata.ClrType.Name}.{navMetadata.Name}";

            var classification = ClassifyNavigation(navMetadata);

            if (classification == NavigationClassification.Unsupported)
            {
                // FR-018/FR-019: Check if mutations exist in unsupported navigation
                if (HasMutations(context, navigation, updatedEntity, navMetadata))
                {
                    guard.AddError(new UnsupportedNavigationMutatedException(
                        entityPath,
                        GetRelationshipTypeName(navMetadata)));
                }
                // else: silently skip (FR-019)
                continue;
            }

            ValidateLoadedChildren(
                context,
                navigation,
                updatedEntity,
                aggregateType,
                guard,
                recursionPath);
        }

        // FR-015/FR-016: Check unloaded navigations for attempted mutations
        foreach (var navigation in existingEntry.Navigations
            .Where(n => !n.IsLoaded))
        {
            var navMetadata = navigation.Metadata;

            if (IsNavigationBackToAggregateRoot(navMetadata, aggregateType))
                continue;

            var entityPath = $"{existingEntry.Metadata.ClrType.Name}.{navMetadata.Name}";

            if (HasUnloadedMutationAttempt(updatedEntity, navMetadata))
            {
                guard.AddError(new UnloadedNavigationMutationException(
                    entityPath,
                    navMetadata.Name));
            }
        }
        }
        finally
        {
            recursionPath.Remove(existingEntry.Entity);
        }
    }

    /// <summary>
    /// Checks whether the updated entity provides non-empty values for a navigation
    /// that was not loaded. A non-empty collection or non-null reference is treated
    /// as an attempted mutation on an unloaded navigation (FR-015).
    /// </summary>
    private static bool HasUnloadedMutationAttempt(
        object updatedEntity,
        INavigationBase navMetadata)
    {
        var navProperty = updatedEntity.GetType().GetProperty(navMetadata.Name);
        if (navProperty is null)
            return false;

        var updatedValue = navProperty.GetValue(updatedEntity);
        if (updatedValue is null)
            return false;

        // Collection navigation: non-null with items = attempted mutation
        if (updatedValue is IEnumerable<object> collection)
            return collection.Any();

        // Reference navigation: non-null = attempted mutation
        return true;
    }

    internal static void ApplyNavigations(
        DbContext context,
        EntityEntry existingEntry,
        object updatedEntity,
        Type aggregateType)
    {
        foreach (var navigation in existingEntry.Navigations
            .Where(n => n.IsLoaded))
        {
            var navMetadata = navigation.Metadata;
            if (IsNavigationBackToAggregateRoot(navMetadata, aggregateType))
                continue;

            var classification = ClassifyNavigation(navMetadata);

            // Skip unsupported navigations (already validated, no mutations)
            if (classification == NavigationClassification.Unsupported)
                continue;

            if (!TryGetUpdatedNavigationValue(updatedEntity, navMetadata, out var updatedValue))
                continue;

            if (navigation is CollectionEntry collectionEntry)
            {
                ApplyCollectionNavigation(context, collectionEntry, updatedValue, classification);
            }
            else if (navigation is ReferenceEntry referenceEntry)
            {
                ApplyReferenceNavigation(context, referenceEntry, updatedValue, classification, aggregateType);
            }
        }
    }

    private static void ApplyCollectionNavigation(
        DbContext context,
        CollectionEntry existingNavigation,
        object? updatedValue,
        NavigationClassification classification)
    {
        var updatedCollection = updatedValue as IEnumerable<object> ?? [];

        switch (classification)
        {
            case NavigationClassification.PureManyToMany:
                PureManyToManyStrategy.Apply(context, existingNavigation, updatedCollection);
                break;

            case NavigationClassification.PayloadManyToMany:
                PayloadManyToManyStrategy.Apply(context, existingNavigation, updatedCollection);
                break;

            default:
                // Should not reach here — unsupported already filtered
                break;
        }
    }

    private static void ApplyReferenceNavigation(
        DbContext context,
        ReferenceEntry existingNavigation,
        object? updatedValue,
        NavigationClassification classification,
        Type aggregateType)
    {
        var existingValue = existingNavigation.CurrentValue;

        if (updatedValue is not null && existingValue is not null)
        {
            if (classification == NavigationClassification.OptionalOneToOne &&
                !ReferenceKeysMatch(context, existingValue, updatedValue))
            {
                OptionalOneToOneStrategy.ReplaceDependent(context, existingNavigation, updatedValue);
                return;
            }

            // Update existing reference — scalars + nested navigations
            var childEntry = context.Entry(existingValue);
            RelatedEntityMutationService.UpdateScalarProperties(childEntry, updatedValue);
            RelatedEntityMutationService.ProcessNavigations(context, childEntry, updatedValue, aggregateType);
        }
        else if (updatedValue is not null && existingValue is null)
        {
            if (classification == NavigationClassification.OptionalOneToOne)
            {
                OptionalOneToOneStrategy.AttachDependent(context, existingNavigation, updatedValue);
                return;
            }

            // Add new reference
            existingNavigation.CurrentValue = updatedValue;
        }
        else if (updatedValue is null && existingValue is not null)
        {
            // Remove reference — behavior depends on required vs optional
            if (classification == NavigationClassification.RequiredOneToOne)
            {
                RequiredOneToOneStrategy.RemoveDependent(context, existingNavigation);
            }
            else if (classification == NavigationClassification.OptionalOneToOne)
            {
                OptionalOneToOneStrategy.DetachDependent(context, existingNavigation);
            }
        }
    }

    /// <summary>
    /// Classifies a navigation as a supported or unsupported relationship pattern.
    /// </summary>
    internal static NavigationClassification ClassifyNavigation(INavigationBase navMetadata)
    {
        if (navMetadata is ISkipNavigation)
            return NavigationClassification.PureManyToMany;

        if (navMetadata is INavigation nav)
        {
            var foreignKey = nav.ForeignKey;

            // Collection navigation on the principal side = one-to-many
            if (nav.IsCollection)
            {
                // Check if this is a payload many-to-many (explicit join entity)
                if (IsPayloadJoinEntity(nav.TargetEntityType))
                    return NavigationClassification.PayloadManyToMany;

                // Regular one-to-many — unsupported in v2
                return NavigationClassification.Unsupported;
            }

            // Reference navigation — one-to-one
            if (!foreignKey.IsUnique)
                return NavigationClassification.Unsupported;

            if (foreignKey.IsRequired)
                return NavigationClassification.RequiredOneToOne;

            return NavigationClassification.OptionalOneToOne;
        }

        return NavigationClassification.Unsupported;
    }

    /// <summary>
    /// Detects whether an entity type is a payload join entity (explicit many-to-many).
    /// A payload join entity has a composite key where all key parts are also foreign keys,
    /// AND it has additional non-key, non-FK properties (the payload).
    /// </summary>
    private static bool IsPayloadJoinEntity(IEntityType entityType)
    {
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count < 2)
            return false;

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var allKeyPropsAreFk = primaryKey.Properties.All(keyProp =>
            foreignKeys.Any(fk => fk.Properties.Contains(keyProp)));

        return allKeyPropsAreFk;
    }

    /// <summary>
    /// Checks whether a loaded navigation has mutations between existing and updated state.
    /// Used for FR-018 to detect changes in unsupported navigation types.
    /// </summary>
    private static bool HasMutations(
        DbContext context,
        NavigationEntry navigation,
        object updatedEntity,
        INavigationBase navMetadata)
    {
        if (!TryGetUpdatedNavigationValue(updatedEntity, navMetadata, out var updatedValue))
            return false;

        if (navigation is CollectionEntry collectionEntry)
        {
            var existingItems = collectionEntry.CurrentValue?.Cast<object>().ToList() ?? [];
            var updatedItems = (updatedValue as IEnumerable<object>)?.ToList() ?? [];

            if (existingItems.Count != updatedItems.Count)
                return true;

            // Compare by primary keys
            foreach (var existingItem in existingItems)
            {
                var existingKeys = EntityKeyHelper.GetKeyValues(context.Entry(existingItem));
                var match = EntityKeyHelper.FindByKey(context, updatedItems, existingKeys);
                if (match is null)
                    return true;

                if (HasScalarDifferences(context.Entry(existingItem), match))
                    return true;
            }

            return false;
        }

        if (navigation is ReferenceEntry referenceEntry)
        {
            var existingValue = referenceEntry.CurrentValue;
            if (existingValue is null && updatedValue is null)
                return false;
            if (existingValue is null || updatedValue is null)
                return true;

            // Both non-null — check if keys match
            var existingKeys = EntityKeyHelper.GetKeyValues(context.Entry(existingValue));
            var updatedKeys = EntityKeyHelper.GetKeyValues(context, updatedValue);
            if (!EntityKeyHelper.KeysEqual(existingKeys, updatedKeys))
                return true;

            return HasScalarDifferences(context.Entry(existingValue), updatedValue);
        }

        return false;
    }

    private static void ValidateLoadedChildren(
        DbContext context,
        NavigationEntry navigation,
        object updatedEntity,
        Type aggregateType,
        OperationGuard guard,
        HashSet<object> recursionPath)
    {
        if (!TryGetUpdatedNavigationValue(updatedEntity, navigation.Metadata, out var updatedValue) ||
            updatedValue is null)
        {
            return;
        }

        if (navigation is ReferenceEntry referenceEntry &&
            referenceEntry.CurrentValue is not null)
        {
            ValidateNavigations(
                context,
                context.Entry(referenceEntry.CurrentValue),
                updatedValue,
                aggregateType,
                guard,
                recursionPath);
            return;
        }

        if (navigation is not CollectionEntry collectionEntry ||
            updatedValue is not IEnumerable<object> updatedCollection)
        {
            return;
        }

        var updatedItems = updatedCollection.ToList();
        var existingItems = collectionEntry.CurrentValue?.Cast<object>() ?? [];

        foreach (var existingItem in existingItems)
        {
            var existingKeys = EntityKeyHelper.GetKeyValues(context.Entry(existingItem));
            var match = EntityKeyHelper.FindByKey(context, updatedItems, existingKeys);
            if (match is null)
                continue;

            ValidateNavigations(
                context,
                context.Entry(existingItem),
                match,
                aggregateType,
                guard,
                recursionPath);
        }
    }

    private static bool TryGetUpdatedNavigationValue(
        object updatedEntity,
        INavigationBase navMetadata,
        out object? updatedValue)
    {
        var navProperty = updatedEntity.GetType().GetProperty(navMetadata.Name);
        if (navProperty is null)
        {
            updatedValue = null;
            return false;
        }

        updatedValue = navProperty.GetValue(updatedEntity);
        return true;
    }

    private static bool HasScalarDifferences(EntityEntry existingEntry, object updatedEntity)
    {
        foreach (var property in existingEntry.Metadata.GetProperties()
            .Where(p => !p.IsShadowProperty()))
        {
            var existingValue = existingEntry.Property(property.Name).CurrentValue;
            var updatedValue = EntityKeyHelper.ReadDetachedPropertyValue(property, updatedEntity);
            if (!Equals(existingValue, updatedValue))
                return true;
        }

        return false;
    }

    private static bool ReferenceKeysMatch(DbContext context, object existingValue, object updatedValue)
    {
        var existingKeys = EntityKeyHelper.GetKeyValues(context.Entry(existingValue));
        var updatedKeys = EntityKeyHelper.GetKeyValues(context, updatedValue);
        return EntityKeyHelper.KeysEqual(existingKeys, updatedKeys);
    }

    private static bool IsNavigationBackToAggregateRoot(INavigationBase navMetadata, Type aggregateType)
    {
        return navMetadata is INavigation nav &&
               nav.TargetEntityType.ClrType == aggregateType;
    }

    private static string GetRelationshipTypeName(INavigationBase navMetadata)
    {
        if (navMetadata is ISkipNavigation) return "SkipNavigation";
        if (navMetadata is INavigation nav)
        {
            if (nav.IsCollection) return "OneToMany";
            return nav.ForeignKey.IsUnique ? "OneToOne" : "ManyToOne";
        }
        return "Unknown";
    }
}

internal enum NavigationClassification
{
    PureManyToMany,
    PayloadManyToMany,
    RequiredOneToOne,
    OptionalOneToOne,
    Unsupported
}
