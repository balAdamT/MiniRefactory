using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace MiniAnalyzers.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InitializerListAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BA00006";
        internal static readonly LocalizableString Title = "The initializer list is lacking some members!";
        internal static readonly LocalizableString MessageFormat = "The following members are not initialized: '{0}'";
        internal const string Category = "Structure";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInitializer, SyntaxKind.ObjectInitializerExpression);
        }

        private void AnalyzeInitializer(SyntaxNodeAnalysisContext context)
        {
            var initializerNode = (InitializerExpressionSyntax)context.Node;
            var type = context.SemanticModel.GetTypeInfo(initializerNode.Parent).Type;

            var all = context.SemanticModel.LookupSymbols(initializerNode.SpanStart, type).Where(m => m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property);
            var foundOnes = initializerNode.Expressions.OfType<AssignmentExpressionSyntax>().Cast<AssignmentExpressionSyntax>().Select(e => e.Left.ToString());

            var missingOnes = all.Select(s => s.Name).Except(foundOnes);
            if (missingOnes.Count() > 0)
            {
                var missingOnesString = string.Join(" ,", missingOnes);
                var diagnostic = Diagnostic.Create(Rule, initializerNode.GetLocation(), missingOnesString);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}