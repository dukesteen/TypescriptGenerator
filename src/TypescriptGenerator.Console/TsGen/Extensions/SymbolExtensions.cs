using Microsoft.CodeAnalysis;

namespace TypescriptGenerator.Console.TsGen.Extensions;

internal static class SymbolExtensions
{
	public static bool HasAttributeWithFullyQualifiedName(this ISymbol symbol, string attributeName)
	{
		return symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeName);
	}
}
