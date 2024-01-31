using System.Diagnostics.CodeAnalysis;

namespace IntegrationTests.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test class")]
public partial class DiscoverApiEndpoints(SharedFixture sharedFixture) : IntegrationTest(sharedFixture);
