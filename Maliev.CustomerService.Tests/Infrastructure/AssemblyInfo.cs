using Xunit;

// Disable parallel test execution for integration tests
[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]
