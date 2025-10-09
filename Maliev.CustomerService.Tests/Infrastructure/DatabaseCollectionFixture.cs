namespace Maliev.CustomerService.Tests.Infrastructure;

/// <summary>
/// xUnit collection definition to share test database across all test classes
/// This ensures only one PostgreSQL container is started for all tests
/// </summary>
[CollectionDefinition("Database Collection")]
public class DatabaseCollectionFixture : ICollectionFixture<TestDatabaseFixture>
{
    // This class has no code, and is never created.
    // Its purpose is simply to be the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}
