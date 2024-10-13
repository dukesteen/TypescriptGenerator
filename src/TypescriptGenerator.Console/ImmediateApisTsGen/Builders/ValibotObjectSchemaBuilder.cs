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

		if (Members.Any(x => x.Key == "timeZoneId"))
		{
			stringBuilder.AppendLine($"const {name} = v.pipe(");
			stringBuilder.AppendLine($"    v.object({{");
			foreach (var member in Members)
			{
				stringBuilder.AppendLine($"        {member.Key}: {member.Value.ToString()},");
			}

			stringBuilder.AppendLine($"    }}),");
			stringBuilder.AppendLine($"    v.transform((input) => {{");
			stringBuilder.AppendLine($"        return {{");
			stringBuilder.AppendLine($"            ...input,");
			foreach (var member in Members)
			{
				if (member.Value == ValibotSchema.Array(ValibotSchema.InstantResponse))
				{
					stringBuilder.AppendLine(
						$"            {member.Key}: input.{member.Key}.map((item) => input.{member.Key}.toZonedDateTimeISO(Temporal.TimeZone.from(item.timeZoneId))),");
				}
				else if (member.Value == ValibotSchema.Array(ValibotSchema.Optional(ValibotSchema.InstantResponse)))
				{
					stringBuilder.AppendLine(
						$"            {member.Key}: input.{member.Key}.map((item) => item === null ? null : input.{member.Key}.toZonedDateTimeISO(Temporal.TimeZone.from(item.timeZoneId))),");
				}
				else if (member.Value == ValibotSchema.Optional(ValibotSchema.InstantResponse))
				{
					stringBuilder.AppendLine(
						$"            {member.Key}: input.{member.Key}?.toZonedDateTimeISO(Temporal.TimeZone.from(input.timeZoneId)),");
				}
				else if (member.Value == ValibotSchema.InstantResponse)
				{
					stringBuilder.AppendLine(
						$"            {member.Key}: input.{member.Key}.toZonedDateTimeISO(Temporal.TimeZone.from(input.timeZoneId)),");
				}
			}

			stringBuilder.AppendLine($"        }};");
			stringBuilder.AppendLine($"    }})");
			stringBuilder.Append(");");
		}
		else
		{
			stringBuilder.AppendLine($"const {name} = v.object({{");

			foreach (var member in Members)
			{
				stringBuilder.AppendLine($"    {member.Key}: {member.Value.ToString()},");
			}

			stringBuilder.Append("});");
		}

		return stringBuilder.ToString();
	}
}
