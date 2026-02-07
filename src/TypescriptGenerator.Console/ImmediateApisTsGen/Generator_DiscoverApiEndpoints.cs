using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

using TypescriptGenerator.Console.ImmediateApisTsGen.Extensions;
using TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;
using TypescriptGenerator.Console.ImmediateApisTsGen.SyntaxWalkers;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types;

namespace TypescriptGenerator.Console.ImmediateApisTsGen;

internal partial class Generator
{
	internal List<EndpointDescriptor> DiscoverApiEndpoints(Compilation compilation)
	{
		bool IsAllowedByNamespaceFilters(ITypeSymbol typeSymbol)
		{
			if (typeSymbol.IsSystemType())
				return true;

			if (typeSymbol.IsInExcludedNamespaces(config.GenerateTypesInNamespacesExcludes))
				return false;

			return typeSymbol.IsInIncludedNamespaces(config.GenerateTypesInNamespacesIncludes);
		}

		bool IsAllowedByNamespaceFiltersRecursively(INamedTypeSymbol typeSymbol)
		{
			if (!IsAllowedByNamespaceFilters(typeSymbol))
				return false;

			if (typeSymbol.IsCollection() || typeSymbol.IsValueTaskT())
			{
				foreach (var typeArgument in typeSymbol.TypeArguments.OfType<INamedTypeSymbol>())
				{
					if (!IsAllowedByNamespaceFiltersRecursively(typeArgument))
						return false;
				}
			}
			else if (typeSymbol.IsGenericType && typeSymbol.IsSystemType() && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
			{
				if (typeSymbol.TypeArguments.FirstOrDefault() is INamedTypeSymbol nullableTypeArgument)
					return IsAllowedByNamespaceFiltersRecursively(nullableTypeArgument);
			}

			return true;
		}

		var endpointClasses = new List<INamedTypeSymbol>();
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			var root = syntaxTree.GetRoot();
			var semanticModel = compilation.GetSemanticModel(syntaxTree);
			var classDeclarationCollector = new EndpointDeclarationCollector(semanticModel);
			classDeclarationCollector.Visit(root);
			endpointClasses.AddRange(classDeclarationCollector.Handlers);
		}
		endpointClasses = endpointClasses.Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>().ToList();


		var endpointDescriptors = new List<EndpointDescriptor>();
		foreach (var endpointClass in endpointClasses)
		{
			if (!IsAllowedByNamespaceFilters(endpointClass))
			{
				logger.LogDebug(
					"Endpoint {EndpointName} is excluded or not included for generation, skipping",
					endpointClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
				continue;
			}

			var httpAttribute = endpointClass.GetAttributes()
				.FirstOrDefault(x => Constants.EndpointAttributes.Contains(x.AttributeClass?.ToDisplayString()))!;

			var httpMethod = GetHttpMethodFromAttribute(httpAttribute);
			var relativePath = httpAttribute.ConstructorArguments[0].Values.First().Value?.ToString() ??
				throw new InvalidOperationException("Failed to get relative path");

			var handleMethod = endpointClass.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == "HandleAsync") ??
				throw new InvalidOperationException("Failed to find handle method");

			var returnType = handleMethod.ReturnType;

			if (returnType is not INamedTypeSymbol namedReturnType)
			{
				logger.LogError("Return type is not a named type symbol, skipping endpoint {EndpointName} with relative path {RelativePath}", endpointClass.Name, relativePath);
				continue;
			}

			if (namedReturnType.IsGenericType && namedReturnType.IsCollection())
			{
				logger.LogError("Collection return types are not supported, skipping endpoint {EndpointName} with relative path {RelativePath}", endpointClass.Name, relativePath);
				continue;
			}

			if (namedReturnType.TryUnwrapValueTaskT(out var unwrappedReturnType) && unwrappedReturnType is INamedTypeSymbol)
				namedReturnType = (INamedTypeSymbol)unwrappedReturnType;

			if (!IsAllowedByNamespaceFiltersRecursively(namedReturnType))
			{
				logger.LogError(
					"Return type {ReturnType} is excluded or not included for generation, skipping endpoint {EndpointName} with relative path {RelativePath}",
					namedReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					endpointClass.Name,
					relativePath);
				continue;
			}

			var requestTypeSymbol = handleMethod.Parameters.FirstOrDefault()?.Type!;

			if (requestTypeSymbol is not INamedTypeSymbol namedRequestType)
			{
				logger.LogError("Request type is not a named type symbol, skipping endpoint {EndpointName} with relative path {RelativePath}", endpointClass.Name, relativePath);
				continue;
			}

			if (namedRequestType.IsGenericType && namedRequestType.IsCollection())
			{
				logger.LogError("Collection request types are not supported, skipping endpoint {EndpointName} with relative path {RelativePath}",
					endpointClass.Name, relativePath);
				continue;
			}

			var requestType = namedRequestType;

			var requestTypeHasNoProperties = requestTypeSymbol.GetMembers().OfType<IPropertySymbol>().Where(x => x.Name != "EqualityContract")
				.Where(x => !x.IsStatic).ToList()
				.Count == 0;

			if (requestTypeSymbol.Name == "object" ||
			requestTypeHasNoProperties)
			{
				if (requestTypeHasNoProperties) requestType = requestTypeSymbol.BaseType ?? null;
			}

			if (requestType is not null && !IsAllowedByNamespaceFiltersRecursively(requestType))
			{
				logger.LogError(
					"Request type {RequestType} is excluded or not included for generation, skipping endpoint {EndpointName} with relative path {RelativePath}",
					requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					endpointClass.Name,
					relativePath);
				continue;
			}

			endpointDescriptors.Add(new EndpointDescriptor
			{
				Path = relativePath,
				HttpMethod = httpMethod,
				EndpointWrapperType = endpointClass,
				ReturnType = namedReturnType,
				RequestType = requestType,
				RequestTypeBoundAs = GetRequestTypeBindingOptionsFromParameterSymbol(handleMethod.Parameters.FirstOrDefault()!),
			});

			logger.LogDebug("Discovered {HttpMethod} endpoint with endpoint name {EndpointName} and relative path {RelativePath}",
				httpMethod.ToString(),
				endpointClass.Name, relativePath);
		}

		return endpointDescriptors;
	}

