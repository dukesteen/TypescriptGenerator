# TypescriptGenerator

## Console config

`TypescriptGenerator.Console` expects a JSON config file like:

```json
{
  "csprojPath": "path/to/YourProject.csproj",
  "generateTypesInNamespacesIncludes": ["Timespace."],
  "generateTypesInNamespacesExcludes": ["Timespace.Internal."],
  "tsApiClientName": "apiClient",
  "tsApiClientPath": "@/apiClient",
  "outputPath": "path/to/generated.ts",
  "policiesOutputPath": "path/to/policies.ts"
}
```
