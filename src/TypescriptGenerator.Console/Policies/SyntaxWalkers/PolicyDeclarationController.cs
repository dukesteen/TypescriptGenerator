using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TypescriptGenerator.Console.Policies.SyntaxWalkers;

internal class PolicyDeclarationController(SemanticModel model) : CSharpSyntaxWalker
{
	public IList<INamedTypeSymbol> PolicyDeclarations { get; } = [];

	public override void VisitClassDeclaration(ClassDeclarationSyntax node)
	{
		var classSymbol = model.GetDeclaredSymbol(node) ?? throw new InvalidOperationException("Failed to get class symbol");
		if (classSymbol.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == "Timespace.GeneratePermissionPolicyAttribute"))
			PolicyDeclarations.Add(classSymbol);

		base.VisitClassDeclaration(node);
	}
}
