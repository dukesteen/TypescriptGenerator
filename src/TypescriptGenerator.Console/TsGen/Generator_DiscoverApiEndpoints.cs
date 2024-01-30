using System.Text.RegularExpressions;

using Asp.Versioning;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using TypescriptGenerator.Console.TsGen.Types;

using ApiVersion = Asp.Versioning.ApiVersion;
using HttpMethod = TypescriptGenerator.Console.TsGen.Types.HttpMethod;

namespace TypescriptGenerator.Console.TsGen;

internal partial class Generator
{
	internal static List<ApiEndpointDescriptor> DiscoverApiEndpoints(Compilation compilation)
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
				throw new InvalidOperationException("Multiple HTTP attributes found");

			var httpattribute = httpAttributes.FirstOrDefault()!;

			var httpMethod = GetHttpMethodFromAttributeData(httpattribute);
			var relativePath = GetRelativePathFromEndpointAction(methodSymbol, httpattribute);
			endpoints.Add(new ApiEndpointDescriptor(methodSymbol, httpMethod, relativePath));
		}

		return endpoints;
	}

	private static HttpMethod GetHttpMethodFromAttributeData(AttributeData attribute)
	{
		return attribute.AttributeClass?.Name switch
		{
			"HttpGetAttribute" => HttpMethod.Get,
			"HttpPostAttribute" => HttpMethod.Post,
			"HttpPutAttribute" => HttpMethod.Put,
			"HttpDeleteAttribute" => HttpMethod.Delete,
			"HttpPatchAttribute" => HttpMethod.Patch,
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
			throw new InvalidOperationException("Multiple Route attributes found");

		var controllerRouteAttribute = controllerRouteAttributes.FirstOrDefault();
		var controllerRouteString = controllerRouteAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";

		var actionRoute = httpAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
		var relativePath = Path.Combine(controllerRouteString, actionRoute);

		var versionAttributes = endpointAction.GetAttributes().Where(x => x.AttributeClass?.Name == "ApiVersionAttribute").ToList();

		if (versionAttributes.Count > 1)
			throw new InvalidOperationException("Multiple ApiVersion attributes found");

		if (versionAttributes.Count == 0)
		{
			versionAttributes = endpointAction.ContainingType.GetAttributes().Where(x => x.AttributeClass?.Name == "ApiVersionAttribute").ToList();
			if (versionAttributes.Count > 1)
				throw new InvalidOperationException("Multiple ApiVersion attributes found");
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
			else
			{
				apiVersion = version as ApiVersion ?? throw new InvalidOperationException("Failed to parse ApiVersion");
			}

			// TODO: Support multiple version formats
			relativePath = apiVersionRegex.Replace(relativePath, apiVersion.ToString("VVV"));

			var routeParamRegex = new Regex(@":\S*}", RegexOptions.Multiline);
			relativePath = routeParamRegex.Replace(relativePath, "}");
		}

		return relativePath;
	}
}
