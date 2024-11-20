using System.Text;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Builders;

internal class TypescriptObjectBuilder(string name)
{
	private Dictionary<string, string> Members { get; } = [];

	public TypescriptObjectBuilder WithProperty(string propertyName, string propertyValue)
	{
		Members.Add(propertyName, propertyValue);
		return this;
	}

	public string Build()
	{
		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine($"export const {name} = {{");

		foreach (var member in Members)
		{
			_ = stringBuilder.AppendLine($"    {member.Key}: '{member.Value}',");
		}

		_ = stringBuilder.Append('}');
		return stringBuilder.ToString();
	}
}
