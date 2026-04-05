using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using System.Collections.ObjectModel;

namespace Diwink.Extensions.EntityFrameworkCore.GraphUpdate;

/// <summary>
/// Enforces all-or-nothing rejection semantics (FR-017).
/// Collects validation errors during graph analysis before any mutations
/// are applied. If any error is found, the entire operation is rejected.
/// </summary>
internal sealed class OperationGuard
{
    private readonly List<GraphUpdateException> _errors = [];
    private readonly ReadOnlyCollection<GraphUpdateException> _readonlyErrors;

    public OperationGuard()
    {
        _readonlyErrors = _errors.AsReadOnly();
    }

    public bool HasErrors => _errors.Count > 0;

    public IReadOnlyList<GraphUpdateException> Errors => _readonlyErrors;

    /// <summary>
    /// Records a validation error. No mutations should be applied until
    /// <see cref="ThrowIfErrors"/> is called and passes.
    /// <summary>
    /// Adds a graph validation error to the guard so it can be enforced later.
    /// </summary>
    /// <param name="error">The validation error to record; cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public void AddError(GraphUpdateException error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _errors.Add(error);
    }

    /// <summary>
    /// Throws the first recorded error if any exist, enforcing all-or-nothing
    /// rejection before mutations are applied to the change tracker.
    /// <summary>
    /// Enforces collected validation errors by throwing an appropriate exception when any errors were recorded.
    /// </summary>
    /// <remarks>
    /// If no errors are recorded, the method returns without side effects.
    /// </remarks>
    /// <exception cref="GraphUpdateException">Thrown when exactly one validation error was recorded; the single recorded exception is rethrown.</exception>
    /// <exception cref="PartialMutationNotAllowedException">Thrown when multiple validation errors were recorded; constructed with the first recorded error's RelationshipPath and a comma-separated list of all recorded RelationshipPath values for diagnostic context.</exception>
    public void ThrowIfErrors()
    {
        if (_errors.Count == 0)
            return;

        if (_errors.Count == 1)
            throw _errors[0];

        // When multiple errors exist, wrap in PartialMutationNotAllowed
        // with all unsupported branches listed for diagnostic clarity
        var allPaths = string.Join(", ", _errors.Select(e => e.RelationshipPath));
        throw new PartialMutationNotAllowedException(
            _errors[0].RelationshipPath,
            allPaths);
    }
}
