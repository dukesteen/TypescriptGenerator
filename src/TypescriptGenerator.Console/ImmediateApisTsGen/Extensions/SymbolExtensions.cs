using Microsoft.CodeAnalysis;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Extensions;

internal static class SymbolExtensions
{
	public static bool HasAttributeWithFullyQualifiedName(this ISymbol symbol, string attributeName)
	{
		return symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeName);
	}

	public static AttributeData GetAttributeWithFullyQualifiedName(this ISymbol symbol, string attributeName)
	{
		return symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeName) ?? throw new InvalidOperationException($"Attribute {attributeName} not found on {symbol.ToDisplayString()}");
	}
}
