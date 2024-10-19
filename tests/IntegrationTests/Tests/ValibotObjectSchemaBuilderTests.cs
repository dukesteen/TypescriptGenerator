using TypescriptGenerator.Console.ImmediateApisTsGen.Builders;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types.Valibot;

namespace IntegrationTests.Tests;

public class ValibotObjectSchemaBuilderTests
{
	[Fact]
	public void ShouldBuildCorrectSchema()
	{
		var schema = new ValibotObjectSchemaBuilder("PersonSchema")
			.WithProperty("FirstName", ValibotSchema.StringSchema)
			.WithProperty("LastName", ValibotSchema.StringSchema)
			.WithProperty("Age", ValibotSchema.NumberSchema)
			.WithProperty("Address", ValibotSchema.Ref("Address"))
			.Build();

		Assert.Equal("""
		             const PersonSchema = v.object({
		                 FirstName: v.string(),
		                 LastName: v.string(),
		                 Age: v.number(),
		                 Address: Address,
		             });
		             """, schema);
	}

	[Fact]
	public void ShouldBuildCorrectSchema_WithArray()
	{
		var schema = new ValibotObjectSchemaBuilder("PersonSchema")
			.WithProperty("FirstName", ValibotSchema.StringSchema)
			.WithProperty("LastName", ValibotSchema.StringSchema)
			.WithProperty("Age", ValibotSchema.NumberSchema)
			.WithProperty("Addresses", ValibotSchema.Array(ValibotSchema.Ref("Address")))
			.Build();

		Assert.Equal("""
		             const PersonSchema = v.object({
		                 FirstName: v.string(),
		                 LastName: v.string(),
		                 Age: v.number(),
		                 Addresses: v.array(Address),
		             });
		             """, schema);
	}

	[Fact]
	public void ShouldBuildCorrectSchema_WithMap()
	{
		var schema = new ValibotObjectSchemaBuilder("PersonSchema")
			.WithProperty("FirstName", ValibotSchema.StringSchema)
			.WithProperty("LastName", ValibotSchema.StringSchema)
			.WithProperty("Age", ValibotSchema.NumberSchema)
			.WithProperty("Addresses", ValibotSchema.Map(ValibotSchema.StringSchema, ValibotSchema.Ref("Address")))
			.Build();

		Assert.Equal("""
		             const PersonSchema = v.object({
		                 FirstName: v.string(),
		                 LastName: v.string(),
		                 Age: v.number(),
		                 Addresses: v.map(v.string(), Address),
		             });
		             """, schema);
	}

	[Fact]
	public void ShouldBuildCorrectSchema_WithOptional()
	{
		var schema = new ValibotObjectSchemaBuilder("PersonSchema")
			.WithProperty("FirstName", ValibotSchema.StringSchema)
			.WithProperty("LastName", ValibotSchema.StringSchema)
			.WithProperty("Age", ValibotSchema.Optional(ValibotSchema.NumberSchema))
			.Build();

		Assert.Equal("""
		             const PersonSchema = v.object({
		                 FirstName: v.string(),
		                 LastName: v.string(),
		                 Age: v.optional(v.number()),
		             });
		             """, schema);
	}

}
