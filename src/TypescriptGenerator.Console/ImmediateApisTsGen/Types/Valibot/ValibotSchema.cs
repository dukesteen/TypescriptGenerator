namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types.Valibot;

public record ValibotSchema
{
	public required string Name { get; init; }
	public bool IsValibotMethod { get; init; }
	public EquatableReadOnlyList<ValibotSchema> Members { get; init; } = [];

	public override string ToString()
	{
		if (IsValibotMethod)
			return $"v.{Name}({string.Join(", ", Members.Select(x => x.ToString()).ToList())})";
		return Name;
	}

	public static ValibotSchema Array(ValibotSchema schema) => new() { Name = "array", IsValibotMethod = true, Members = new List<ValibotSchema>([schema]).ToEquatableReadOnlyList() };
	public static ValibotSchema Map(ValibotSchema keyType, ValibotSchema valueType) => new() { Name = "map", IsValibotMethod = true, Members = new List<ValibotSchema>([keyType, valueType]).ToEquatableReadOnlyList() };
	public static ValibotSchema Optional(ValibotSchema schema) => new() { Name = "nullable", IsValibotMethod = true, Members = new List<ValibotSchema>([schema]).ToEquatableReadOnlyList() };
	public static ValibotSchema Pipe(ValibotSchema schema1, ValibotSchema schema2) => new() { Name = "pipe", IsValibotMethod = true, Members = new List<ValibotSchema>([schema1, schema2]).ToEquatableReadOnlyList() };
	public static ValibotSchema Pipe(ValibotSchema schema1, ValibotSchema schema2, ValibotSchema schema3) => new() { Name = "pipe", IsValibotMethod = true, Members = new List<ValibotSchema>([schema1, schema2, schema3]).ToEquatableReadOnlyList() };
	public static ValibotSchema Pipe(ValibotSchema schema1, ValibotSchema schema2, ValibotSchema schema3, ValibotSchema schema4) => new() { Name = "pipe", IsValibotMethod = true, Members = new List<ValibotSchema>([schema1, schema2, schema3, schema4]).ToEquatableReadOnlyList() };
	public static ValibotSchema Pipe(ValibotSchema schema1, ValibotSchema schema2, ValibotSchema schema3, ValibotSchema schema4, ValibotSchema schema5) => new() { Name = "pipe", IsValibotMethod = true, Members = new List<ValibotSchema>([schema1, schema2, schema3, schema4, schema5]).ToEquatableReadOnlyList() };
	public static ValibotSchema Transform(string function) => new() { Name = "transform", IsValibotMethod = true, Members = new List<ValibotSchema>([new ValibotSchema { Name = function }]).ToEquatableReadOnlyList() };

	public static ValibotSchema InstantRequest => Pipe(Instance("Temporal.Instant"), Transform("(input) => input.toString()"));
	public static ValibotSchema InstantResponse => Pipe(StringSchema, Transform("(input) => Temporal.Instant.from(input)"));
	public static ValibotSchema ZonedDateTimeRequest => Pipe(Instance("Temporal.ZonedDateTime"), Transform("(input) => input.toString()"));
	public static ValibotSchema ZonedDateTimeResponse => Pipe(StringSchema, Transform("(input) => Temporal.ZonedDateTime.from(input)"));
	public static ValibotSchema LocalDateRequest => Pipe(Instance("Temporal.PlainDate"), Transform("(input) => input.toString()"));
	public static ValibotSchema LocalDateResponse => Pipe(StringSchema, Transform("(input) => Temporal.PlainDate.from(input)"));
	public static ValibotSchema PeriodRequest => Pipe(Instance("Temporal.Duration"), Transform("(input) => input.toString()"));
	public static ValibotSchema PeriodResponse => Pipe(StringSchema, Transform("(input) => Temporal.Duration.from(input)"));
	public static ValibotSchema DurationRequest => Pipe(Instance("Temporal.Duration"), Transform("(input) => { const d = input.round({ largestUnit: 'hours' }); return `${d.hours}:${String(d.minutes).padStart(2, '0')}:${String(d.seconds).padStart(2, '0')}`; }"));
	public static ValibotSchema DurationResponse => Pipe(StringSchema, Transform("(input) => { let [hours, minutes, seconds] = input.split(':'); return Temporal.Duration.from({ hours: parseInt(hours), minutes: parseInt(minutes), seconds: parseInt(seconds) }); }"));

	public static ValibotSchema StringSchema => new() { Name = "string", IsValibotMethod = true };
	public static ValibotSchema NumberSchema => new() { Name = "number", IsValibotMethod = true };
	public static ValibotSchema BooleanSchema => new() { Name = "boolean", IsValibotMethod = true };
	public static ValibotSchema FileSchema => new() { Name = "file", IsValibotMethod = true };
	public static ValibotSchema Instance(string name) => new() { Name = "instance", IsValibotMethod = true, Members = new List<ValibotSchema>([new ValibotSchema { Name = name }]).ToEquatableReadOnlyList() };

	public static ValibotSchema Ref(string name) => new() { Name = name };

}
