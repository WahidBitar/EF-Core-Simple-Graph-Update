namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Base exception for all graph update contract violations.
/// </summary>
public abstract class GraphUpdateException : InvalidOperationException
{
    public string RelationshipPath { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="GraphUpdateException"/> with a specified error message and the relationship path related to the contract violation.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="relationshipPath">The relationship path associated with the graph update contract violation.</param>
    protected GraphUpdateException(string message, string relationshipPath)
        : base(message)
    {
        RelationshipPath = relationshipPath;
    }
}
