using System.Text.Json;

using Cocona;

using Microsoft.Build.Locator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

using TypescriptGenerator.Console.ImmediateApisTsGen.Types;

using Generator = TypescriptGenerator.Console.ImmediateApisTsGen.Generator;

MSBuildLocator.RegisterDefaults();

var builder = CoconaApp.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", false, true);

var loggerConfiguration = new LoggerConfiguration()
	.ReadFrom.Configuration(builder.Configuration)
	.CreateLogger();

Log.Logger = loggerConfiguration;

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration);

var app = builder.Build();

app.AddCommand("generate", async (
	string configPath,
	[FromService] ILogger<Generator> logger,
	[FromService] IServiceProvider serviceProvider) =>
{
	var configText = await File.ReadAllTextAsync(configPath);
	var config = JsonSerializer.Deserialize<GeneratorConfig>(configText);

	if (config == null)
	{
		logger.LogError("Failed to deserialize config");
		return 2;
	}

	var generator = ActivatorUtilities.CreateInstance<Generator>(serviceProvider, config);
	return await generator.Execute();
});

app.Run();
