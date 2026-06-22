using Xunit;

namespace GeorgiaERP.Tests.Integration;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<ErpApiFactory> { }
