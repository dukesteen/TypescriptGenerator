using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace IntegrationTests;

public static class CSharpCompilationHelpers
{
	public static async ValueTask<Compilation> CreateCompilationAsync(string source)
	{
		_ = MSBuildLocator.RegisterDefaults();
		using var workspace = MSBuildWorkspace.Create();
		// workspace.LoadMetadataForReferencedProjects = true;

		var solnRoot = Directory.GetCurrentDirectory()[..Environment.CurrentDirectory.IndexOf("bin", StringComparison.Ordinal)];
		var project = await workspace.OpenProjectAsync(Path.Combine(solnRoot,
			"../EmptyWebApplication/EmptyWebApplication.csproj"));

		var compilation = await project.GetCompilationAsync() ?? throw new InvalidOperationException("Failed to get compilation");

		compilation = compilation.AddSyntaxTrees([CSharpSyntaxTree.ParseText(source)]);

		var errorDiagnostics = compilation.GetDiagnostics().Where(x => x.WarningLevel == 0).ToList();
		if (errorDiagnostics.Count != 0)
		{
			errorDiagnostics.ForEach(x => Console.WriteLine($"{x.GetMessage()} @ {x.Location.GetLineSpan()}"));
			throw new InvalidOperationException("Compilation failed");
		}

		return compilation;
	}
}
