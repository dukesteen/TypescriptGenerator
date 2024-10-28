using Microsoft.CodeAnalysis;

using TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Visitors;

internal class GeneratableTypeCollector(List<string> includedNamespaces, TypeUsage typeUsage)
{
	public IList<TypeDescriptor> GeneratableTypes { get; } = [];

	internal void CollectFrom(INamedTypeSymbol from)
	{
		if (from.BaseType is not null)
			CollectFrom(from.BaseType);

		if (from.IsCollection() || from.IsValueTaskT())
		{
			foreach (var typeArgument in from.TypeArguments)
			{
				if (typeArgument is INamedTypeSymbol namedTypeArgument)
					CollectFrom(namedTypeArgument);
			}
		}
		else if (from.IsGenericType && from.IsSystemType())
		{
			throw new InvalidOperationException("Cannot generate TypeScript for type " + from.ToDisplayString());
		}
		else
		{
			if (from.IsSystemType())
				return;

			if (includedNamespaces.Any(x =>
				from.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains(x, StringComparison.InvariantCulture)) ||
			includedNamespaces.Count == 0)
			{
				GeneratableTypes.Add(new TypeDescriptor
				{
					TypeSymbol = from,
					Properties = GetPropertiesFromNamedTypeSymbol(from),
					TypeUsage = typeUsage,
				});
			}
		}
	}

	private List<PropertyDescriptor> GetPropertiesFromNamedTypeSymbol(INamedTypeSymbol type)
	{
		var properties = type.GetMembers().OfType<IPropertySymbol>().Where(x => x.Name != "EqualityContract").Where(x => x.IsStatic == false).ToList();
		foreach (var property in properties)
		{
			if (property.Type is INamedTypeSymbol propertyType)
			{
				if (propertyType.IsCollection() || propertyType.IsValueTaskT())
				{
					foreach (var typeArgument in propertyType.TypeArguments)
					{
						if (typeArgument is INamedTypeSymbol namedTypeArgument)
							CollectFrom(namedTypeArgument);
					}
				}
				else if (includedNamespaces.Any(x =>
					propertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains(x, StringComparison.InvariantCulture)) ||
				(includedNamespaces.Count == 0 && !propertyType.IsSystemType()))
				{
					CollectFrom(propertyType);
				}
			}
		}

		return properties.Select(p => new PropertyDescriptor { PropertySymbol = p }).ToList();
	}
}
