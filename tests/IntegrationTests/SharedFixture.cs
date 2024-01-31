using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

using TypescriptGenerator.Console.TsGen;

namespace IntegrationTests;

public class SharedFixture : IAsyncLifetime
{
	public ServiceProvider Services { get; set; } = null!;

	public Task InitializeAsync()
	{
		_ = MSBuildLocator.RegisterDefaults();

		var serviceCollectionBuilder =
			new ServiceCollection()
				.AddSingleton<Generator>()
				.AddLogging(lsp => lsp.ClearProviders()
					.AddSerilog(new LoggerConfiguration().CreateLogger()));

		Services = serviceCollectionBuilder.BuildServiceProvider();

		return Task.CompletedTask;
	}

	public Task DisposeAsync()
	{
		return Task.CompletedTask;
	}
}
