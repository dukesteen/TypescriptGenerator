global using System;
using System.Diagnostics;
using System.Text.Json;

using Cocona;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

using TypescriptGenerator.Console.ImmediateApisTsGen.Types;
using TypescriptGenerator.Console.Policies;

using Generator = TypescriptGenerator.Console.ImmediateApisTsGen.Generator;

MSBuildLocator.RegisterDefaults();

var builder = CoconaApp.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
	.MinimumLevel.Information()
	.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
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
	var totalStopwatch = Stopwatch.StartNew();
	var stepStopwatch = Stopwatch.StartNew();

	var configText = await File.ReadAllTextAsync(configPath);
	var config = JsonSerializer.Deserialize<GeneratorConfig>(configText);

	if (config == null)
	{
		logger.LogError("Failed to deserialize config");
		return 2;
	}

	// Open project ONCE
	var properties = new Dictionary<string, string>
	{
		{ "DesignTimeBuild", "true" },
		{ "BuildProjectReferences", "false" },
		{ "SkipCompilerExecution", "true" },
		{ "RunAnalyzers", "false" },
		{ "RunAnalyzersDuringBuild", "false" },
	};

	using var workspace = MSBuildWorkspace.Create(properties);
	workspace.LoadMetadataForReferencedProjects = true;

	logger.LogInformation("Opening project...");
	stepStopwatch.Restart();
	var project = await workspace.OpenProjectAsync(config.CsprojPath);
	logger.LogInformation("Opened project in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

	logger.LogInformation("Getting compilation...");
	stepStopwatch.Restart();
	var compilation = await project.GetCompilationAsync()
		?? throw new InvalidOperationException("Failed to get compilation");
	logger.LogInformation("Got compilation in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

	var errorDiagnostics = compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).ToList();
	if (errorDiagnostics.Count != 0)
	{
		foreach (var errorDiagnostic in errorDiagnostics)
			logger.LogError("{Message} @ {Location}", errorDiagnostic.GetMessage(), errorDiagnostic.Location.GetLineSpan());
		return 1;
	}

	// Pass compilation to both generators
	logger.LogInformation("Running policies generator...");
	stepStopwatch.Restart();
	var policyGenerator = ActivatorUtilities.CreateInstance<PoliciesGenerator>(serviceProvider, config);
	var result = await policyGenerator.Execute(compilation);
	logger.LogInformation("Policies generator completed in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

	logger.LogInformation("Running TypeScript generator...");
	stepStopwatch.Restart();
	var generator = ActivatorUtilities.CreateInstance<Generator>(serviceProvider, config);
	result = await generator.Execute(compilation);
	logger.LogInformation("TypeScript generator completed in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

	logger.LogInformation("Total time: {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);
	return result;
});

app.Run();
