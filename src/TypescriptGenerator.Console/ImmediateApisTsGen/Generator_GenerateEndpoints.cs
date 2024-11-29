using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

using TypescriptGenerator.Console.ImmediateApisTsGen.Extensions;
using TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;
using TypescriptGenerator.Console.ImmediateApisTsGen.Templates;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types;

namespace TypescriptGenerator.Console.ImmediateApisTsGen;

internal partial class Generator
{
	internal string GenerateEndpoints()
	{
		var stringBuilder = new StringBuilder();

		foreach (var endpointDescriptor in EndpointDescriptors)
		{
			var parameters = GetEndpointParameters(endpointDescriptor);
			var isListLike = endpointDescriptor.ReturnType.IsListLike();
			var returnTypeDescriptor = isListLike
				? TypeDescriptors.FirstOrDefault(x => x.FullyQualifiedName == endpointDescriptor.ReturnType.TypeArguments.First().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
				: TypeDescriptors.FirstOrDefault(x => x.FullyQualifiedName == endpointDescriptor.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

			if (returnTypeDescriptor is null && !endpointDescriptor.ReturnType.IsValueTaskT() && !endpointDescriptor.ReturnType.IsValueTask())
			{
				logger.LogError("Failed to find return type descriptor for endpoint {EndpointName} with return type {ReturnType}", endpointDescriptor.EndpointWrapperType.Name, endpointDescriptor.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
				continue;
			}

			var requestTypeDescriptor = endpointDescriptor.RequestType is not null ? TypeDescriptors.FirstOrDefault(x => x.FullyQualifiedName == endpointDescriptor.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) : null;

			var fetcherFunctionName = endpointDescriptor.EndpointWrapperType.Name.ToCamelCase();
			var requestDataTypeName = requestTypeDescriptor?.Name;
			var returnTypeName = returnTypeDescriptor != null ? returnTypeDescriptor.Name + (isListLike ? "[]" : "") : null;
			var inputSchemaParseFunctionName = "parse" + requestTypeDescriptor?.Name;
			var apiCall = GetApiCall(endpointDescriptor, parameters, returnTypeName);
			var outputSchemaParseFunctionName = "parse" + returnTypeDescriptor?.Name;

			var fetcherFunctionTemplate = Utility.FetcherFunctionTemplate;
			var fetcherFunction = fetcherFunctionTemplate.Render(new
			{
				FetcherFunctionName = fetcherFunctionName,
				DataType = requestDataTypeName,
				ReturnType = returnTypeName,
				InputSchemaParseFunctionName = inputSchemaParseFunctionName,
				ApiCall = apiCall,
				OutputSchemaParseFunctionName = outputSchemaParseFunctionName,
				IsList = isListLike,
			});

			var queryFunctionTemplate = Utility.QueryFunctionTemplate;
			var queryFunction = queryFunctionTemplate.Render(new
			{
				QueryName = "use" + endpointDescriptor.EndpointWrapperType.Name,
				FetcherFunctionName = fetcherFunctionName,
				QueryKey = "queryKeys." + endpointDescriptor.EndpointWrapperType.Name,
				DataType = requestDataTypeName,
				StaleTime = 1000 * 60 * 5,
			});

			_ = stringBuilder.AppendLine(fetcherFunction);
			_ = stringBuilder.AppendLine(queryFunction);
		}

		return stringBuilder.ToString();
	}

	[SuppressMessage("Error", "CA1308:Normalize strings to uppercase", Justification = "Lowercase is required for TypeScript")]
	private string GetApiCall(EndpointDescriptor endpointDescriptor, List<EndpointParameterDescriptor> parameters, string? returnTypeName)
	{
		if (parameters.Count == 0)
			return $"{endpointDescriptor.HttpMethod.ToString().ToLowerInvariant()}{(returnTypeName != null ? $"<{returnTypeName}>" : "")}(`{endpointDescriptor.Path}`)";

		var pathParams = parameters.Where(x => x.ParameterType == ParameterType.Path).ToList();
		var queryParams = parameters.Where(x => x.ParameterType == ParameterType.Query).ToList();
		var formParams = parameters.Where(x => x.ParameterType == ParameterType.Form).ToList();
		var bodyParams = parameters.Where(x => x.ParameterType == ParameterType.Body).ToList();

		var url = endpointDescriptor.Path;
		foreach (var pathParam in pathParams)
		{
			url = url.Replace($"{{{pathParam.RouteParamName}}}", "${parsedData." + pathParam.PropertyPath + "}", StringComparison.Ordinal);
		}

		var queryBuilder = new Dictionary<string, string>();
		foreach (var queryParam in queryParams)
		{
			queryBuilder.Add(queryParam.RouteParamName ?? queryParam.Name.ToCamelCase(), "${parsedData." + queryParam.PropertyPath + "}");
		}
		var query = queryBuilder.Count > 0 ? "?" + string.Join("&", queryBuilder.Select(x => $"{x.Key}={x.Value}")) : "";

		url += query;

		if (endpointDescriptor.HttpMethod is EndpointHttpMethod.Get)
			return $"get{(returnTypeName != null ? $"<{returnTypeName}>" : "")}(`{url}`)";

		if (endpointDescriptor.HttpMethod is EndpointHttpMethod.Post or EndpointHttpMethod.Put or EndpointHttpMethod.Patch)
		{
			if (bodyParams.Count > 1)
			{
				logger.LogError("Multiple body parameters are not supported for endpoint {EndpointName}", endpointDescriptor.EndpointWrapperType.Name);
				throw new InvalidOperationException("Multiple body parameters are not supported");
			}
			return bodyParams.Count == 1
				? parameters.Count == 1
					? $"{endpointDescriptor.HttpMethod.ToString().ToLowerInvariant()}{(returnTypeName != null ? $"<{returnTypeName}>" : "")}(`{url}`, parsedData)"
					: $"{endpointDescriptor.HttpMethod.ToString().ToLowerInvariant()}{(returnTypeName != null ? $"<{returnTypeName}>" : "")}(`{url}`, parsedData.{bodyParams.First().PropertyPath})"
				: formParams.Count > 0
				? parameters.Count == 1
					? $"{endpointDescriptor.HttpMethod.ToString().ToLowerInvariant()}Form{(returnTypeName != null ? $"<{returnTypeName}>" : "")}(`{url}`, parsedData)"
					: $"{endpointDescriptor.HttpMethod.ToString().ToLowerInvariant()}Form{(returnTypeName != null ? $"<{returnTypeName}>" : "")}(`{url}`, {{ {string.Join(", ", formParams.Select(x => $"{x.Name.ToCamelCase()}: parsedData.{x.PropertyPath}"))} }})"
				: $"{endpointDescriptor.HttpMethod.ToString().ToLowerInvariant()}{(returnTypeName != null ? $"<{returnTypeName}>" : "")}(`{url}`, parsedData)";
		}

		if (endpointDescriptor.HttpMethod is EndpointHttpMethod.Delete)
			return $"delete<{returnTypeName}>(`{url}`)";

		logger.LogError("Unsupported HTTP method {HttpMethod} for endpoint {EndpointName}", endpointDescriptor.HttpMethod, endpointDescriptor.EndpointWrapperType.Name);
		throw new InvalidOperationException("Unsupported HTTP method");
	}

	internal List<EndpointParameterDescriptor> GetEndpointParameters(EndpointDescriptor endpointDescriptor)
	{
		var parameters = new List<EndpointParameterDescriptor>();

		if (endpointDescriptor.RequestType is null)
			return parameters;

		var requestTypeProperties = endpointDescriptor.RequestType.GetMembers().OfType<IPropertySymbol>().Where(x => x.Name != "EqualityContract").ToList();
		if (endpointDescriptor is { RequestTypeBoundAs: RequestTypeBindingOptions.Parameters, RequestType: not null })
		{
			foreach (var property in requestTypeProperties)
			{
				if (property.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromRouteAttribute"))
				{
					var attribute = property.GetAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromRouteAttribute");
					var routeName = attribute.NamedArguments.First(x => x.Key == "Name").Value.Value?.ToString() ?? property.Name;
					parameters.Add(new()
					{
						Name = property.Name,
						ParameterType = ParameterType.Path,
						PropertyPath = property.Name.ToCamelCase(),
						RouteParamName = routeName,
					});
				}
				else if (property.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromQueryAttribute"))
				{
					var attribute = property.GetAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromQueryAttribute");
					var routeName = attribute.NamedArguments.FirstOrDefault(x => x.Key == "Name").Value.Value?.ToString() ?? property.Name.ToCamelCase();
					parameters.Add(new()
					{
						Name = property.Name,
						ParameterType = ParameterType.Query,
						PropertyPath = property.Name.ToCamelCase(),
						RouteParamName = routeName,
					});
				}
				else if (property.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromFormAttribute") ||
						property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Microsoft.AspNetCore.Http.IFormFile" ||
						property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Microsoft.AspNetCore.Http.IFormFileCollection")
				{
					parameters.Add(new()
					{
						Name = property.Name,
						ParameterType = ParameterType.Form,
						PropertyPath = property.Name.ToCamelCase(),
					});
				}
				else if (property.HasAttributeWithFullyQualifiedName("Microsoft.AspNetCore.Mvc.FromBodyAttribute"))
				{
					parameters.Add(new()
					{
						Name = property.Name,
						ParameterType = ParameterType.Body,
						PropertyPath = property.Name.ToCamelCase(),
					});
				}
				else
				{
					if (!property.IsStatic)
					{
						logger.LogError("Unsupported parameter type {ParameterType} for property {PropertyName} in request type {RequestType}",
							property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), property.Name, endpointDescriptor.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
					}
				}
			}
		}
		else if (endpointDescriptor is { RequestTypeBoundAs: RequestTypeBindingOptions.Form })
		{
			parameters.Add(new() { Name = "form", ParameterType = ParameterType.Form, PropertyPath = null, });

		}
		else if (endpointDescriptor is { RequestTypeBoundAs: RequestTypeBindingOptions.Query })
		{
			foreach (var property in requestTypeProperties)
			{
				parameters.Add(new()
				{
					Name = property.Name,
					ParameterType = ParameterType.Query,
					PropertyPath = property.Name.ToCamelCase(),
				});
			}
		}
		else
		{
			if (requestTypeProperties.Count > 0)
				parameters.Add(new() { Name = "body", ParameterType = ParameterType.Body, PropertyPath = null, });
		}

		return parameters;
	}
}
