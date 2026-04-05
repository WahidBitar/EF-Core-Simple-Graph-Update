using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using FluentAssertions;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.Exceptions;

/// <summary>
/// Unit tests for all GraphUpdateException-derived types, verifying constructor
/// argument storage, message content, property accessors, and inheritance chain.
/// </summary>
public class GraphUpdateExceptionTests
{
    // -------------------------------------------------------------------------
    // UnsupportedNavigationMutatedException
    // -------------------------------------------------------------------------

    [Fact]
    public void UnsupportedNavigationMutatedException_stores_relationship_path()
    {
        var ex = new UnsupportedNavigationMutatedException("Course.Items", "OneToMany");

        ex.RelationshipPath.Should().Be("Course.Items");
    }

    [Fact]
    public void UnsupportedNavigationMutatedException_stores_relationship_type()
    {
        var ex = new UnsupportedNavigationMutatedException("Course.Items", "OneToMany");

        ex.RelationshipType.Should().Be("OneToMany");
    }

    [Fact]
    public void UnsupportedNavigationMutatedException_message_contains_path_and_type()
    {
        var ex = new UnsupportedNavigationMutatedException("Catalog.Courses", "OneToMany");

        ex.Message.Should().Contain("Catalog.Courses");
        ex.Message.Should().Contain("OneToMany");
    }

