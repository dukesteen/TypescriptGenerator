using Microsoft.CodeAnalysis;

using TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types;

internal class PropertyDescriptor
{
	public required IPropertySymbol PropertySymbol { get; init; }
	public string PropertyName => PropertySymbol.Name;
	public string ValibotPropertyName => PropertyName.ToCamelCase();
	public INamedTypeSymbol PropertyType => (INamedTypeSymbol)PropertySymbol.Type;
}
