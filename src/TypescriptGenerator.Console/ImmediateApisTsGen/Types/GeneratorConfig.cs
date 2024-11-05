using System.Text.Json.Serialization;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types;

public class GeneratorConfig
{
	[JsonPropertyName("csprojPath")] public required string CsprojPath { get; init; }

	[JsonPropertyName("generateTypesInNamespacesIncludes")]
	public List<string> GenerateTypesInNamespacesIncludes { get; init; } = [];

	[JsonPropertyName("tsApiClientName")]
	public required string TsApiClientName { get; init; }

	[JsonPropertyName("tsApiClientPath")]
	public required string TsApiClientPath { get; init; }

	[JsonPropertyName("outputPath")]
	public required string OutputPath { get; init; }

	[JsonPropertyName("policiesOutputPath")]
	public required string PoliciesOutputPath { get; init; }
}
