using Xunit;

namespace GeorgiaERP.Tests.Integration;

/// <summary>
/// Shared collection so all integration test classes use the same ErpApiFactory instance.
/// This avoids the "logger is already frozen" Serilog error that occurs when multiple
/// WebApplicationFactory instances try to configure the bootstrap logger concurrently.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<ErpApiFactory>
{
}
