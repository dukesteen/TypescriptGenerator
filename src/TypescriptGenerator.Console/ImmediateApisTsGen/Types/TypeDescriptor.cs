using Microsoft.CodeAnalysis;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Types;

internal record TypeDescriptor
{
	public required INamedTypeSymbol TypeSymbol { get; init; }
	public required TypeUsage TypeUsage { get; init; }
	public string FullyQualifiedName => TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	public string Name => GetTypeName(TypeSymbol);
	public string SchemaName => Name + "Schema";
	public required IReadOnlyList<PropertyDescriptor> Properties { get; init; }

	public INamedTypeSymbol? InheritsFrom => TypeSymbol.BaseType;

	private string GetTypeName(INamedTypeSymbol type)
	{
		var name = TypeSymbol.ContainingType == null ? TypeSymbol.Name : $"{TypeSymbol.ContainingType.Name}{TypeSymbol.Name}";
		if (type.IsGenericType)
		{
			name = name + "Of" + string.Join("And", type.TypeArguments.Select(x => GetTypeName((INamedTypeSymbol)x)));
		}

		if (TypeSymbol.TypeKind != TypeKind.Enum)
		{
			if (TypeUsage == TypeUsage.Request && !name.EndsWith("Query", StringComparison.InvariantCulture) && !name.EndsWith("Command", StringComparison.InvariantCulture))
				name += "Request";
			else if (TypeUsage == TypeUsage.Response && !name.EndsWith("Response", StringComparison.InvariantCulture))
				name += "Response";
		}

		return name;
	}
}
