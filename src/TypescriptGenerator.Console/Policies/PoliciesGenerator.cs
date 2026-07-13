using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

using TypescriptGenerator.Console.ImmediateApisTsGen.Extensions;
using TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types;
using TypescriptGenerator.Console.Policies.SyntaxWalkers;

namespace TypescriptGenerator.Console.Policies;

internal class PoliciesGenerator(ILogger<PoliciesGenerator> logger, GeneratorConfig config)
{
	public async ValueTask<int> Execute()
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

			return 1;
		}

		return await ExecuteInternal(compilation);
	}

	internal async ValueTask<int> Execute(Compilation compilation)
	{
		return await ExecuteInternal(compilation);
	}

	internal async ValueTask<int> ExecuteInternal(Compilation compilation)
	{
		logger.LogInformation("Starting policies generator");

		var policies = new List<INamedTypeSymbol>();
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			var root = await syntaxTree.GetRootAsync();
			var semanticModel = compilation.GetSemanticModel(syntaxTree);
			var classDeclarationCollector = new PolicyDeclarationWalker(semanticModel, config.GeneratePermissionPolicyAttribute);
			classDeclarationCollector.Visit(root);
			policies.AddRange(classDeclarationCollector.PolicyDeclarations);
		}
		logger.LogInformation("Found {Count} policies", policies.Count);

		policies = policies.Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>().ToList();

		var policyBuilder = new StringBuilder();
		_ = policyBuilder.AppendLine("export const policies = {");

		foreach (var policy in policies)
		{
			var policyValue = policy.GetAttributeWithFullyQualifiedName(config.GeneratePermissionPolicyAttribute)
				.ConstructorArguments[0].Value?.ToString() ?? policy.Name;

			_ = policyBuilder.AppendLine($"    {policy.Name.ToCamelCase()}: \"{policyValue}\",");
		}

		_ = policyBuilder.AppendLine("};");

		var fullPoliciesOutputPath = Path.GetFullPath(config.PoliciesOutputPath);
		var outputDirectory = Path.GetDirectoryName(fullPoliciesOutputPath)
			?? throw new InvalidOperationException("Policies output path does not have a directory.");

		Directory.CreateDirectory(outputDirectory);
		await File.WriteAllTextAsync(fullPoliciesOutputPath, policyBuilder.ToString());

		logger.LogInformation("Policies generator finished");

		return 0;
	}
}
