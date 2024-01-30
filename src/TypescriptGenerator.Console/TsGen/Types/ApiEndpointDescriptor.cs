using Microsoft.CodeAnalysis;

namespace TypescriptGenerator.Console.TsGen.Types;

public record ApiEndpointDescriptor(
	IMethodSymbol Action,
	HttpMethod HttpMethod,
	string RelativePath
);

public enum HttpMethod
{
	Get,
	Post,
	Put,
	Delete,
	Patch,
}
