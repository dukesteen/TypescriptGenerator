using Microsoft.CodeAnalysis;

using TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.Visitors;

internal class GeneratableTypeCollector(IReadOnlyList<string> includedNamespacePrefixes, IReadOnlyList<string> excludedNamespacePrefixes, TypeUsage typeUsage)
{
	public IList<TypeDescriptor> GeneratableTypes { get; } = [];

	internal void CollectFrom(INamedTypeSymbol from)
	{
		if (from.IsInExcludedNamespaces(excludedNamespacePrefixes))
			return;
		
		// Handle enums early
		if (from.TypeKind == TypeKind.Enum && from.IsInIncludedNamespaces(includedNamespacePrefixes))
		{
			GeneratableTypes.Add(new TypeDescriptor
			{
				TypeSymbol = from,
				Properties = [],
				TypeUsage = typeUsage,
			});
			return;
		}

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
			if (from.NullableAnnotation == NullableAnnotation.Annotated)
				CollectFrom((from.TypeArguments.First() as INamedTypeSymbol)!);
			else
				throw new InvalidOperationException("Cannot generate TypeScript for type " + from.ToDisplayString());
		}
		else
		{
			if (from.IsSystemType())
				return;

			if (from.IsInIncludedNamespaces(includedNamespacePrefixes))
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
		var properties = type.GetMembers().OfType<IPropertySymbol>().Where(x => x.Name != "EqualityContract").Where(x => !x.IsStatic).ToList();
		foreach (var property in properties)
		{
			if (property.Type is INamedTypeSymbol propertyType)
			{
				if (propertyType.IsCollection() || propertyType.IsValueTaskT() || propertyType.NullableAnnotation == NullableAnnotation.Annotated)
				{
					if (propertyType.IsReferenceType && !propertyType.IsSystemType() &&
					!propertyType.IsInExcludedNamespaces(excludedNamespacePrefixes) &&
					propertyType.IsInIncludedNamespaces(includedNamespacePrefixes))
					{
						CollectFrom(propertyType);
					}

					foreach (var typeArgument in propertyType.TypeArguments)
					{
						if (typeArgument is INamedTypeSymbol namedTypeArgument)
							CollectFrom(namedTypeArgument);
					}
				}
				else if (!propertyType.IsSystemType() &&
					!propertyType.IsInExcludedNamespaces(excludedNamespacePrefixes) &&
					propertyType.IsInIncludedNamespaces(includedNamespacePrefixes))
				{
					CollectFrom(propertyType);
				}
			}
		}

		return properties.Select(p => new PropertyDescriptor { PropertySymbol = p }).ToList();
	}
}
