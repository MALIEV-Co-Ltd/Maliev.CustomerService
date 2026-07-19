namespace Maliev.CustomerService.Tests.Infrastructure;

/// <summary>
/// xUnit collection definition to share test database across all test classes
/// This ensures only one PostgreSQL container is started for all tests
/// DisableParallelization ensures tests run serially to prevent database conflicts
/// </summary>
[CollectionDefinition("Database Collection", DisableParallelization = true)]
public class DatabaseCollectionFixture : ICollectionFixture<TestWebApplicationFactory>
{
    // This class has no code, and is never created.
    // Its purpose is simply to be the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}
