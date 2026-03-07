using Xunit;

// The suite shares one PostgreSQL container, so tests run sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
