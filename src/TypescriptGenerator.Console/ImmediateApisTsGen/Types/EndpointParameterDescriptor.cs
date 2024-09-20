namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types;

public class EndpointParameterDescriptor
{
	public required string Name { get; set; }
	public string? RouteParamName { get; set; }
	public string? PropertyPath { get; set; }
	public ParameterType ParameterType { get; set; }
}

public enum ParameterType
{
	Body,
	Query,
	Path,
	Form,
}

