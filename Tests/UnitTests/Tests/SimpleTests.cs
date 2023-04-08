
namespace UnitTests.Tests;

[TestCaseOrderer(ordererTypeName: "UnitTests.PriorityOrderer", ordererAssemblyName: "UnitTests")]
public class SimpleTests : IClassFixture<TestDbFixture>
{
    [Fact]
    [TestPriority(2)]
    public void Test1()
    {
        Assert.True(true);
    }

    [Fact]
    [TestPriority(2)]
    public void Test2()
    {
        Assert.True(true);
    }

    [Fact]
    [TestPriority(1)]
    public void Test3()
    {
        Assert.True(true);
    }
}