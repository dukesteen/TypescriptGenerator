using System.Diagnostics.CodeAnalysis;

namespace IntegrationTests;

[CollectionDefinition("SharedFixture")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit convention")]
public sealed class SharedFixtureCollection : ICollectionFixture<SharedFixture>;
