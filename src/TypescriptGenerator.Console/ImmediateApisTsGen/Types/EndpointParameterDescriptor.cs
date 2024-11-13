namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types;

internal class EndpointParameterDescriptor
{
	public required string Name { get; set; }
	public string? RouteParamName { get; set; }
	public string? PropertyPath { get; set; }
	public ParameterType ParameterType { get; set; }
}

internal enum ParameterType
{
	Body,
	Query,
	Path,
	Form,
}

