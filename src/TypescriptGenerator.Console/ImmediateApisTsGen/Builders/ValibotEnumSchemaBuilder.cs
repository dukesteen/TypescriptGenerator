using System.Text;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Builders;

internal class ValibotEnumSchemaBuilder(string enumName, string enumSchemaName)
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
		_ = stringBuilder.AppendLine($"export enum {enumName} {{");

		foreach (var member in Members)
		{
			_ = stringBuilder.AppendLine($"    {member.Key} = {member.Value.ToString()},");
		}

		_ = stringBuilder.AppendLine("}");
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.Append($"const {enumSchemaName} = v.enum({enumName});");
		return stringBuilder.ToString();
	}
}
