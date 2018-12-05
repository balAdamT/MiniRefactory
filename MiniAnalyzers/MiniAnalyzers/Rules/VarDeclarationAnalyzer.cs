using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MiniAnalyzers.Tools;
using System.Collections.Immutable;
using System.Linq;

namespace MiniAnalyzers.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VarDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BA00005";
        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Local variable should use implicit declaration where possible",
            messageFormat: "Local variable declaration \"{0}\" should use the var keyword.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(analyzeNode, SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement);

        private void analyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated())
                return;

            VariableDeclarationSyntax variableDeclaration;
            if (context.Node is LocalDeclarationStatementSyntax localDeclaration && !localDeclaration.IsConst)
            {
                variableDeclaration = localDeclaration.Declaration;
            }
            else if (context.Node is ForStatementSyntax forStatement && forStatement.Declaration != null)
            {
                variableDeclaration = forStatement.Declaration;
            }
            else
                return;

            if (isReportable(variableDeclaration, context.SemanticModel))
                context.ReportDiagnostic(Diagnostic.Create(rule, getReportLocation(variableDeclaration), variableDeclaration));
        }

        private bool isReportable(VariableDeclarationSyntax declaration, SemanticModel model)
        {
            // Check if declaration is var
            if (declaration.Type.IsVar)
                return false;

            // Check if declaration has multiple variables
            if (declaration.Variables.Count > 1)
                return false;

            var variable = declaration.Variables.Single();

            // Check if the variable has initializer
            if (variable.Initializer is null)
                return false;

            var initializer = variable.Initializer;

            // Check if the variable is initialized as null
            if (initializer.Value.Kind() == SyntaxKind.NullLiteralExpression)
                return false;

            var declaredType = model.GetTypeInfo(declaration.Type);
            var staticType = model.GetTypeInfo(initializer.Value);

            // Check if the declared type is the same as the static type
            if (declaredType.Type == staticType.Type)
                return true;

            return false;
        }

        private Location getReportLocation(VariableDeclarationSyntax declaration)
        {
            return declaration.Type.GetLocation();
        }
    }
}
