using System.Text;

using Microsoft.CodeAnalysis;

using TypescriptGenerator.Console.ImmediateApisTsGen.Builders;
using TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;
using TypescriptGenerator.Console.ImmediateApisTsGen.Templates;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types.Valibot;

namespace TypescriptGenerator.Console.ImmediateApisTsGen;

internal partial class Generator
{
	internal string GenerateTypes()
	{
		var stringBuilder = new StringBuilder();
		var generatedEnumNames = new List<string>();

		foreach (var typeDescriptor in TypeDescriptors)
		{
			if (typeDescriptor.TypeSymbol.TypeKind == TypeKind.Enum)
			{
				if (generatedEnumNames.Contains(typeDescriptor.SchemaName))
					continue;

				var fields = typeDescriptor.TypeSymbol.GetMembers().OfType<IFieldSymbol>();
				var schemaBuilder = new ValibotEnumSchemaBuilder(typeDescriptor.Name, typeDescriptor.SchemaName);
				foreach (var member in fields)
				{
					schemaBuilder.WithMember(member.Name, (int)member.ConstantValue!);
				}

				stringBuilder.AppendLine(schemaBuilder.Build());

				generatedEnumNames.Add(typeDescriptor.SchemaName);
			}
			else
			{
				var schemaBuilder = new ValibotObjectSchemaBuilder(typeDescriptor.SchemaName);
				foreach (var property in typeDescriptor.Properties)
				{
					schemaBuilder.WithProperty(property.ValibotPropertyName, GetValibotSchemaFromType(property.PropertyType, typeDescriptor.TypeUsage));
				}

				stringBuilder.AppendLine(schemaBuilder.Build());
				if (typeDescriptor.TypeUsage == TypeUsage.Request)
					stringBuilder.AppendLine($"export type {typeDescriptor.Name} = v.InferInput<typeof {typeDescriptor.SchemaName}>");
				else
					stringBuilder.AppendLine($"export type {typeDescriptor.Name} = v.InferOutput<typeof {typeDescriptor.SchemaName}>");

				var schemaParseFunctionTemplate = Utility.SchemaParseFunctionTemplate;
				var schemaParseFunction = schemaParseFunctionTemplate.Render(new
				{
					FunctionName = $"parse{typeDescriptor.Name}",
					InferredInput = $"v.InferInput<typeof {typeDescriptor.SchemaName}>",
					InferredOutput = $"v.InferOutput<typeof {typeDescriptor.SchemaName}>",
					typeDescriptor.SchemaName,
				});

				stringBuilder.AppendLine();
				stringBuilder.AppendLine(schemaParseFunction);
			}
		}

		return stringBuilder.ToString();
	}

	private ValibotSchema GetValibotSchemaFromType(INamedTypeSymbol type, TypeUsage typeUsage, bool alreadyNullable = false)
	{
		if (type.NullableAnnotation == NullableAnnotation.Annotated && !alreadyNullable)
		{
			return ValibotSchema.Optional(GetValibotSchemaFromType(type, typeUsage, true));
		}

		if (type.IsCollection())
		{
			List<ValibotSchema> members = [];
			foreach (var typeArgument in type.TypeArguments)
			{
				if (typeArgument is INamedTypeSymbol namedTypeArgument)
					members.Add(GetValibotSchemaFromType(namedTypeArgument, typeUsage));
			}

			if (type.IsListLike())
				return ValibotSchema.Array(members.First());

			if (type.IsDictionaryLike())
				return ValibotSchema.Map(members.First(), members.Last());
		}
		else if (type.IsValueTaskT())
		{
			if (type.TypeArguments.First() is INamedTypeSymbol namedTypeArgument)
				return GetValibotSchemaFromType(namedTypeArgument, typeUsage);
		}

		var typeDisplayString = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("?", "", StringComparison.InvariantCulture);

		if (typeUsage == TypeUsage.Request)
		{
			if (Constants.RequestTypeMappings.TryGetValue(typeDisplayString, out var schema))
			{
				return schema;
			}
		}

		if (typeUsage == TypeUsage.Response)
		{
			if (Constants.ResponseTypeMappings.TryGetValue(typeDisplayString, out var schema))
			{
				return schema;
			}
		}

		if (TypeDescriptors.Any(x => x.FullyQualifiedName == typeDisplayString))
			return ValibotSchema.Ref(TypeDescriptors.First(x => x.FullyQualifiedName == typeDisplayString).SchemaName);

		throw new InvalidOperationException($"Cannot generate schema for type {typeDisplayString}");
	}
}
