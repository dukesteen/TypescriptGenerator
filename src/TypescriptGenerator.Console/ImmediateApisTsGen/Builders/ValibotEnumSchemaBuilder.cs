using System.Text;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Builders;

public class ValibotEnumSchemaBuilder(string enumName, string enumSchemaName)
{
	private Dictionary<string, int> Members { get; } = [];

	public ValibotEnumSchemaBuilder WithMember(string propertyName, int schema)
	{
		Members.Add(propertyName, schema);
		return this;
	}

	public string Build()
	{
		var stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"enum {enumName} {{");

		foreach (var member in Members)
		{
			stringBuilder.AppendLine($"    {member.Key} = {member.Value.ToString()},");
		}

		stringBuilder.AppendLine("}");
		stringBuilder.AppendLine();
		stringBuilder.Append($"const {enumSchemaName} = v.enum({enumName});");
		return stringBuilder.ToString();
	}
}
