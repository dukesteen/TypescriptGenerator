using System.Text.RegularExpressions;

using Asp.Versioning;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

using TypescriptGenerator.Console.TsGen.Types;

using ApiVersion = Asp.Versioning.ApiVersion;

namespace TypescriptGenerator.Console.TsGen;

internal partial class Generator
{
	internal List<ApiEndpointDescriptor> DiscoverApiEndpoints(Compilation compilation)
	{
		var endpointActions = compilation.SyntaxTrees
			.SelectMany(x => x.GetRoot().DescendantNodes())
			.OfType<MethodDeclarationSyntax>()
			.Where(x => x.AttributeLists.Any(y =>
				y.Attributes.Any(z => z.Name.ToString() is "HttpGet" or "HttpPost" or "HttpPut" or "HttpDelete" or "HttpPatch")))
			.Where(x => x.Parent is ClassDeclarationSyntax parent && parent.AttributeLists.Any(y =>
				y.Attributes.Any(z => z.Name.ToString() == "ApiController"))).ToList();

		var endpoints = new List<ApiEndpointDescriptor>();
		foreach (var endpointAction in endpointActions)
		{
			var semanticModel = compilation.GetSemanticModel(endpointAction.SyntaxTree);
			var methodSymbol = semanticModel.GetDeclaredSymbol(endpointAction) as IMethodSymbol ??
				throw new InvalidOperationException("Failed to get method symbol");

			var httpAttributes = GetHttpAttributes(methodSymbol);
			if (httpAttributes.Count > 1)
				throw new InvalidOperationException("Multiple HTTP attributes found on action");

			var httpattribute = httpAttributes.FirstOrDefault()!;

			var httpMethod = GetHttpMethodFromAttributeData(httpattribute);
			var relativePath = GetRelativePathFromEndpointAction(methodSymbol, httpattribute);
			endpoints.Add(new ApiEndpointDescriptor(methodSymbol, httpMethod, relativePath));

			logger.LogDebug("Discovered {HttpMethod} endpoint with action name {ActionName} and relative path {RelativePath}", httpMethod.ToString(),
				methodSymbol.Name, relativePath);
		}

		return endpoints;
	}

	private static EndpointHttpMethod GetHttpMethodFromAttributeData(AttributeData attribute)
	{
		return attribute.AttributeClass?.Name switch
		{
			"HttpGetAttribute" => EndpointHttpMethod.Get,
			"HttpPostAttribute" => EndpointHttpMethod.Post,
			"HttpPutAttribute" => EndpointHttpMethod.Put,
			"HttpDeleteAttribute" => EndpointHttpMethod.Delete,
			"HttpPatchAttribute" => EndpointHttpMethod.Patch,
			_ => throw new InvalidOperationException("Failed to find HTTP method"),
		};
	}

	private static List<AttributeData> GetHttpAttributes(IMethodSymbol endpointAction)
	{
		return endpointAction.GetAttributes()
			.Where(x => x.AttributeClass?.Name is "HttpGetAttribute" or "HttpPostAttribute" or "HttpPutAttribute" or "HttpDeleteAttribute"
				or "HttpPatchAttribute")
			.ToList();
	}

	private static string GetRelativePathFromEndpointAction(IMethodSymbol endpointAction, AttributeData httpAttribute)
	{
		var controllerRouteAttributes = endpointAction.ContainingType.GetAttributes().Where(x => x.AttributeClass?.Name == "RouteAttribute").ToList();
		if (controllerRouteAttributes.Count > 1)
			throw new InvalidOperationException("Multiple Route attributes found on controller");

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

		if (versionAttributes.Count > 1)
			throw new InvalidOperationException("Multiple ApiVersion attributes found on action");

		if (versionAttributes.Count == 0)
		{
			versionAttributes = endpointAction.ContainingType.GetAttributes().Where(x => x.AttributeClass?.Name == "ApiVersionAttribute").ToList();
			if (versionAttributes.Count > 1)
				throw new InvalidOperationException("Multiple ApiVersion attributes found on controller");
		}

		var versionAttribute = versionAttributes.FirstOrDefault();

		if (versionAttribute is not null)
		{
			var apiVersionRegex = new Regex(@"{\S*:apiVersion}", RegexOptions.Multiline);
			var apiVersion = new ApiVersion(1);
			var version = versionAttribute.ConstructorArguments.FirstOrDefault().Value;
			if (version is string versionString)
			{
				if (ApiVersionParser.Default.TryParse(versionString, out var apiVersionFromString))
					apiVersion = apiVersionFromString;
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
