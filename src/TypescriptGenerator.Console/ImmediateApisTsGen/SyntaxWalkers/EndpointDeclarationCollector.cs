using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TypescriptGenerator.Console.ImmediateApisTsGen.SyntaxWalkers;

internal class EndpointDeclarationCollector(SemanticModel model) : CSharpSyntaxWalker
{
	public IList<INamedTypeSymbol> Handlers { get; } = [];

	public override void VisitClassDeclaration(ClassDeclarationSyntax node)
	{
		var classSymbol = model.GetDeclaredSymbol(node) ?? throw new InvalidOperationException("Failed to get class symbol");
		if (classSymbol.GetAttributes().Any(x => Constants.EndpointAttributes.Contains(x.AttributeClass?.ToDisplayString() ?? string.Empty)))
			Handlers.Add(classSymbol);

		base.VisitClassDeclaration(node);
	}
}
