using TypescriptGenerator.Console.ImmediateApisTsGen.Types;
using TypescriptGenerator.Console.ImmediateApisTsGen.Visitors;

namespace TypescriptGenerator.Console.ImmediateApisTsGen;

internal partial class Generator
{
	internal List<TypeDescriptor> DiscoverGeneratableTypes()
	{
		List<TypeDescriptor> generatableTypes = [];

		foreach (var endpointDescriptor in EndpointDescriptors)
		{
			var requestTypeCollector = new GeneratableTypeCollector(config.GenerateTypesInNamespacesIncludes, TypeUsage.Request);
			var returnTypeCollector = new GeneratableTypeCollector(config.GenerateTypesInNamespacesIncludes, TypeUsage.Response);
			returnTypeCollector.CollectFrom(endpointDescriptor.ReturnType);
			if (endpointDescriptor.RequestType != null)
				requestTypeCollector.CollectFrom(endpointDescriptor.RequestType);

			var types = requestTypeCollector.GeneratableTypes.Concat(
				returnTypeCollector.GeneratableTypes);

			generatableTypes.AddRange(types);
		}

		return generatableTypes.DistinctBy(x => new { x.FullyQualifiedName, x.TypeUsage }).ToList();
	}
}
