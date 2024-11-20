using System.Text;

using TypescriptGenerator.Console.ImmediateApisTsGen.Types.Valibot;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Builders;

internal class ValibotObjectSchemaBuilder(string name)
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
			_ = stringBuilder.AppendLine($"const {name} = v.pipe(");
			_ = stringBuilder.AppendLine($"    v.object({{");
			foreach (var member in Members)
			{
				_ = stringBuilder.AppendLine($"        {member.Key}: {member.Value.ToString()},");
			}

			_ = stringBuilder.AppendLine($"    }}),");
			_ = stringBuilder.AppendLine($"    v.transform((input) => {{");
			_ = stringBuilder.AppendLine($"        return {{");
			_ = stringBuilder.AppendLine($"            ...input,");
			foreach (var member in Members)
			{
				if (member.Value == ValibotSchema.Array(ValibotSchema.InstantResponse))
				{
					_ = stringBuilder.AppendLine(
						$"            {member.Key}: input.{member.Key}.map((item) => input.{member.Key}.toZonedDateTimeISO(Temporal.TimeZone.from(item.timeZoneId))),");
				}
				else if (member.Value == ValibotSchema.Array(ValibotSchema.Optional(ValibotSchema.InstantResponse)))
				{
					_ = stringBuilder.AppendLine(
						$"            {member.Key}: input.{member.Key}.map((item) => item === null ? null : input.{member.Key}.toZonedDateTimeISO(Temporal.TimeZone.from(item.timeZoneId))),");
				}
				else if (member.Value == ValibotSchema.Optional(ValibotSchema.InstantResponse))
				{
					_ = stringBuilder.AppendLine(
						$"            {member.Key}: input.{member.Key}?.toZonedDateTimeISO(Temporal.TimeZone.from(input.timeZoneId)),");
				}
				else if (member.Value == ValibotSchema.InstantResponse)
				{
					_ = stringBuilder.AppendLine(
						$"            {member.Key}: input.{member.Key}.toZonedDateTimeISO(Temporal.TimeZone.from(input.timeZoneId)),");
				}
			}

			_ = stringBuilder.AppendLine($"        }};");
			_ = stringBuilder.AppendLine($"    }})");
			_ = stringBuilder.Append(");");
		}
		else
		{
			_ = stringBuilder.AppendLine($"const {name} = v.object({{");

			foreach (var member in Members)
			{
				_ = stringBuilder.AppendLine($"    {member.Key}: {member.Value.ToString()},");
			}

			_ = stringBuilder.Append("});");
		}

		return stringBuilder.ToString();
	}
}
