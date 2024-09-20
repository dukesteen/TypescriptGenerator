using Microsoft.CodeAnalysis;

using TypescriptGenerator.Console.TsGen.Types;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types;

public record EndpointDescriptor
{
	public required string Path { get; init; }
	public required EndpointHttpMethod HttpMethod { get; init; }
	public required INamedTypeSymbol EndpointWrapperType { get; init; }
	public required INamedTypeSymbol ReturnType { get; init; }
	public required INamedTypeSymbol? RequestType { get; init; }
	public required RequestTypeBindingOptions RequestTypeBoundAs { get; init; } = RequestTypeBindingOptions.None;
}

public enum RequestTypeBindingOptions
{
	None,
	Query,
	Route,
	Form,
	Parameters,
}

public enum EndpointHttpMethod
{
	Get,
	Post,
	Put,
	Patch,
	Delete,
}
