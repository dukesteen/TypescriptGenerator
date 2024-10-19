using System.Text;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Builders;

public class TypescriptObjectBuilder(string name)
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
		stringBuilder.AppendLine($"export const {name} = {{");

		foreach (var member in Members)
		{
			stringBuilder.AppendLine($"    {member.Key}: '{member.Value}',");
		}

		stringBuilder.Append('}');
		return stringBuilder.ToString();
	}
}
