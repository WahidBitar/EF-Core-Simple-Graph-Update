using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using FluentAssertions;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.GraphDiff;

public class ExceptionValidationTests
{
    [Fact]
    public void UnsupportedNavigationMutatedException_rejects_blank_relationship_path()
    {
        var act = () => new UnsupportedNavigationMutatedException("   ", "OneToMany");

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("relationshipPath");
    }

    [Fact]
    public void AmbiguousOwnershipSemanticsException_rejects_blank_missing_detail()
    {
        var act = () => new AmbiguousOwnershipSemanticsException("Course.Policy", " ");

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("missingDetail");
    }

    [Fact]
    public void PartialMutationNotAllowedException_rejects_blank_unsupported_branch()
    {
        var act = () => new PartialMutationNotAllowedException("Course.Tags", " ");

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("unsupportedBranch");
    }

    [Fact]
    public void UnsupportedRelationshipPatternException_rejects_blank_pattern_identifier()
    {
        var act = () => new UnsupportedRelationshipPatternException("Course.Tags", " ");

        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("patternIdentifier");
    }
}
