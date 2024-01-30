using Cocona;

using Microsoft.Build.Locator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

using TypescriptGenerator.Console.TsGen;

MSBuildLocator.RegisterDefaults();

var builder = CoconaApp.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", false, true);

var loggerConfiguration = new LoggerConfiguration()
	.ReadFrom.Configuration(builder.Configuration)
	.CreateLogger();

Log.Logger = loggerConfiguration;

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfiguration);

builder.Services.AddSingleton<Generator>();

var app = builder.Build();

app.AddCommand("generate", async (string csprojPath, [FromService] Generator generator) => await generator.Execute(csprojPath));

app.Run();
