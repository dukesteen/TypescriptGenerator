using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace TypescriptGenerator.Console.TsGen;

internal partial class Generator(ILogger<Generator> logger)
{
	public async ValueTask Execute(string csprojPath)
	{
		using var workspace = MSBuildWorkspace.Create();
		workspace.LoadMetadataForReferencedProjects = true;

		var project = await workspace.OpenProjectAsync(csprojPath);
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
		_ = DiscoverApiEndpoints(compilation);
		await Task.Run(() => { });
	}
}
