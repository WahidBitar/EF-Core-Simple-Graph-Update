using Diwink.Extensions.EntityFrameworkCore.Exceptions;
using Diwink.Extensions.EntityFrameworkCore.GraphUpdate;
using FluentAssertions;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.GraphDiff;

/// <summary>
/// Unit tests for OperationGuard all-or-nothing behavior (FR-017).
/// </summary>
public class OperationGuardTests
{
    [Fact]
    public void ThrowIfErrors_with_no_errors_does_not_throw()
    {
        var guard = new OperationGuard();

        var act = () => guard.ThrowIfErrors();

        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowIfErrors_with_single_error_throws_that_error()
    {
        var guard = new OperationGuard();
        var error = new UnsupportedNavigationMutatedException("Course.Items", "OneToMany");

        guard.AddError(error);
        var act = () => guard.ThrowIfErrors();

        act.Should().Throw<UnsupportedNavigationMutatedException>()
            .Which.Should().BeSameAs(error);
    }

    [Fact]
    public void ThrowIfErrors_with_multiple_errors_throws_PartialMutationNotAllowed()
    {
        var guard = new OperationGuard();
        guard.AddError(new UnsupportedNavigationMutatedException("Course.Items", "OneToMany"));
        guard.AddError(new UnsupportedNavigationMutatedException("Catalog.Students", "OneToMany"));

        var act = () => guard.ThrowIfErrors();

        act.Should().Throw<PartialMutationNotAllowedException>();
    }

    [Fact]
    public void HasErrors_is_false_when_empty()
    {
        var guard = new OperationGuard();
        guard.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasErrors_is_true_after_adding_error()
    {
        var guard = new OperationGuard();
        guard.AddError(new UnsupportedNavigationMutatedException("X.Y", "OneToMany"));
        guard.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void Errors_collection_tracks_all_added_errors()
    {
        var guard = new OperationGuard();
        guard.AddError(new UnsupportedNavigationMutatedException("A.B", "OneToMany"));
        guard.AddError(new UnsupportedRelationshipPatternException("C.D", "Custom"));

        guard.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void AddError_with_null_throws_ArgumentNullException()
    {
        var guard = new OperationGuard();

        var act = () => guard.AddError(null!);

        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("error");
    }

    [Fact]
    public void ThrowIfErrors_with_mixed_error_types_throws_PartialMutationNotAllowed()
    {
        // Two different exception types — both unsupported navigation errors
        var guard = new OperationGuard();
        guard.AddError(new UnsupportedNavigationMutatedException("Catalog.Courses", "OneToMany"));
        guard.AddError(new UnloadedNavigationMutationException("Course.Tags", "Tags"));

        var act = () => guard.ThrowIfErrors();

        act.Should().Throw<PartialMutationNotAllowedException>()
            .Which.UnsupportedBranch.Should().Contain("Catalog.Courses",
                "multiple error paths should be listed in the unsupported branch description");
    }

    [Fact]
    public void ThrowIfErrors_multiple_errors_unsupported_branch_lists_all_paths()
    {
        var guard = new OperationGuard();
        guard.AddError(new UnsupportedNavigationMutatedException("A.B", "OneToMany"));
        guard.AddError(new UnsupportedNavigationMutatedException("C.D", "OneToMany"));
        guard.AddError(new UnsupportedNavigationMutatedException("E.F", "OneToMany"));

        var act = () => guard.ThrowIfErrors();

        act.Should().Throw<PartialMutationNotAllowedException>()
            .Which.UnsupportedBranch.Should().Contain("A.B").And.Contain("C.D").And.Contain("E.F");
    }

    [Fact]
    public void Errors_collection_is_readonly_and_reflects_added_order()
    {
        var guard = new OperationGuard();
        var err1 = new UnsupportedNavigationMutatedException("A.B", "OneToMany");
        var err2 = new UnloadedNavigationMutationException("C.D", "D");

        guard.AddError(err1);
        guard.AddError(err2);

        guard.Errors.Should().HaveCount(2);
        guard.Errors[0].Should().BeSameAs(err1);
        guard.Errors[1].Should().BeSameAs(err2);
    }
}