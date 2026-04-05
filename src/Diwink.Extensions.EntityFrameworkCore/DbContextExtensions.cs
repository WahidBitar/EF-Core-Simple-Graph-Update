using Diwink.Extensions.EntityFrameworkCore.GraphUpdate;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore;

/// <summary>
/// Public extension methods for EF Core graph update v2.
/// Accepts a detached object graph and diffs it against the tracked original (FR-001a).
/// </summary>
public static class DbContextExtensions
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
    /// <summary>
    /// Synchronizes the tracked entity graph represented by <paramref name="existingEntity"/> so it matches the detached <paramref name="updatedEntity"/> graph.
    /// </summary>
    /// <param name="updatedEntity">The detached entity graph containing the desired state.</param>
    /// <param name="existingEntity">The already-tracked entity graph to be updated to match <paramref name="updatedEntity"/>.</param>
    /// <returns>The updated tracked entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/>, <paramref name="updatedEntity"/>, or <paramref name="existingEntity"/> is null.</exception>
    public static T UpdateGraph<T>(
        this DbContext context,
        T updatedEntity,
        T existingEntity)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(updatedEntity);
        ArgumentNullException.ThrowIfNull(existingEntity);

        return GraphUpdateOrchestrator.UpdateGraph(context, updatedEntity, existingEntity);
    }
}
