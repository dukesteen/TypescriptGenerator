using Microsoft.CodeAnalysis;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types;

internal record EndpointDescriptor
{
	public required string Path { get; init; }
	public required EndpointHttpMethod HttpMethod { get; init; }
	public required INamedTypeSymbol EndpointWrapperType { get; init; }
	public required INamedTypeSymbol ReturnType { get; init; }
	public required INamedTypeSymbol? RequestType { get; init; }
	public required RequestTypeBindingOptions RequestTypeBoundAs { get; init; } = RequestTypeBindingOptions.None;
}

internal enum RequestTypeBindingOptions
{
	None,
	Query,
	Route,
	Form,
	Parameters,
}

internal enum EndpointHttpMethod
{
	Get,
	Post,
	Put,
	Patch,
	Delete,
}
