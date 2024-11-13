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
			var httpAttribute = endpointClass.GetAttributes()
				.FirstOrDefault(x => Constants.EndpointAttributes.Contains(x.AttributeClass?.ToDisplayString()))!;

			var httpMethod = GetHttpMethodFromAttribute(httpAttribute);
			var relativePath = httpAttribute.ConstructorArguments[0].Value?.ToString() ??
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

			if (namedReturnType.IsGenericType && !config.GenerateTypesInNamespacesIncludes.Any(x =>
				namedReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains(x, StringComparison.InvariantCulture)))
			{
				logger.LogError("Generic return type is not in included namespace to generate types, skipping endpoint {EndpointName} with relative path {RelativePath}", endpointClass.Name, relativePath);
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

		return parameterSymbol.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromFormAttribute")
			? RequestTypeBindingOptions.Form
			: parameterSymbol.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Http.AsParametersAttribute")
			? RequestTypeBindingOptions.Parameters
			: RequestTypeBindingOptions.None;
	}
}
