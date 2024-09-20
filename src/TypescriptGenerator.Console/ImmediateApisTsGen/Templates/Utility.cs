using System.Reflection;

using Microsoft.CodeAnalysis;

using Scriban;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Templates;

public static class Utility
{
	private static Template GetScribanTemplate(string templateName)
	{
		using var stream = Assembly
			.GetExecutingAssembly()
			.GetManifestResourceStream(
				typeof(Utility),
				$"{templateName}.sbntxt"
			)!;

		using var reader = new StreamReader(stream);
		return Template.Parse(reader.ReadToEnd());
	}

	public static IncrementalValuesProvider<T> WhereNotNull<T>(
		this IncrementalValuesProvider<T?> provider
	) where T : struct =>
		provider
			.Where(x => x != null)
			.Select((x, _) => x!.Value);

	public static Template ApiClientTemplate => GetScribanTemplate("ApiClient");
	public static Template SchemaParseFunctionTemplate => GetScribanTemplate("SchemaParseFunction");
	public static Template FetcherFunctionTemplate => GetScribanTemplate("FetcherFunction");
	public static Template QueryFunctionTemplate => GetScribanTemplate("QueryFunction");
}
