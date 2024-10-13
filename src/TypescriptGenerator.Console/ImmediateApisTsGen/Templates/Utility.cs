using System.Reflection;

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

	public static Template ApiClientTemplate { get; } = GetScribanTemplate("ApiClient");
	public static Template SchemaParseFunctionTemplate { get; } = GetScribanTemplate("SchemaParseFunction");
	public static Template FetcherFunctionTemplate { get; } = GetScribanTemplate("FetcherFunction");
	public static Template QueryFunctionTemplate { get; } = GetScribanTemplate("QueryFunction");
}
