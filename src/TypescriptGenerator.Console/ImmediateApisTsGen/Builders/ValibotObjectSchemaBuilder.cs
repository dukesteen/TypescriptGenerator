using System.Text;

using TypescriptGenerator.Console.ImmediateApisTsGen.Types.Valibot;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Builders;

public class ValibotObjectSchemaBuilder(string name)
{
	private Dictionary<string, ValibotSchema> Members { get; } = [];

	public ValibotObjectSchemaBuilder WithProperty(string propertyName, ValibotSchema schema)
	{
		Members.Add(propertyName, schema);
		return this;
	}

	public string Build()
	{
		var stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"const {name} = v.object({{");

		foreach (var member in Members)
		{
			stringBuilder.AppendLine($"    {member.Key}: {member.Value.ToString()},");
		}

		stringBuilder.Append("});");
		return stringBuilder.ToString();
	}
}
