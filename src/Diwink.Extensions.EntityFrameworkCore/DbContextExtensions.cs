using Diwink.Extensions.EntityFrameworkCore.V2.GraphUpdate;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.V2;

/// <summary>
/// Public extension methods for EF Core graph update v2.
/// Accepts a detached object graph and diffs it against the tracked original (FR-001a).
/// </summary>
public static class DbContextExtensionsV2
{
    /// <summary>
    /// Updates the tracked <paramref name="existingEntity"/> graph to match the
    /// state of the detached <paramref name="updatedEntity"/> graph.
    ///
    /// Supported relationship patterns: pure many-to-many, many-to-many with
    /// payload, required one-to-one, optional one-to-one.
    ///
    /// Unsupported patterns in loaded navigations are silently skipped if unchanged,
    /// or rejected if mutations are detected (FR-018/FR-019).
    ///
    /// All-or-nothing semantics: if any mutation is unsupported, the entire
    /// operation is rejected before any changes are applied (FR-017).
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="context">The DbContext tracking the existing entity.</param>
    /// <param name="updatedEntity">Detached entity graph representing desired state.</param>
    /// <param name="existingEntity">Tracked entity graph loaded from the database.</param>
    /// <returns>The updated tracked entity.</returns>
    public static T InsertUpdateOrDeleteGraph<T>(
        this DbContext context,
        T updatedEntity,
        T existingEntity)
        where T : class
    {
        return GraphUpdateOrchestrator.UpdateGraph(context, updatedEntity, existingEntity);
    }
}
