namespace Diwink.Extensions.EntityFrameworkCore.Tests.Integration.Infrastructure;

/// <summary>
/// xUnit collection definition that shares a single SQL Server container
/// across all integration test classes in this collection.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<SqlServerContainerFixture>
{
    public const string Name = "SqlServerIntegration";
}