	private static EndpointHttpMethod GetHttpMethodFromAttribute(AttributeData attributeData)
	{
		return attributeData.AttributeClass?.ToDisplayString() switch
		{
			"Immediate.Apis.Shared.MapGetAttribute" => EndpointHttpMethod.Get,
			"Immediate.Apis.Shared.MapPostAttribute" => EndpointHttpMethod.Post,
			"Immediate.Apis.Shared.MapPutAttribute" => EndpointHttpMethod.Put,
			"Immediate.Apis.Shared.MapDeleteAttribute" => EndpointHttpMethod.Delete,
			"Immediate.Apis.Shared.MapPatchAttribute" => EndpointHttpMethod.Patch,
			_ => throw new InvalidOperationException("Failed to find HTTP method"),
		};
	}

	private static RequestTypeBindingOptions GetRequestTypeBindingOptionsFromParameterSymbol(IParameterSymbol parameterSymbol)
	{
		if (parameterSymbol.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromRouteAttribute"))
			return RequestTypeBindingOptions.Route;

		if (parameterSymbol.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromQueryAttribute"))
			return RequestTypeBindingOptions.Query;

		if (parameterSymbol.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromFormAttribute"))
			return RequestTypeBindingOptions.Form;

		if (parameterSymbol.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Http.AsParametersAttribute"))
			return RequestTypeBindingOptions.Parameters;

		// Infer [AsParameters] if the request type's properties have binding attributes
		if (parameterSymbol.Type is INamedTypeSymbol namedType)
		{
			var bindingAttributes = new[]
			{
				"Microsoft.AspNetCore.Mvc.FromRouteAttribute",
				"Microsoft.AspNetCore.Mvc.FromQueryAttribute",
				"Microsoft.AspNetCore.Mvc.FromFormAttribute",
				"Microsoft.AspNetCore.Mvc.FromBodyAttribute",
			};

			var hasPropertyBindingAttributes = namedType.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.Name != "EqualityContract" && !p.IsStatic)
				.Any(p => bindingAttributes.Any(attr => p.HasAttributeWithFullyQualifiedName(attr)));

			if (hasPropertyBindingAttributes)
				return RequestTypeBindingOptions.Parameters;
		}

		return RequestTypeBindingOptions.None;
	}
}
