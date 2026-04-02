namespace Diwink.Extensions.EntityFrameworkCore.V2.Exceptions;

/// <summary>
/// Base exception for all graph update contract violations.
/// </summary>
public abstract class GraphUpdateException : InvalidOperationException
{
    public string RelationshipPath { get; }

    protected GraphUpdateException(string message, string relationshipPath)
        : base(message)
    {
        RelationshipPath = relationshipPath;
    }
}
