using Microsoft.EntityFrameworkCore.Metadata;

namespace Diwink.Extensions.EntityFrameworkCore.RelationshipStrategies;

/// <summary>
/// Resolves one-to-one ownership semantics from EF Core metadata.
/// Determines whether a one-to-one relationship is required (delete on removal)
/// or optional (null/detach on removal) based on FK configuration.
/// </summary>
internal static class OneToOneOwnershipResolver
{
    /// <summary>
    /// Determines the removal behavior for a one-to-one relationship.
    /// </summary>
    /// <param name="foreignKey">The FK metadata for the one-to-one relationship.</param>
    /// <returns>
    /// <c>true</c> if the relationship is required (dependent should be deleted on removal);
    /// <c>false</c> if optional (FK should be nulled on removal).
    /// <summary>
    /// Determines whether the dependent end of the specified one-to-one relationship is required.
    /// </summary>
    /// <param name="foreignKey">EF Core foreign-key metadata representing the one-to-one relationship.</param>
    /// <returns>`true` if the dependent is required (cannot be null); `false` if the dependent is optional (foreign key can be nullified).</returns>
    public static bool IsRequiredDependent(IForeignKey foreignKey)
    {
        ArgumentNullException.ThrowIfNull(foreignKey);
        return foreignKey.IsRequired;
    }
}
