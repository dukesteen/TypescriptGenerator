using Microsoft.CodeAnalysis;

namespace TypescriptGenerator.Console.TsGen.Types;

public record ApiEndpointDescriptor(
	IMethodSymbol Action,
	EndpointHttpMethod EndpointHttpMethod,
	string RelativePath
);

public enum EndpointHttpMethod
{
	Get,
	Post,
	Put,
	Delete,
	Patch,
}
