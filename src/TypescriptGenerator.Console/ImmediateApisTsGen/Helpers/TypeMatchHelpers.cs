using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;

public static class TypeMatchHelpers
{
	internal static bool IsCollection(this INamedTypeSymbol type)
	{
		return type.AllInterfaces.Any(i => i.MetadataName == "IEnumerable`1") && type.Name != "String";
	}

	internal static bool IsListLike(this INamedTypeSymbol type)
	{
		return type.AllInterfaces.Any(i => i.MetadataName == "IEnumerable`1") &&
			!type.AllInterfaces.Any(i => i.MetadataName == "IDictionary`2");
	}

	internal static bool IsDictionaryLike(this INamedTypeSymbol type)
	{
		return type.AllInterfaces.Any(i => i.MetadataName == "IDictionary`2") ||
			type.AllInterfaces.Any(i => i.MetadataName == "IReadOnlyDictionary`2");
	}

	internal static bool IsValueTaskT(this ITypeSymbol? typeSymbol)
	{
		return typeSymbol is INamedTypeSymbol
		{
			MetadataName: "ValueTask`1",
			ContainingNamespace:
			{
				Name: "Tasks",
				ContainingNamespace:
				{
					Name: "Threading",
					ContainingNamespace:
					{
						Name: "System",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			},
		};
	}

	internal static bool IsValueTask(this ITypeSymbol? typeSymbol)
	{
		return typeSymbol is INamedTypeSymbol
		{
			MetadataName: "ValueTask",
			ContainingNamespace:
			{
				Name: "Tasks",
				ContainingNamespace:
				{
					Name: "Threading",
					ContainingNamespace:
					{
						Name: "System",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			},
		};
	}

	internal static bool TryUnwrapValueTaskT(this ITypeSymbol? typeSymbol, [NotNullWhen(true)] out ITypeSymbol? unwrappedType)
	{
		if (typeSymbol is INamedTypeSymbol
			{
				MetadataName: "ValueTask`1",
				ContainingNamespace:
				{
					Name: "Tasks",
					ContainingNamespace:
					{
						Name: "Threading",
						ContainingNamespace:
						{
							Name: "System",
							ContainingNamespace.IsGlobalNamespace: true,
						},
					},
				},
				TypeArguments: { Length: 1 } typeArguments,
			})
		{
			unwrappedType = typeArguments[0];
			return true;
		}

		unwrappedType = null;
		return false;
	}

	internal static bool IsSystemType(this ITypeSymbol typeSymbol)
	{
		return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
				.StartsWith("global::System.", StringComparison.InvariantCulture) ||
			typeSymbol.ContainingNamespace.ToDisplayString().StartsWith("System", StringComparison.InvariantCulture);
	}
}
