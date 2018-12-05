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
    public class PublicMethodNameAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BA00003";
        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Public method name should begin with uppercase letter",
            messageFormat: "Public method name \"{0}\" should begin with upper case letter.",
            category: "Naming",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(analyzeNode, SyntaxKind.MethodDeclaration);

        private void analyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated())
                return;

            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            if (!methodDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword))
                return;

            var firstCharacter = methodDeclaration.Identifier.Text[0];
            if (char.IsLower(firstCharacter))
                context.ReportDiagnostic(Diagnostic.Create(rule, getReportLocation(methodDeclaration), methodDeclaration.Identifier.Text));
        }

        private static Location getReportLocation(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Identifier.GetLocation();
        }
    }
}
