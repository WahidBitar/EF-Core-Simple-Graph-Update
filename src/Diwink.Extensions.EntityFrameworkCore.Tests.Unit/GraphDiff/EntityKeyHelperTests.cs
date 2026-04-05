using Diwink.Extensions.EntityFrameworkCore.GraphUpdate;
using Diwink.Extensions.EntityFrameworkCore.TestModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.Tests.Unit.GraphDiff;

public class EntityKeyHelperTests
{
    private static TestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public void KeysEqual_compares_byte_arrays_structurally()
    {
        var left = new object[] { new byte[] { 1, 2, 3 }, 42 };
        var right = new object[] { new byte[] { 1, 2, 3 }, 42 };

        EntityKeyHelper.KeysEqual(left, right).Should().BeTrue();
    }

    [Fact]
    public void KeysEqual_returns_false_when_only_one_side_is_a_byte_array()
    {
        var left = new object[] { new byte[] { 1, 2, 3 } };
        var right = new object[] { "AQID" };

        EntityKeyHelper.KeysEqual(left, right).Should().BeFalse();
    }

    [Fact]
    public void GetKeyValues_for_unmapped_entity_throws()
    {
        using var context = CreateInMemoryContext();

        var act = () => EntityKeyHelper.GetKeyValues(context, new UnmappedEntity());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not exist in the current DbContext model*");
    }

    private sealed class UnmappedEntity;
}
