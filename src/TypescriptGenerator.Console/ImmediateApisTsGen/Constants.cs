using TypescriptGenerator.Console.ImmediateApisTsGen.Types.Valibot;

namespace TypescriptGenerator.Console.ImmediateApisTsGen;

internal static class Constants
{
	public static readonly IReadOnlyList<string> EndpointAttributes =
	[
		"Immediate.Apis.Shared.MapGetAttribute",
		"Immediate.Apis.Shared.MapPostAttribute",
		"Immediate.Apis.Shared.MapPutAttribute",
		"Immediate.Apis.Shared.MapDeleteAttribute",
		"Immediate.Apis.Shared.MapPatchAttribute",
	];

	public static readonly Dictionary<string, ValibotSchema> RequestTypeMappings = new() {
		{"string", ValibotSchema.StringSchema},
		{"int", ValibotSchema.NumberSchema},
		{"long", ValibotSchema.StringSchema},
		{"double", ValibotSchema.NumberSchema},
		{"float", ValibotSchema.NumberSchema},
		{"decimal", ValibotSchema.NumberSchema},
		{"bool", ValibotSchema.BooleanSchema},
		{"System.String", ValibotSchema.StringSchema},
		{"System.Int32", ValibotSchema.NumberSchema},
		{"System.Int64", ValibotSchema.NumberSchema},
		{"System.Double", ValibotSchema.NumberSchema},
		{"System.Single", ValibotSchema.NumberSchema},
		{"System.Decimal", ValibotSchema.NumberSchema},
		{"System.Boolean", ValibotSchema.BooleanSchema},
		{"System.Guid", ValibotSchema.StringSchema},
		{"global::Microsoft.AspNetCore.Http.IFormFile", ValibotSchema.FileSchema},
		{"global::Microsoft.AspNetCore.Http.IFormFileCollection", ValibotSchema.Array(ValibotSchema.FileSchema)},
		{"global::NodaTime.Instant", ValibotSchema.InstantRequest},
		{"global::NodaTime.LocalDate", ValibotSchema.LocalDateRequest},
		{"global::NodaTime.Period", ValibotSchema.PeriodRequest},
		{"global::NodaTime.Duration", ValibotSchema.DurationRequest},
		{"global::NodaTime.AnnualDate", ValibotSchema.StringSchema},
	};

	public static readonly Dictionary<string, ValibotSchema> ResponseTypeMappings = new() {
		{"string", ValibotSchema.StringSchema},
		{"int", ValibotSchema.NumberSchema},
		{"long", ValibotSchema.StringSchema},
		{"double", ValibotSchema.NumberSchema},
		{"float", ValibotSchema.NumberSchema},
		{"decimal", ValibotSchema.NumberSchema},
		{"bool", ValibotSchema.BooleanSchema},
		{"System.String", ValibotSchema.StringSchema},
		{"System.Int32", ValibotSchema.NumberSchema},
		{"System.Int64", ValibotSchema.NumberSchema},
		{"System.Double", ValibotSchema.NumberSchema},
		{"System.Single", ValibotSchema.NumberSchema},
		{"System.Decimal", ValibotSchema.NumberSchema},
		{"System.Boolean", ValibotSchema.BooleanSchema},
		{"System.Guid", ValibotSchema.StringSchema},
		{"global::Microsoft.AspNetCore.Http.IFormFile", ValibotSchema.FileSchema},
		{"global::Microsoft.AspNetCore.Http.IFormFileCollection", ValibotSchema.Array(ValibotSchema.FileSchema)},
		{"global::NodaTime.Instant", ValibotSchema.InstantResponse},
		{"global::NodaTime.LocalDate", ValibotSchema.LocalDateResponse},
		{"global::NodaTime.Period", ValibotSchema.PeriodResponse},
		{"global::NodaTime.Duration", ValibotSchema.DurationResponse},
		{"global::NodaTime.AnnualDate", ValibotSchema.StringSchema},
	};
}
