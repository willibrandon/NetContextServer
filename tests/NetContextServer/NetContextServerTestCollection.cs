using Xunit;

namespace NetContextServer.Tests;

[CollectionDefinition("NetContextServer Tests", DisableParallelization = true)]
public class NetContextServerTestCollection : ICollectionFixture<NetContextServerTestCollection>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
} 