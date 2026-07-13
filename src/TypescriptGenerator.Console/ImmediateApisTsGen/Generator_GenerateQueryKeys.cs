using TypescriptGenerator.Console.ImmediateApisTsGen.Builders;

namespace TypescriptGenerator.Console.ImmediateApisTsGen;

internal partial class Generator
{
	internal string GenerateQueryKeys()
	{
		var objectBuilder = new TypescriptObjectBuilder("queryKeys");
		foreach (var endpointDescriptor in EndpointDescriptors
			.OrderBy(x => x.Path, StringComparer.Ordinal)
			.ThenBy(x => x.HttpMethod)
			.ThenBy(x => x.EndpointWrapperType.Name, StringComparer.Ordinal))
		{
			_ = objectBuilder.WithProperty(endpointDescriptor.EndpointWrapperType.Name, endpointDescriptor.Path);
		}

		return objectBuilder.Build();
	}
}
