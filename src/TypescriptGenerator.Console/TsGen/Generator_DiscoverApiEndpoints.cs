using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using Asp.Versioning;

using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

using TypescriptGenerator.Console.TsGen.Types;
using TypescriptGenerator.Console.TsGen.Walkers;

using ApiVersion = Asp.Versioning.ApiVersion;

namespace TypescriptGenerator.Console.TsGen;

internal partial class Generator
{
	[SuppressMessage("Style", "IDE0010:Add missing cases")]
	internal List<ApiEndpointDescriptor> DiscoverApiEndpoints(Compilation compilation)
	{
		var methods = new List<IMethodSymbol>();
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			var root = syntaxTree.GetRoot();
			var semanticModel = compilation.GetSemanticModel(syntaxTree);
			var methodDeclarationCollector = new MethodDeclarationCollector(semanticModel);
			methodDeclarationCollector.Visit(root);
			methods.AddRange(methodDeclarationCollector.MethodSymbols);
		}

		var endpoints = new List<ApiEndpointDescriptor>();
		foreach (var methodSymbol in methods)
		{
			var httpAttribute = methodSymbol.GetAttributes()
				.FirstOrDefault(x => Constants.HttpAttributeNames.Contains(x.AttributeClass?.ToDisplayString()))!;

			var httpMethod = GetHttpMethodFromAttribute(httpAttribute);
			var relativePath = GetRelativePathFromEndpointAction(methodSymbol, httpAttribute);

			endpoints.Add(new ApiEndpointDescriptor(methodSymbol, httpMethod, relativePath));

			logger.LogDebug("Discovered {HttpMethod} endpoint with action name {ActionName} and relative path {RelativePath}", httpMethod.ToString(),
				methodSymbol.Name, relativePath);
		}

		return endpoints;
	}

	private static EndpointHttpMethod GetHttpMethodFromAttribute(AttributeData attributeData)
	{
		return attributeData.AttributeClass?.ToDisplayString() switch
		{
			"Microsoft.AspNetCore.Mvc.HttpGetAttribute" => EndpointHttpMethod.Get,
			"Microsoft.AspNetCore.Mvc.HttpPostAttribute" => EndpointHttpMethod.Post,
			"Microsoft.AspNetCore.Mvc.HttpPutAttribute" => EndpointHttpMethod.Put,
			"Microsoft.AspNetCore.Mvc.HttpDeleteAttribute" => EndpointHttpMethod.Delete,
			"Microsoft.AspNetCore.Mvc.HttpPatchAttribute" => EndpointHttpMethod.Patch,
			_ => throw new InvalidOperationException("Failed to find HTTP method"),
		};
	}

	private static string GetRelativePathFromEndpointAction(IMethodSymbol endpointAction, AttributeData httpAttribute)
	{
		var controllerRouteAttributes = endpointAction.ContainingType.GetAttributes().Where(x => x.AttributeClass?.Name == "RouteAttribute").ToList();
		if (controllerRouteAttributes.Count > 1) throw new InvalidOperationException("Multiple Route attributes found on controller");

		var controllerRouteAttribute = controllerRouteAttributes.FirstOrDefault();
		var controllerRouteString = controllerRouteAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";

		var actionRoute = httpAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
		var relativePath = Path.Combine(controllerRouteString, actionRoute);

		relativePath = HandleApiVersioning(endpointAction, relativePath);

		return relativePath;
	}

	private static string HandleApiVersioning(IMethodSymbol endpointAction, string relativePath)
	{
		var versionAttributes = endpointAction.GetAttributes().Where(x => x.AttributeClass?.Name == "ApiVersionAttribute").ToList();

		if (versionAttributes.Count > 1) throw new InvalidOperationException("Multiple ApiVersion attributes found on action");

		if (versionAttributes.Count == 0)
		{
			versionAttributes = endpointAction.ContainingType.GetAttributes().Where(x => x.AttributeClass?.Name == "ApiVersionAttribute").ToList();
			if (versionAttributes.Count > 1) throw new InvalidOperationException("Multiple ApiVersion attributes found on controller");
		}

		var versionAttribute = versionAttributes.FirstOrDefault();

		if (versionAttribute is not null)
		{
			var apiVersionRegex = new Regex(@"{\S*:apiVersion}", RegexOptions.Multiline);
			var apiVersion = new ApiVersion(1);
			var version = versionAttribute.ConstructorArguments.FirstOrDefault().Value;
			if (version is string versionString)
			{
				if (ApiVersionParser.Default.TryParse(versionString, out var apiVersionFromString)) apiVersion = apiVersionFromString;
			}
			else if (version is double doubleVersion)
			{
				apiVersion = new ApiVersion(doubleVersion);
			}

			// TODO: Support multiple version formats
			relativePath = apiVersionRegex.Replace(relativePath, apiVersion.ToString("VVV"));

			var routeParamRegex = new Regex(@":\S*}", RegexOptions.Multiline);
			relativePath = routeParamRegex.Replace(relativePath, "}");
		}

		return relativePath;
	}
}
