install-new-version:
	dotnet pack src/TypescriptGenerator.Console/TypescriptGenerator.Console.csproj -c Release -p:MinVerVersion=0.1.0-preview.0.25
	dotnet tool uninstall -g Duke.TypescriptGenerator
	dotnet tool install -g Duke.TypescriptGenerator --version "0.1.0-preview.0.25" --add-source /Users/duke/Dev/Personal/TypescriptGenerator/src/TypescriptGenerator.Console/nupkg
