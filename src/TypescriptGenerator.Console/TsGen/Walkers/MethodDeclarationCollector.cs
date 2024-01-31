using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TypescriptGenerator.Console.TsGen.Walkers;

public class MethodDeclarationCollector(SemanticModel model) : CSharpSyntaxWalker
{
	public IList<IMethodSymbol> MethodSymbols { get; } = new List<IMethodSymbol>();

	public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
	{
		var methodSymbol = model.GetDeclaredSymbol(node) ?? throw new InvalidOperationException("Failed to get method symbol");

		var httpAttributeCount = methodSymbol.GetAttributes()
			.Count(x => Constants.HttpAttributeNames.Contains(x.AttributeClass?.ToDisplayString() ?? string.Empty));

		if (httpAttributeCount == 1)
			MethodSymbols.Add(methodSymbol);
		else if (httpAttributeCount > 1)
			throw new InvalidOperationException("Controller actions with multiple HTTP attributes are not supported.");

		base.VisitMethodDeclaration(node);
	}
}