    [Fact]
    public void UnsupportedNavigationMutatedException_inherits_from_GraphUpdateException()
    {
        var ex = new UnsupportedNavigationMutatedException("A.B", "OneToMany");

        ex.Should().BeAssignableTo<GraphUpdateException>();
        ex.Should().BeAssignableTo<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // UnloadedNavigationMutationException
    // -------------------------------------------------------------------------

    [Fact]
    public void UnloadedNavigationMutationException_stores_relationship_path()
    {
        var ex = new UnloadedNavigationMutationException("Course.Tags", "Tags");

        ex.RelationshipPath.Should().Be("Course.Tags");
    }

    [Fact]
    public void UnloadedNavigationMutationException_stores_navigation_name()
    {
        var ex = new UnloadedNavigationMutationException("Course.Tags", "Tags");

        ex.NavigationName.Should().Be("Tags");
    }

    [Fact]
    public void UnloadedNavigationMutationException_message_contains_path_and_navigation()
    {
        var ex = new UnloadedNavigationMutationException("Course.Tags", "Tags");

        ex.Message.Should().Contain("Course.Tags");
        ex.Message.Should().Contain("Tags");
    }

    [Fact]
    public void UnloadedNavigationMutationException_inherits_from_GraphUpdateException()
    {
        var ex = new UnloadedNavigationMutationException("A.B", "B");

        ex.Should().BeAssignableTo<GraphUpdateException>();
        ex.Should().BeAssignableTo<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // PartialMutationNotAllowedException
    // -------------------------------------------------------------------------

    [Fact]
    public void PartialMutationNotAllowedException_stores_relationship_path()
    {
        var ex = new PartialMutationNotAllowedException("Catalog.Courses", "Catalog.Courses");

        ex.RelationshipPath.Should().Be("Catalog.Courses");
    }

    [Fact]
    public void PartialMutationNotAllowedException_stores_unsupported_branch()
    {
        var ex = new PartialMutationNotAllowedException("Catalog.Courses", "Catalog.Courses, Catalog.Tags");

        ex.UnsupportedBranch.Should().Be("Catalog.Courses, Catalog.Tags");
    }

    [Fact]
    public void PartialMutationNotAllowedException_message_contains_unsupported_branch()
    {
        var ex = new PartialMutationNotAllowedException("Catalog.Courses", "Catalog.Courses");

        ex.Message.Should().Contain("Catalog.Courses");
    }

    [Fact]
    public void PartialMutationNotAllowedException_inherits_from_GraphUpdateException()
    {
        var ex = new PartialMutationNotAllowedException("A.B", "A.B");

        ex.Should().BeAssignableTo<GraphUpdateException>();
        ex.Should().BeAssignableTo<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // AmbiguousOwnershipSemanticsException
    // -------------------------------------------------------------------------

    [Fact]
    public void AmbiguousOwnershipSemanticsException_stores_relationship_path()
    {
        var ex = new AmbiguousOwnershipSemanticsException("Mentor.Workspace", "FK nullability unknown");

        ex.RelationshipPath.Should().Be("Mentor.Workspace");
    }

    [Fact]
    public void AmbiguousOwnershipSemanticsException_stores_missing_detail()
    {
        var ex = new AmbiguousOwnershipSemanticsException("Mentor.Workspace", "FK nullability unknown");

        ex.MissingDetail.Should().Be("FK nullability unknown");
    }

    [Fact]
    public void AmbiguousOwnershipSemanticsException_message_contains_path_and_detail()
    {
        var ex = new AmbiguousOwnershipSemanticsException("Mentor.Workspace", "FK nullability unknown");

        ex.Message.Should().Contain("Mentor.Workspace");
        ex.Message.Should().Contain("FK nullability unknown");
    }

    [Fact]
    public void AmbiguousOwnershipSemanticsException_inherits_from_GraphUpdateException()
    {
        var ex = new AmbiguousOwnershipSemanticsException("A.B", "detail");

        ex.Should().BeAssignableTo<GraphUpdateException>();
        ex.Should().BeAssignableTo<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // UnsupportedRelationshipPatternException
    // -------------------------------------------------------------------------

    [Fact]
    public void UnsupportedRelationshipPatternException_stores_relationship_path()
    {
        var ex = new UnsupportedRelationshipPatternException("Course.Modules", "SelfReferential");

        ex.RelationshipPath.Should().Be("Course.Modules");
    }

    [Fact]
    public void UnsupportedRelationshipPatternException_stores_pattern_identifier()
    {
        var ex = new UnsupportedRelationshipPatternException("Course.Modules", "SelfReferential");

        ex.PatternIdentifier.Should().Be("SelfReferential");
    }

    [Fact]
    public void UnsupportedRelationshipPatternException_message_contains_path_and_pattern()
    {
        var ex = new UnsupportedRelationshipPatternException("Course.Modules", "SelfReferential");

        ex.Message.Should().Contain("Course.Modules");
        ex.Message.Should().Contain("SelfReferential");
    }

    [Fact]
    public void UnsupportedRelationshipPatternException_inherits_from_GraphUpdateException()
    {
        var ex = new UnsupportedRelationshipPatternException("A.B", "pattern");

        ex.Should().BeAssignableTo<GraphUpdateException>();
        ex.Should().BeAssignableTo<InvalidOperationException>();
    }

    // -------------------------------------------------------------------------
    // Boundary / regression cases
    // -------------------------------------------------------------------------

    [Fact]
    public void All_exception_types_are_sealed_or_abstract_not_further_subclassable()
    {
        // Verify all concrete exception types are sealed (design intent)
        typeof(UnsupportedNavigationMutatedException).IsSealed.Should().BeTrue();
        typeof(UnloadedNavigationMutationException).IsSealed.Should().BeTrue();
        typeof(PartialMutationNotAllowedException).IsSealed.Should().BeTrue();
        typeof(AmbiguousOwnershipSemanticsException).IsSealed.Should().BeTrue();
        typeof(UnsupportedRelationshipPatternException).IsSealed.Should().BeTrue();
    }

    [Fact]
    public void GraphUpdateException_is_abstract()
    {
        typeof(GraphUpdateException).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void UnsupportedNavigationMutatedException_rejects_empty_relationship_path()
    {
        var act = () => new UnsupportedNavigationMutatedException(string.Empty, "OneToMany");

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("relationshipPath");
    }

    [Fact]
    public void UnsupportedNavigationMutatedException_rejects_blank_relationship_type()
    {
        var act = () => new UnsupportedNavigationMutatedException("Course.Items", " ");

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("relationshipType");
    }

    [Fact]
    public void UnloadedNavigationMutationException_navigation_name_differs_from_path_segment()
    {
        // RelationshipPath is the full dotted path; NavigationName is just the nav property name
        var ex = new UnloadedNavigationMutationException("Course.Policy.Revision", "Revision");

        ex.RelationshipPath.Should().Be("Course.Policy.Revision");
        ex.NavigationName.Should().Be("Revision");
        ex.RelationshipPath.Should().NotBe(ex.NavigationName);
    }
}
