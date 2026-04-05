using Diwink.Extensions.EntityFrameworkCore.Exceptions;

namespace Diwink.Extensions.EntityFrameworkCore.GraphUpdate;

/// <summary>
/// Enforces all-or-nothing rejection semantics (FR-017).
/// Collects validation errors during graph analysis before any mutations
/// are applied. If any error is found, the entire operation is rejected.
/// </summary>
internal sealed class OperationGuard
{
    private readonly List<GraphUpdateException> _errors = [];

    public bool HasErrors => _errors.Count > 0;

    public IReadOnlyList<GraphUpdateException> Errors => _errors;

    /// <summary>
    /// Records a validation error. No mutations should be applied until
    /// <see cref="ThrowIfErrors"/> is called and passes.
    /// </summary>
    public void AddError(GraphUpdateException error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _errors.Add(error);
    }

    /// <summary>
    /// Throws the first recorded error if any exist, enforcing all-or-nothing
    /// rejection before mutations are applied to the change tracker.
    /// </summary>
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
