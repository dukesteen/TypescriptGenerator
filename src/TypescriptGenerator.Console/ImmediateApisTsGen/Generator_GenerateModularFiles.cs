using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

using TypescriptGenerator.Console.ImmediateApisTsGen.Builders;
using TypescriptGenerator.Console.ImmediateApisTsGen.Extensions;
using TypescriptGenerator.Console.ImmediateApisTsGen.Helpers;
using TypescriptGenerator.Console.ImmediateApisTsGen.Templates;
using TypescriptGenerator.Console.ImmediateApisTsGen.Types;

namespace TypescriptGenerator.Console.ImmediateApisTsGen;

internal partial class Generator
{
	private sealed record EndpointTypePlacementPlan(
		HashSet<string> InlineTypeKeys,
		Dictionary<string, HashSet<string>> InlineTypeKeysByEndpoint);

	private sealed record ObjectTypeRenderResult(
		string Body,
		List<TypeDescriptor> Dependencies);

	private const string TypescriptFileHeader = """
		/* eslint-disable */
		/* tslint:disable */
		// @ts-nocheck
		""";

	internal List<GeneratedFile> GenerateModularFiles()
	{
		var generatedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var typePlacementPlan = BuildEndpointTypePlacementPlan();

		AddGeneratedFile(generatedFiles, "common/runtime.ts", GenerateRuntimeModule());
		AddGeneratedFile(generatedFiles, "common/queryKeys.ts", GenerateQueryKeysModule());

		var enumDescriptors = GetEnumDescriptors();
		var requestTypeDescriptors = GetTypeDescriptorsByUsage(TypeUsage.Request);
		var responseTypeDescriptors = GetTypeDescriptorsByUsage(TypeUsage.Response);
		var sharedRequestTypeDescriptors = requestTypeDescriptors
			.Where(x => !typePlacementPlan.InlineTypeKeys.Contains(GetTypeDependencyKey(x)))
			.ToList();
		var sharedResponseTypeDescriptors = responseTypeDescriptors
			.Where(x => !typePlacementPlan.InlineTypeKeys.Contains(GetTypeDependencyKey(x)))
			.ToList();

		foreach (var enumDescriptor in enumDescriptors)
		{
			var enumModulePath = GetTypeModulePath(enumDescriptor);
			AddGeneratedFile(generatedFiles, enumModulePath, GenerateEnumTypeModule(enumDescriptor, enumModulePath));
		}

		foreach (var requestTypeDescriptor in sharedRequestTypeDescriptors)
		{
			var requestTypeModulePath = GetTypeModulePath(requestTypeDescriptor);
			AddGeneratedFile(generatedFiles, requestTypeModulePath, GenerateObjectTypeModule(requestTypeDescriptor, requestTypeModulePath));
		}

		foreach (var responseTypeDescriptor in sharedResponseTypeDescriptors)
		{
			var responseTypeModulePath = GetTypeModulePath(responseTypeDescriptor);
			AddGeneratedFile(generatedFiles, responseTypeModulePath, GenerateObjectTypeModule(responseTypeDescriptor, responseTypeModulePath));
		}

		var endpointModulePaths = GenerateEndpointModules(generatedFiles, typePlacementPlan);

		AddGeneratedFile(
			generatedFiles,
			"enums/index.ts",
			BuildBarrelFile("enums/index.ts", enumDescriptors.Select(GetTypeModulePath)));
		AddGeneratedFile(
			generatedFiles,
			"types/requests/index.ts",
			BuildBarrelFile("types/requests/index.ts", sharedRequestTypeDescriptors.Select(GetTypeModulePath)));
		AddGeneratedFile(
			generatedFiles,
			"types/responses/index.ts",
			BuildBarrelFile("types/responses/index.ts", sharedResponseTypeDescriptors.Select(GetTypeModulePath)));
		AddGeneratedFile(
			generatedFiles,
			"endpoints/index.ts",
			BuildBarrelFile("endpoints/index.ts", endpointModulePaths));

		AddGeneratedFile(generatedFiles, "index.ts", BuildRootIndexFile());

		return generatedFiles
			.OrderBy(x => x.Key, StringComparer.Ordinal)
			.Select(x => new GeneratedFile(x.Key.Replace('\\', '/'), x.Value))
			.ToList();
	}

