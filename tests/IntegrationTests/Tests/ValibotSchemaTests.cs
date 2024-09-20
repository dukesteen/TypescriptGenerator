using TypescriptGenerator.Console.ImmediateApisTsGen.Types.Valibot;

namespace IntegrationTests.Tests;

public class ValibotSchemaTests
{
	[Fact]
	public void ValibotSchemaToString()
	{
		var schema = new ValibotSchema
		{
			Name = "array",
			IsValibotMethod = true,
			Members =
			[
				new ValibotSchema { Name = "OtherSchema" },
			],
		};

		Assert.Equal("v.array(OtherSchema)", schema.ToString());
	}

	[Fact]
	public void InstantRequestSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.InstantRequest;
		Assert.Equal("v.pipe(v.instance(Temporal.Instant), v.transform((input) => input.toString()))", schema.ToString());
	}

	[Fact]
	public void InstantResponseSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.InstantResponse;
		Assert.Equal("v.pipe(v.string(), v.transform((input) => Temporal.Instant.from(input)))", schema.ToString());
	}

	[Fact]
	public void LocalDateRequestSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.LocalDateRequest;
		Assert.Equal("v.pipe(v.instance(Temporal.LocalDate), v.transform((input) => input.toString()))", schema.ToString());
	}

	[Fact]
	public void LocalDateResponseSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.LocalDateResponse;
		Assert.Equal("v.pipe(v.string(), v.transform((input) => Temporal.LocalDate.from(input)))", schema.ToString());
	}

	[Fact]
	public void DurationRequestSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.DurationRequest;
		Assert.Equal("v.pipe(v.instance(Temporal.Duration), v.transform((input) => input.toString()))", schema.ToString());
	}

	[Fact]
	public void DurationResponseSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.DurationResponse;
		Assert.Equal("v.pipe(v.string(), v.transform((input) => Temporal.Duration.from(input)))", schema.ToString());
	}

	[Fact]
	public void StringSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.StringSchema;
		Assert.Equal("v.string()", schema.ToString());
	}

	[Fact]
	public void NumberSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.NumberSchema;
		Assert.Equal("v.number()", schema.ToString());
	}

	[Fact]
	public void BooleanSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.BooleanSchema;
		Assert.Equal("v.boolean()", schema.ToString());
	}

	[Fact]
	public void InstanceSchema_ShouldBeCorrect()
	{
		var schema = ValibotSchema.Instance("SomeInstance");
		Assert.Equal("v.instance(SomeInstance)", schema.ToString());
	}
}
