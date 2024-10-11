namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types.Valibot;

public record ValibotSchema
{
	public required string Name { get; init; }
	public bool IsValibotMethod { get; init; }
	public List<ValibotSchema> Members { get; init; } = [];

	public override string ToString()
	{
		if (IsValibotMethod)
			return $"v.{Name}({string.Join(", ", Members.Select(x => x.ToString()).ToList())})";
		return Name;
	}

	public static ValibotSchema Array(ValibotSchema schema) => new() { Name = "array", IsValibotMethod = true, Members = [schema] };
	public static ValibotSchema Map(ValibotSchema keyType, ValibotSchema valueType) => new() { Name = "map", IsValibotMethod = true, Members = [keyType, valueType] };
	public static ValibotSchema Optional(ValibotSchema schema) => new() { Name = "nullable", IsValibotMethod = true, Members = [schema] };
	public static ValibotSchema Pipe(ValibotSchema schema1, ValibotSchema schema2) => new() { Name = "pipe", IsValibotMethod = true, Members = [schema1, schema2] };
	public static ValibotSchema Pipe(ValibotSchema schema1, ValibotSchema schema2, ValibotSchema schema3) => new() { Name = "pipe", IsValibotMethod = true, Members = [schema1, schema2, schema3] };
	public static ValibotSchema Pipe(ValibotSchema schema1, ValibotSchema schema2, ValibotSchema schema3, ValibotSchema schema4) => new() { Name = "pipe", IsValibotMethod = true, Members = [schema1, schema2, schema3, schema4] };
	public static ValibotSchema Pipe(ValibotSchema schema1, ValibotSchema schema2, ValibotSchema schema3, ValibotSchema schema4, ValibotSchema schema5) => new() { Name = "pipe", IsValibotMethod = true, Members = [schema1, schema2, schema3, schema4, schema5] };
	public static ValibotSchema Transform(string function) => new() { Name = "transform", IsValibotMethod = true, Members = [new ValibotSchema { Name = function }] };

	public static ValibotSchema InstantRequest => Pipe(Instance("Temporal.Instant"), Transform("(input) => input.toString()"));
	public static ValibotSchema InstantResponse => Pipe(StringSchema, Transform("(input) => Temporal.Instant.from(input)"));
	public static ValibotSchema LocalDateRequest => Pipe(Instance("Temporal.PlainDate"), Transform("(input) => input.toString()"));
	public static ValibotSchema LocalDateResponse => Pipe(StringSchema, Transform("(input) => Temporal.PlainDate.from(input)"));
	public static ValibotSchema DurationRequest => Pipe(Instance("Temporal.Duration"), Transform("(input) => input.toString()"));
	public static ValibotSchema DurationResponse => Pipe(StringSchema, Transform("(input) => Temporal.Duration.from(input)"));

	public static ValibotSchema StringSchema => new() { Name = "string", IsValibotMethod = true };
	public static ValibotSchema NumberSchema => new() { Name = "number", IsValibotMethod = true };
	public static ValibotSchema BooleanSchema => new() { Name = "boolean", IsValibotMethod = true };
	public static ValibotSchema FileSchema => new() { Name = "file", IsValibotMethod = true };
	public static ValibotSchema Instance(string name) => new() { Name = "instance", IsValibotMethod = true, Members = [new ValibotSchema { Name = name }] };

	public static ValibotSchema Ref(string name) => new() { Name = name };

}