	internal GeneratorOutputLayout ResolveOutputLayout()
	{
		var fullOutputPath = Path.GetFullPath(config.OutputPath);
		var outputPathLooksLikeFile = fullOutputPath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase);

		if (!outputPathLooksLikeFile)
		{
			var modularRootPath = fullOutputPath;
			var compatibilityShimPath = Path.Combine(modularRootPath, "client.ts");
			return new GeneratorOutputLayout(modularRootPath, compatibilityShimPath);
		}

		var outputDirectory = Path.GetDirectoryName(fullOutputPath)
			?? throw new InvalidOperationException("Output path does not have a directory.");

		var outputDirectoryName = new DirectoryInfo(outputDirectory).Name;
		var modularRoot = outputDirectoryName.Equals("generated", StringComparison.OrdinalIgnoreCase)
			? outputDirectory
			: Path.Combine(outputDirectory, "generated");

		return new GeneratorOutputLayout(modularRoot, fullOutputPath);
	}

	internal async ValueTask WriteGeneratedFiles(string modularRootPath, IEnumerable<GeneratedFile> generatedFiles)
	{
		var fullModularRootPath = Path.GetFullPath(modularRootPath);
		var pathRoot = Path.GetPathRoot(fullModularRootPath)
			?? throw new InvalidOperationException($"Failed to resolve root for output path '{modularRootPath}'.");
		var normalizedPathRoot = pathRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var normalizedModularRootPath = fullModularRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		if (string.Equals(normalizedModularRootPath, normalizedPathRoot, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException($"Refusing to clear filesystem root '{fullModularRootPath}'.");

		if (Directory.Exists(fullModularRootPath))
			Directory.Delete(fullModularRootPath, recursive: true);

		Directory.CreateDirectory(fullModularRootPath);

		foreach (var generatedFile in generatedFiles.OrderBy(x => x.RelativePath, StringComparer.Ordinal))
		{
			var filePath = Path.Combine(fullModularRootPath, generatedFile.RelativePath.Replace('/', Path.DirectorySeparatorChar));
			var directory = Path.GetDirectoryName(filePath)
				?? throw new InvalidOperationException($"Failed to resolve output directory for generated file: {generatedFile.RelativePath}");

			Directory.CreateDirectory(directory);
			await File.WriteAllTextAsync(filePath, generatedFile.Content);
		}
	}

	internal async ValueTask WriteCompatibilityShim(GeneratorOutputLayout outputLayout)
	{
		var indexFilePath = Path.Combine(outputLayout.ModularRootPath, "index.ts");
		var importPath = GetRelativeImportPathFromAbsolute(outputLayout.CompatibilityShimPath, indexFilePath);

		var shimBuilder = new StringBuilder();
		_ = shimBuilder.AppendLine(TypescriptFileHeader);
		_ = shimBuilder.AppendLine();
		_ = shimBuilder.AppendLine($"export * from '{importPath}';");

		var directory = Path.GetDirectoryName(outputLayout.CompatibilityShimPath)
			?? throw new InvalidOperationException("Compatibility shim path does not have a directory.");

		Directory.CreateDirectory(directory);
		await File.WriteAllTextAsync(outputLayout.CompatibilityShimPath, shimBuilder.ToString());
	}

	private static void AddGeneratedFile(IDictionary<string, string> generatedFiles, string relativePath, string content)
	{
		var normalizedPath = relativePath.Replace('\\', '/');
		if (!generatedFiles.TryAdd(normalizedPath, content))
			throw new InvalidOperationException($"Duplicate generated file path: {normalizedPath}");
	}

	private EndpointTypePlacementPlan BuildEndpointTypePlacementPlan()
	{
		var objectTypeDescriptorsByKey = TypeDescriptors
			.Where(x => x.TypeSymbol.TypeKind != TypeKind.Enum)
			.ToDictionary(GetTypeDependencyKey, StringComparer.Ordinal);

		var objectTypeDependencyGraph = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		foreach (var objectTypeDescriptor in objectTypeDescriptorsByKey.Values)
		{
			var objectTypeKey = GetTypeDependencyKey(objectTypeDescriptor);
			var objectTypeDependencies = GetObjectTypeDependencies(objectTypeDescriptor)
				.Where(x => x.TypeSymbol.TypeKind != TypeKind.Enum)
				.Select(GetTypeDependencyKey)
				.Distinct(StringComparer.Ordinal)
				.ToList();

			objectTypeDependencyGraph[objectTypeKey] = objectTypeDependencies;
		}

		var endpointKeysByType = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		var reachableTypeKeysByEndpoint = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

		foreach (var endpointDescriptor in EndpointDescriptors)
		{
			var endpointKey = GetEndpointKey(endpointDescriptor);
			var rootTypeKeys = new HashSet<string>(StringComparer.Ordinal);

			var requestTypeDescriptor = ResolveRequestTypeDescriptor(endpointDescriptor);
			if (requestTypeDescriptor is not null && requestTypeDescriptor.TypeSymbol.TypeKind != TypeKind.Enum)
				rootTypeKeys.Add(GetTypeDependencyKey(requestTypeDescriptor));

			var returnTypeDescriptor = ResolveReturnTypeDescriptor(endpointDescriptor, endpointDescriptor.ReturnType.IsListLike());
			if (returnTypeDescriptor is not null && returnTypeDescriptor.TypeSymbol.TypeKind != TypeKind.Enum)
				rootTypeKeys.Add(GetTypeDependencyKey(returnTypeDescriptor));

			var reachableTypeKeys = TraverseObjectTypeDependencies(rootTypeKeys, objectTypeDependencyGraph);
			reachableTypeKeysByEndpoint[endpointKey] = reachableTypeKeys;

			foreach (var typeKey in reachableTypeKeys)
			{
				if (!endpointKeysByType.TryGetValue(typeKey, out var endpointKeys))
				{
					endpointKeys = new HashSet<string>(StringComparer.Ordinal);
					endpointKeysByType[typeKey] = endpointKeys;
				}

				_ = endpointKeys.Add(endpointKey);
			}
		}

		var inlineTypeKeys = endpointKeysByType
			.Where(x => x.Value.Count == 1)
			.Select(x => x.Key)
			.ToHashSet(StringComparer.Ordinal);

		var inlineTypeKeysByEndpoint = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		foreach (var endpoint in reachableTypeKeysByEndpoint)
		{
			var inlineKeysForEndpoint = endpoint.Value
				.Where(inlineTypeKeys.Contains)
				.ToHashSet(StringComparer.Ordinal);

			if (inlineKeysForEndpoint.Count > 0)
				inlineTypeKeysByEndpoint[endpoint.Key] = inlineKeysForEndpoint;
		}

		return new EndpointTypePlacementPlan(inlineTypeKeys, inlineTypeKeysByEndpoint);
	}

	private static HashSet<string> TraverseObjectTypeDependencies(
		IEnumerable<string> rootTypeKeys,
		IReadOnlyDictionary<string, List<string>> objectTypeDependencyGraph)
	{
		var visited = new HashSet<string>(StringComparer.Ordinal);
		var queue = new Queue<string>(rootTypeKeys.Distinct(StringComparer.Ordinal));

		while (queue.Count > 0)
		{
			var typeKey = queue.Dequeue();
			if (!visited.Add(typeKey))
				continue;

			if (!objectTypeDependencyGraph.TryGetValue(typeKey, out var dependencies))
				continue;

			foreach (var dependency in dependencies)
				queue.Enqueue(dependency);
		}

		return visited;
	}

	private List<string> GenerateEndpointModules(
		IDictionary<string, string> generatedFiles,
		EndpointTypePlacementPlan typePlacementPlan)
	{
		var endpointModulePaths = new List<string>();
		var usedModulePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		var orderedEndpoints = EndpointDescriptors
			.OrderBy(x => x.Path, StringComparer.Ordinal)
			.ThenBy(x => x.HttpMethod)
			.ThenBy(x => x.EndpointWrapperType.Name, StringComparer.Ordinal)
			.ToList();

		foreach (var endpointDescriptor in orderedEndpoints)
		{
			var endpointModulePath = BuildEndpointModulePath(endpointDescriptor);
			endpointModulePath = EnsureUniqueEndpointModulePath(endpointModulePath, endpointDescriptor.HttpMethod, usedModulePaths);

			var endpointContent = GenerateEndpointModule(endpointDescriptor, endpointModulePath, typePlacementPlan);
			if (endpointContent is null)
				continue;

			AddGeneratedFile(generatedFiles, endpointModulePath, endpointContent);
			endpointModulePaths.Add(endpointModulePath);
		}

		return endpointModulePaths;
	}

	private string EnsureUniqueEndpointModulePath(string modulePath, EndpointHttpMethod httpMethod, ISet<string> usedModulePaths)
	{
		if (usedModulePaths.Add(modulePath))
			return modulePath;

		var modulePathWithMethod = AppendSuffixToTsFile(modulePath, $"_{httpMethod.ToString().ToLowerInvariant()}");
		if (usedModulePaths.Add(modulePathWithMethod))
			return modulePathWithMethod;

		var suffixCounter = 2;
		while (true)
		{
			var candidatePath = AppendSuffixToTsFile(modulePathWithMethod, $"_{suffixCounter}");
			if (usedModulePaths.Add(candidatePath))
				return candidatePath;

			suffixCounter++;
		}
	}

	private string? GenerateEndpointModule(
		EndpointDescriptor endpointDescriptor,
		string endpointModulePath,
		EndpointTypePlacementPlan typePlacementPlan)
	{
		var parameters = GetEndpointParameters(endpointDescriptor);
		var isListLike = endpointDescriptor.ReturnType.IsListLike();
		var endpointKey = GetEndpointKey(endpointDescriptor);
		var inlineTypeKeys = typePlacementPlan.InlineTypeKeysByEndpoint.TryGetValue(endpointKey, out var endpointInlineTypeKeys)
			? endpointInlineTypeKeys
			: new HashSet<string>(StringComparer.Ordinal);

		var returnTypeDescriptor = ResolveReturnTypeDescriptor(endpointDescriptor, isListLike);
		if (returnTypeDescriptor is null && !endpointDescriptor.ReturnType.IsValueTaskT() && !endpointDescriptor.ReturnType.IsValueTask())
		{
			logger.LogError(
				"Failed to find return type descriptor for endpoint {EndpointName} with return type {ReturnType}",
				endpointDescriptor.EndpointWrapperType.Name,
				endpointDescriptor.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
			return null;
		}

		var requestTypeDescriptor = ResolveRequestTypeDescriptor(endpointDescriptor);
		if (requestTypeDescriptor is null && endpointDescriptor.RequestType is not null && !endpointDescriptor.RequestType.IsSystemType())
		{
			logger.LogError(
				"Failed to find request type descriptor for endpoint {EndpointName} with request type {RequestType}",
				endpointDescriptor.EndpointWrapperType.Name,
				endpointDescriptor.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
			return null;
		}

		var fetcherFunctionName = endpointDescriptor.EndpointWrapperType.Name.ToCamelCase();
		var requestDataTypeName = requestTypeDescriptor?.Name;
		var returnTypeName = returnTypeDescriptor is null
			? null
			: returnTypeDescriptor.Name + (isListLike ? "[]" : string.Empty);
		var inputSchemaParseFunctionName = requestTypeDescriptor is null ? null : $"parse{requestTypeDescriptor.Name}";
		var outputSchemaParseFunctionName = returnTypeDescriptor is null ? null : $"parse{returnTypeDescriptor.Name}";
		var apiCall = GetApiCall(endpointDescriptor, parameters, returnTypeName);

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

		var importLines = new HashSet<string>(StringComparer.Ordinal)
		{
			$"import {{ apiClient, useQuery, unref, v, type ExtraQueryOptions, type MaybeRef }} from '{GetRelativeImportPath(endpointModulePath, "common/runtime.ts")}';",
			$"import {{ queryKeys }} from '{GetRelativeImportPath(endpointModulePath, "common/queryKeys.ts")}';",
		};
		var inlinedTypeKeys = new HashSet<string>(StringComparer.Ordinal);
		var inlineTypeBlocks = new List<string>();

		if (requestTypeDescriptor is not null)
		{
			if (ShouldInlineType(requestTypeDescriptor, inlineTypeKeys))
			{
				AddInlineTypeContent(
					endpointModulePath,
					requestTypeDescriptor,
					inlineTypeKeys,
					inlinedTypeKeys,
					importLines,
					inlineTypeBlocks);
			}
			else
			{
				importLines.Add(BuildTypeImportLine(endpointModulePath, requestTypeDescriptor, TypeUsage.Request));
			}
		}

		if (returnTypeDescriptor is not null)
		{
			if (ShouldInlineType(returnTypeDescriptor, inlineTypeKeys))
			{
				AddInlineTypeContent(
					endpointModulePath,
					returnTypeDescriptor,
					inlineTypeKeys,
					inlinedTypeKeys,
					importLines,
					inlineTypeBlocks);
			}
			else
			{
				importLines.Add(BuildTypeImportLine(endpointModulePath, returnTypeDescriptor, TypeUsage.Response));
			}
		}

		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine(TypescriptFileHeader);
		_ = stringBuilder.AppendLine();
		foreach (var importLine in importLines.OrderBy(x => x, StringComparer.Ordinal))
			_ = stringBuilder.AppendLine(importLine);

		_ = stringBuilder.AppendLine();
		foreach (var inlineTypeBlock in inlineTypeBlocks)
		{
			_ = stringBuilder.AppendLine(inlineTypeBlock);
			_ = stringBuilder.AppendLine();
		}

		_ = stringBuilder.AppendLine(fetcherFunction);
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine(queryFunction);

		return stringBuilder.ToString();
	}

	private static bool ShouldInlineType(TypeDescriptor typeDescriptor, ISet<string> inlineTypeKeys)
	{
		if (typeDescriptor.TypeSymbol.TypeKind == TypeKind.Enum)
			return false;

		return inlineTypeKeys.Contains(GetTypeDependencyKey(typeDescriptor));
	}

	private void AddInlineTypeContent(
		string endpointModulePath,
		TypeDescriptor inlineTypeDescriptor,
		ISet<string> inlineTypeKeys,
		ISet<string> inlinedTypeKeys,
		ISet<string> importLines,
		ICollection<string> inlineTypeBlocks)
	{
		var inlineTypeKey = GetTypeDependencyKey(inlineTypeDescriptor);
		if (!inlinedTypeKeys.Add(inlineTypeKey))
			return;

		var objectTypeRenderResult = RenderObjectType(inlineTypeDescriptor);

		foreach (var dependency in objectTypeRenderResult.Dependencies)
		{
			var dependencyKey = GetTypeDependencyKey(dependency);
			if (inlineTypeKeys.Contains(dependencyKey))
			{
				AddInlineTypeContent(
					endpointModulePath,
					dependency,
					inlineTypeKeys,
					inlinedTypeKeys,
					importLines,
					inlineTypeBlocks);
				continue;
			}

			importLines.Add($"import {{ {dependency.SchemaName} }} from '{GetRelativeImportPath(endpointModulePath, GetTypeModulePath(dependency))}';");
		}

		inlineTypeBlocks.Add(objectTypeRenderResult.Body);
	}

	private string BuildTypeImportLine(string targetModulePath, TypeDescriptor typeDescriptor, TypeUsage usage)
	{
		var importPath = GetRelativeImportPath(targetModulePath, GetTypeModulePath(typeDescriptor, usage));
		var parseFunctionName = $"parse{typeDescriptor.Name}";

		if (typeDescriptor.TypeSymbol.TypeKind == TypeKind.Enum)
			return $"import {{ {parseFunctionName}, {typeDescriptor.Name} }} from '{importPath}';";

		return $"import {{ {parseFunctionName}, type {typeDescriptor.Name} }} from '{importPath}';";
	}

	private TypeDescriptor? ResolveReturnTypeDescriptor(EndpointDescriptor endpointDescriptor, bool isListLike)
	{
		if (isListLike && endpointDescriptor.ReturnType.TypeArguments.FirstOrDefault() is INamedTypeSymbol listItemType)
		{
			return TypeDescriptors.FirstOrDefault(x =>
				x.TypeUsage == TypeUsage.Response &&
				x.FullyQualifiedName == listItemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
		}

		return TypeDescriptors.FirstOrDefault(x =>
			x.TypeUsage == TypeUsage.Response &&
			x.FullyQualifiedName == endpointDescriptor.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
	}

	private TypeDescriptor? ResolveRequestTypeDescriptor(EndpointDescriptor endpointDescriptor)
	{
		if (endpointDescriptor.RequestType is null)
			return null;

		return TypeDescriptors.FirstOrDefault(x =>
			x.TypeUsage == TypeUsage.Request &&
			x.FullyQualifiedName == endpointDescriptor.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
	}

	private string GenerateRuntimeModule()
	{
		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine(TypescriptFileHeader);
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine("import * as valibot from 'valibot';");
		_ = stringBuilder.AppendLine("import { useQuery } from '@tanstack/vue-query';");
		_ = stringBuilder.AppendLine("import { unref, type MaybeRef } from 'vue';");
		_ = stringBuilder.AppendLine($"import {{ {config.TsApiClientName} as configuredApiClient }} from '{config.TsApiClientPath}';");
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine("export const v = valibot;");
		_ = stringBuilder.AppendLine("export const apiClient = configuredApiClient;");
		_ = stringBuilder.AppendLine("export { useQuery, unref };");
		_ = stringBuilder.AppendLine("export type { MaybeRef };");
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine("export interface ExtraQueryOptions {");
		_ = stringBuilder.AppendLine("\tenabled?: MaybeRef<boolean>;");
		_ = stringBuilder.AppendLine("\tstaleTime?: number;");
		_ = stringBuilder.AppendLine("\tretry?: MaybeRef<boolean>;");
		_ = stringBuilder.AppendLine("}");

		return stringBuilder.ToString();
	}

	private string GenerateQueryKeysModule()
	{
		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine(TypescriptFileHeader);
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine(GenerateQueryKeys());
		return stringBuilder.ToString();
	}

	private string GenerateEnumTypeModule(TypeDescriptor typeDescriptor, string modulePath)
	{
		var flags = typeDescriptor.TypeSymbol.HasAttributeWithFullyQualifiedName("System.FlagsAttribute");
		var fields = typeDescriptor.TypeSymbol.GetMembers().OfType<IFieldSymbol>();
		var schemaBuilder = new ValibotEnumSchemaBuilder(typeDescriptor.Name, typeDescriptor.SchemaName, flags);
		foreach (var field in fields)
			_ = schemaBuilder.WithMember(field.Name, (int)field.ConstantValue!);

		var schemaParseFunctionTemplate = Utility.SchemaParseFunctionTemplate;
		var schemaParseFunction = schemaParseFunctionTemplate.Render(new
		{
			FunctionName = $"parse{typeDescriptor.Name}",
			InferredInput = typeDescriptor.Name,
			InferredOutput = typeDescriptor.Name,
			typeDescriptor.SchemaName,
		});

		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine(TypescriptFileHeader);
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine($"import {{ v }} from '{GetRelativeImportPath(modulePath, "common/runtime.ts")}';");
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine(schemaBuilder.Build());
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine(schemaParseFunction);

		return stringBuilder.ToString();
	}

	private string GenerateObjectTypeModule(TypeDescriptor typeDescriptor, string modulePath)
	{
		var objectTypeRenderResult = RenderObjectType(typeDescriptor);

		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine(TypescriptFileHeader);
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine($"import {{ v }} from '{GetRelativeImportPath(modulePath, "common/runtime.ts")}';");
		foreach (var dependency in objectTypeRenderResult.Dependencies)
		{
			var dependencyPath = GetTypeModulePath(dependency);
			var dependencyImport = $"import {{ {dependency.SchemaName} }} from '{GetRelativeImportPath(modulePath, dependencyPath)}';";
			_ = stringBuilder.AppendLine(dependencyImport);
		}

		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine(objectTypeRenderResult.Body);

		return stringBuilder.ToString();
	}

	private ObjectTypeRenderResult RenderObjectType(TypeDescriptor typeDescriptor)
	{
		var schemaBuilder = new ValibotObjectSchemaBuilder(typeDescriptor.SchemaName);

		foreach (var property in typeDescriptor.Properties)
		{
			_ = schemaBuilder.WithProperty(
				property.ValibotPropertyName,
				GetValibotSchemaFromType(property.PropertyType, typeDescriptor.TypeUsage));
		}

		var dependencies = GetObjectTypeDependencies(typeDescriptor);

		var schemaParseFunctionTemplate = Utility.SchemaParseFunctionTemplate;
		var schemaParseFunction = schemaParseFunctionTemplate.Render(new
		{
			FunctionName = $"parse{typeDescriptor.Name}",
			InferredInput = $"v.InferInput<typeof {typeDescriptor.SchemaName}>",
			InferredOutput = $"v.InferOutput<typeof {typeDescriptor.SchemaName}>",
			typeDescriptor.SchemaName,
		});

		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine(schemaBuilder.Build());
		_ = stringBuilder.AppendLine();
		_ = typeDescriptor.TypeUsage == TypeUsage.Request
			? stringBuilder.AppendLine($"export type {typeDescriptor.Name} = v.InferInput<typeof {typeDescriptor.SchemaName}>")
			: stringBuilder.AppendLine($"export type {typeDescriptor.Name} = v.InferOutput<typeof {typeDescriptor.SchemaName}>");
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine(schemaParseFunction);

		return new ObjectTypeRenderResult(stringBuilder.ToString(), dependencies);
	}

	private List<TypeDescriptor> GetObjectTypeDependencies(TypeDescriptor typeDescriptor)
	{
		var schemaDependencies = new Dictionary<string, TypeDescriptor>(StringComparer.Ordinal);

		foreach (var property in typeDescriptor.Properties)
		{
			_ = GetValibotSchemaFromType(
				property.PropertyType,
				typeDescriptor.TypeUsage,
				schemaDependencies);
		}

		_ = schemaDependencies.Remove(GetTypeDependencyKey(typeDescriptor));

		return schemaDependencies.Values
			.DistinctBy(GetTypeDependencyKey)
			.OrderBy(x => x.Name, StringComparer.Ordinal)
			.ToList();
	}

	private List<TypeDescriptor> GetEnumDescriptors()
	{
		return TypeDescriptors
			.Where(x => x.TypeSymbol.TypeKind == TypeKind.Enum)
			.GroupBy(x => x.FullyQualifiedName, StringComparer.Ordinal)
			.Select(x => x.First())
			.OrderBy(x => x.Name, StringComparer.Ordinal)
			.ToList();
	}

	private List<TypeDescriptor> GetTypeDescriptorsByUsage(TypeUsage usage)
	{
		return TypeDescriptors
			.Where(x => x.TypeUsage == usage && x.TypeSymbol.TypeKind != TypeKind.Enum)
			.OrderBy(x => x.Name, StringComparer.Ordinal)
			.ToList();
	}

	private string BuildBarrelFile(string indexModulePath, IEnumerable<string> exportedModulePaths)
	{
		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine(TypescriptFileHeader);
		_ = stringBuilder.AppendLine();

		foreach (var exportedModulePath in exportedModulePaths.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal))
		{
			var importPath = GetRelativeImportPath(indexModulePath, exportedModulePath);
			_ = stringBuilder.AppendLine($"export * from '{importPath}';");
		}

		return stringBuilder.ToString();
	}

	private string BuildRootIndexFile()
	{
		var stringBuilder = new StringBuilder();
		_ = stringBuilder.AppendLine(TypescriptFileHeader);
		_ = stringBuilder.AppendLine();
		_ = stringBuilder.AppendLine("export * from './common/queryKeys';");
		_ = stringBuilder.AppendLine("export * from './common/runtime';");
		_ = stringBuilder.AppendLine("export * from './enums';");
		_ = stringBuilder.AppendLine("export * from './types/requests';");
		_ = stringBuilder.AppendLine("export * from './types/responses';");
		_ = stringBuilder.AppendLine("export * from './endpoints';");
		return stringBuilder.ToString();
	}

	private static string GetRelativeImportPath(string fromRelativePath, string toRelativePath)
	{
		var fromDirectory = Path.GetDirectoryName(fromRelativePath.Replace('/', Path.DirectorySeparatorChar))
			?? throw new InvalidOperationException($"Invalid relative path: {fromRelativePath}");

		var relativePath = Path.GetRelativePath(
			fromDirectory,
			toRelativePath.Replace('/', Path.DirectorySeparatorChar));
		relativePath = relativePath.Replace('\\', '/');

		if (!relativePath.StartsWith(".", StringComparison.Ordinal))
			relativePath = "./" + relativePath;

		if (relativePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
			relativePath = relativePath[..^3];

		return relativePath;
	}

	private static string GetRelativeImportPathFromAbsolute(string fromAbsolutePath, string toAbsolutePath)
	{
		var fromDirectory = Path.GetDirectoryName(fromAbsolutePath)
			?? throw new InvalidOperationException($"Invalid absolute path: {fromAbsolutePath}");

		var relativePath = Path.GetRelativePath(fromDirectory, toAbsolutePath).Replace('\\', '/');
		if (!relativePath.StartsWith(".", StringComparison.Ordinal))
			relativePath = "./" + relativePath;

		if (relativePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
			relativePath = relativePath[..^3];

		return relativePath;
	}

	private string GetTypeModulePath(TypeDescriptor descriptor)
	{
		return descriptor.TypeSymbol.TypeKind == TypeKind.Enum
			? GetTypeModulePath(descriptor, descriptor.TypeUsage)
			: GetTypeModulePath(descriptor, descriptor.TypeUsage);
	}

	private static string GetTypeModulePath(TypeDescriptor descriptor, TypeUsage usage)
	{
		if (descriptor.TypeSymbol.TypeKind == TypeKind.Enum)
			return $"enums/{descriptor.Name}.ts";

		var usageFolder = usage == TypeUsage.Request ? "requests" : "responses";
		return $"types/{usageFolder}/{descriptor.Name}.ts";
	}

	private string BuildEndpointModulePath(EndpointDescriptor endpointDescriptor)
	{
		var pathSegments = endpointDescriptor.Path
			.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(ToRouteFolderSegment)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToList();

		var moduleName = endpointDescriptor.EndpointWrapperType.Name.ToCamelCase() + ".ts";

		return pathSegments.Count == 0
			? $"endpoints/{moduleName}"
			: $"endpoints/{string.Join("/", pathSegments)}/{moduleName}";
	}

	private static string ToRouteFolderSegment(string segment)
	{
		if (segment.StartsWith("{", StringComparison.Ordinal) && segment.EndsWith("}", StringComparison.Ordinal))
		{
			var parameterName = segment[1..^1];

			if (parameterName.StartsWith("*", StringComparison.Ordinal))
				parameterName = parameterName[1..];

			var constraintSeparatorIndex = parameterName.IndexOf(':', StringComparison.Ordinal);
			if (constraintSeparatorIndex > 0)
				parameterName = parameterName[..constraintSeparatorIndex];

			parameterName = parameterName.TrimEnd('?');
			return $"[{SanitizePathToken(parameterName)}]";
		}

		return SanitizePathToken(segment);
	}

	private static string SanitizePathToken(string token)
	{
		if (string.IsNullOrWhiteSpace(token))
			return "_";

		var stringBuilder = new StringBuilder(token.Length);
		var invalidChars = Path.GetInvalidFileNameChars();

		foreach (var character in token)
		{
			var shouldReplace = invalidChars.Contains(character) ||
				character is '/' or '\\' or '{' or '}' or ':' or '*' or '"' or '<' or '>' or '|';

			if (shouldReplace)
				_ = stringBuilder.Append('_');
			else
				_ = stringBuilder.Append(character);
		}

		var sanitized = stringBuilder.ToString();
		return string.IsNullOrWhiteSpace(sanitized) ? "_" : sanitized;
	}

	private static string AppendSuffixToTsFile(string modulePath, string suffix)
	{
		if (!modulePath.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
			return modulePath + suffix;

		return modulePath[..^3] + suffix + ".ts";
	}

	private static string GetTypeDependencyKey(TypeDescriptor typeDescriptor)
	{
		return $"{typeDescriptor.FullyQualifiedName}|{typeDescriptor.TypeUsage}";
	}

	private static string GetEndpointKey(EndpointDescriptor endpointDescriptor)
	{
		var endpointTypeName = endpointDescriptor.EndpointWrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		return $"{endpointTypeName}|{endpointDescriptor.HttpMethod}|{endpointDescriptor.Path}";
	}
}
