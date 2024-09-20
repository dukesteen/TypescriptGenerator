using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

using TypescriptGenerator.Console.ImmediateApisTsGen.Templates;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types;

namespace TypescriptGenerator.Console.ImmediateApisTsGen;

internal partial class Generator(ILogger<Generator> logger, GeneratorConfig config)
{
	private List<EndpointDescriptor> EndpointDescriptors { get; set; } = [];
	private List<TypeDescriptor> TypeDescriptors { get; set; } = [];

	public async ValueTask Execute()
	{
		using var workspace = MSBuildWorkspace.Create();
		workspace.LoadMetadataForReferencedProjects = true;

		var project = await workspace.OpenProjectAsync(config.CsprojPath);
		var compilation = await project.GetCompilationAsync() ?? throw new InvalidOperationException("Failed to get compilation");

		var errorDiagnostics = compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).ToList();
		if (errorDiagnostics.Count != 0)
		{
			logger.LogError("Compilation failed with {Count} errors", errorDiagnostics.Count);
			foreach (var errorDiagnostic in errorDiagnostics)
				logger.LogError("{Message} @ {Location}", errorDiagnostic.GetMessage(), errorDiagnostic.Location.GetLineSpan());

			return;
		}

		await ExecuteInternal(compilation);
	}

	public async ValueTask Execute(Compilation compilation)
	{
		await ExecuteInternal(compilation);
	}

	internal async ValueTask ExecuteInternal(Compilation compilation)
	{
		logger.LogInformation("Starting TypeScript generator");
		logger.LogInformation("Compilation: {Compilation}", compilation.AssemblyName);
		EndpointDescriptors = DiscoverApiEndpoints(compilation);
		TypeDescriptors = DiscoverGeneratableTypes();
		var types = GenerateTypes();
		var endpoints = GenerateEndpoints();

		var apiClientTemplate = Utility.ApiClientTemplate;
		var apiClient = await apiClientTemplate.RenderAsync(new
		{
			ApiClientName = config.TsApiClientName,
			ApiClientImportPath = config.TsApiClientPath,
			Types = types,
			Endpoints = endpoints,
		});

		await File.WriteAllTextAsync(config.OutputPath, apiClient);

		logger.LogInformation("TypeScript generator completed");
	}
}
