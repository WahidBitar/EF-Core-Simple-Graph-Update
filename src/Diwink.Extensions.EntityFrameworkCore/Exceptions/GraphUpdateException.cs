namespace Diwink.Extensions.EntityFrameworkCore.Exceptions;

/// <summary>
/// Base exception for all graph update contract violations.
/// </summary>
public abstract class GraphUpdateException : InvalidOperationException
{
    public string RelationshipPath { get; }

    protected GraphUpdateException(string message, string relationshipPath)
        : base(message)
    {
        RelationshipPath = ValidateAndNormalize(relationshipPath, nameof(relationshipPath), "Relationship path");
    }

    protected static string ValidateAndNormalize(string? value, string paramName, string displayName)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);

        var normalizedValue = value.Trim();
        if (normalizedValue.Length == 0)
            throw new ArgumentException($"{displayName} cannot be empty or whitespace.", paramName);

        return normalizedValue;
    }
}
