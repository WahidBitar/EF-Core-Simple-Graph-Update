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
    /// <summary>
    /// Orchestrates validation and application of changes from a detached updated entity graph onto an existing tracked entity graph.
    /// </summary>
    /// <param name="context">The DbContext that is tracking <paramref name="existingEntity"/> and provides EF metadata and change tracking.</param>
    /// <param name="updatedEntity">A detached entity graph containing proposed scalar and navigation changes.</param>
    /// <param name="existingEntity">The tracked root entity whose scalar properties and loaded navigations will be updated.</param>
    /// <returns>The same <paramref name="existingEntity"/> instance after applying validated updates from <paramref name="updatedEntity"/>.</returns>
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
        existingEntry.CurrentValues.SetValues(updatedEntity);
        ApplyNavigations(context, existingEntry, updatedEntity, aggregateType);

        return existingEntity;
    }

    /// <summary>
    /// Validates navigation mutation legality on a tracked entity against a detached updated graph and records any violations in the provided guard.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> used to resolve metadata and keys.</param>
    /// <param name="existingEntry">The tracked <see cref="EntityEntry"/> whose navigations are being validated.</param>
    /// <param name="updatedEntity">The detached updated entity graph to inspect for attempted navigation mutations.</param>
    /// <param name="aggregateType">The aggregate root CLR type used to ignore navigations that point back to the aggregate root.</param>
    /// <param name="guard">An <see cref="OperationGuard"/> that will collect validation errors (e.g., unsupported navigation mutations, attempts to mutate unloaded navigations).</param>
    /// <param name="recursionPath">Optional set used to detect and prevent cycles during recursive validation; callers may supply a shared set for the top-level traversal.</param>
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
    /// <summary>
    /// Determines whether the detached updated entity attempted to mutate the specified unloaded navigation.
    /// </summary>
    /// <param name="updatedEntity">The detached entity graph provided for the update.</param>
    /// <param name="navMetadata">Metadata for the navigation property being inspected.</param>
    /// <returns>`true` if the navigation property exists on the detached entity and represents an attempted mutation (non-null reference or a collection with any items), `false` otherwise.</returns>
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

    /// <summary>
    /// Applies navigation property updates from a detached updated entity onto a tracked existing entity entry.
    /// </summary>
    /// <param name="updatedEntity">The detached updated entity graph to read navigation values from.</param>
    /// <param name="aggregateType">The aggregate root CLR type used to identify navigations that point back to the aggregate root and should be skipped.</param>
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

    /// <summary>
    /// Applies updates to a loaded collection navigation on the tracked entity using the appropriate many-to-many strategy.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> used for attach/relationship operations.</param>
    /// <param name="existingNavigation">The tracked collection navigation to update.</param>
    /// <param name="updatedValue">The detached navigation value from the updated graph; treated as an empty collection when null.</param>
    /// <param name="classification">The navigation classification that determines which many-to-many strategy to apply.</param>
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

    /// <summary>
    /// Applies an updated reference navigation value to a tracked reference navigation, performing attach, replace, update, detach, or remove actions according to the navigation classification and the presence of existing and updated values.
    /// </summary>
    /// <param name="existingNavigation">The tracked reference navigation entry to update.</param>
    /// <param name="updatedValue">The detached navigation value from the updated graph, or null to indicate removal.</param>
    /// <param name="classification">The relationship classification for the navigation which determines attach/replace/remove semantics.</param>
    /// <param name="aggregateType">The aggregate root CLR type used when processing nested navigations.</param>
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
            childEntry.CurrentValues.SetValues(updatedValue);
            ApplyNavigations(context, childEntry, updatedValue, aggregateType);
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
    /// <summary>
    /// Classifies the given navigation metadata into a relationship pattern used to drive graph update behavior.
    /// </summary>
    /// <param name="navMetadata">Metadata for the navigation property to classify.</param>
    /// <returns>
    /// A NavigationClassification value indicating the relationship pattern:
    /// `PureManyToMany`, `PayloadManyToMany`, `RequiredOneToOne`, `OptionalOneToOne`, or `Unsupported`.
    /// </returns>
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
    /// <summary>
    /// Determines whether the given entity type represents an explicit many-to-many join entity that carries payload.
    /// </summary>
    /// <param name="entityType">The EF Core entity metadata to inspect.</param>
    /// <returns>`true` if the entity has a primary key with at least two properties and every primary key property is included in at least one foreign key; `false` otherwise.</returns>
    private static bool IsPayloadJoinEntity(IEntityType entityType)
    {
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey is null || primaryKey.Properties.Count < 2)
            return false;

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var allKeyPropsAreFk = primaryKey.Properties.All(keyProp =>
            foreignKeys.Any(fk => fk.Properties.Contains(keyProp)));

        if (!allKeyPropsAreFk)
            return false;

        var foreignKeyProperties = foreignKeys
            .SelectMany(fk => fk.Properties)
            .ToHashSet();

        var hasPayloadProperty = entityType.GetProperties().Any(property =>
            !primaryKey.Properties.Contains(property) &&
            !foreignKeyProperties.Contains(property));

        return hasPayloadProperty;
    }

    /// <summary>
    /// Checks whether a loaded navigation has mutations between existing and updated state.
    /// Used for FR-018 to detect changes in unsupported navigation types.
    /// <summary>
    /// Determines whether the detached updated entity proposes mutations for the specified loaded navigation.
    /// </summary>
    /// <param name="navigation">The tracked navigation entry on the existing entity to compare against.</param>
    /// <param name="updatedEntity">The detached updated entity containing the candidate navigation value.</param>
    /// <param name="navMetadata">Metadata for the navigation property being inspected.</param>
    /// <returns>`true` if the updated entity contains mutations for the navigation — for collections: differing counts, a missing primary-key match, or scalar property differences on matched items; for references: null/non-null changes, differing primary keys, or scalar property differences; `false` otherwise.</returns>
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

    /// <summary>
    /// Recursively validates loaded child navigations when the detached updated entity provides values for them.
    /// </summary>
    /// <param name="context">The DbContext used to obtain tracked entries and metadata.</param>
    /// <param name="navigation">The loaded navigation on the tracked entity to validate against the detached value.</param>
    /// <param name="updatedEntity">The detached entity (or detached navigation value) that may contain proposed child values.</param>
    /// <param name="aggregateType">The aggregate root CLR type used to detect navigations that point back to the aggregate root.</param>
    /// <param name="guard">An OperationGuard that accumulates validation errors found during traversal.</param>
    /// <param name="recursionPath">A reference-equality HashSet used to track visited entities and prevent recursive cycles during validation.</param>
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

    /// <summary>
    /// Attempts to read the value of the navigation property named by <paramref name="navMetadata"/> from <paramref name="updatedEntity"/>.
    /// </summary>
    /// <param name="updatedEntity">The detached entity to read the navigation value from.</param>
    /// <param name="navMetadata">Metadata describing the navigation property to read (its Name is used to locate the CLR property).</param>
    /// <param name="updatedValue">The value of the navigation property if found; otherwise null.</param>
    /// <returns>`true` if the navigation property exists on <paramref name="updatedEntity"/> and its value was retrieved; `false` otherwise.</returns>
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

    /// <summary>
    /// Determines whether any non-shadow scalar property values differ between the tracked entity entry and the detached updated entity.
    /// </summary>
    /// <param name="existingEntry">The tracked entity entry to compare against.</param>
    /// <param name="updatedEntity">The detached entity containing proposed property values.</param>
    /// <returns>`true` if any scalar (non-shadow) property value differs between the tracked entry and the detached entity, `false` otherwise.</returns>
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

    /// <summary>
    /// Determines whether the primary key values of a tracked entity and a detached entity match.
    /// </summary>
    /// <param name="existingValue">The tracked entity instance whose key values are read from the context.</param>
    /// <param name="updatedValue">The detached entity instance whose key values are read for comparison.</param>
    /// <returns>`true` if the primary key values are equal, `false` otherwise.</returns>
    private static bool ReferenceKeysMatch(DbContext context, object existingValue, object updatedValue)
    {
        var existingKeys = EntityKeyHelper.GetKeyValues(context.Entry(existingValue));
        var updatedKeys = EntityKeyHelper.GetKeyValues(context, updatedValue);
        return EntityKeyHelper.KeysEqual(existingKeys, updatedKeys);
    }

    /// <summary>
    /// Determines whether the navigation's target entity type is the aggregate root type.
    /// </summary>
    /// <param name="navMetadata">The navigation metadata to inspect.</param>
    /// <param name="aggregateType">The aggregate root CLR type to compare against.</param>
    /// <returns>`true` if the navigation's target entity CLR type equals <paramref name="aggregateType"/>, `false` otherwise.</returns>
    private static bool IsNavigationBackToAggregateRoot(INavigationBase navMetadata, Type aggregateType)
    {
        return navMetadata is INavigation nav &&
               nav.TargetEntityType.ClrType == aggregateType;
    }

    /// <summary>
    /// Get a human-readable relationship type name for the given navigation metadata.
    /// </summary>
    /// <param name="navMetadata">The navigation metadata to classify.</param>
    /// <returns>
    /// One of: "SkipNavigation", "OneToMany", "OneToOne", "ManyToOne", or "Unknown" describing the relationship type.
    /// </returns>
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
