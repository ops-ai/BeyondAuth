using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BeyondAuth.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ISecurableAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AccessControlBypass";
        private const string Category = "Security";

        private static readonly LocalizableString Title = "IAuthorizationService not used";
        private static readonly LocalizableString MessageFormat = "Class {0} implements ISecurable, but was not passed through IAuthorizationService.";
        private static readonly LocalizableString Description = "This analyzer checks if a class that implements the ISecurable interface is passed through IAuthorizationService.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeClassCreationExpression, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeClassCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var creationExpression = context.Node as ObjectCreationExpressionSyntax;
            if (creationExpression == null) return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(creationExpression.Type);
            if (symbolInfo.Symbol == null) return;

            var typeSymbol = symbolInfo.Symbol as INamedTypeSymbol;
            if (typeSymbol == null) return;

            if (typeSymbol.AllInterfaces.Any(x => x.Name == "ISecurable"))
            {
                var variableSymbol = context.SemanticModel.GetSymbolInfo(creationExpression).Symbol;
                var variableSyntax = variableSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
                var variableName = variableSyntax?.Identifier.ValueText;
                if (variableName != null && !IsPassedToIAuthorizationService(context, variableName))
                {
                    var diagnostic = Diagnostic.Create(Rule, creationExpression.GetLocation(), variableName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsPassedToIAuthorizationService(SyntaxNodeAnalysisContext context, string variableName)
        {
            var invocationExpressions = context.Node.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocationExpression in invocationExpressions)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpression.Expression);
                if (symbolInfo.Symbol?.ContainingType?.Name == "IAuthorizationService")
                {
                    var argumentList = invocationExpression.ArgumentList;
                    foreach (var argument in argumentList.Arguments)
                    {
                        var identifier = argument.Expression as IdentifierNameSyntax;
                        if (identifier != null && identifier.Identifier.ValueText == variableName)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

}
