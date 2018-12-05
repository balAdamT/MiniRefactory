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
    public class PropertyDocumentationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BA00002";
        private static readonly DiagnosticDescriptor rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Propery summaries should use get or set comment format",
            messageFormat: "\"{0}\" property summary should start with {1}.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
        private const string getMessage = "Gets";
        private const string setMessage = "Sets";
        private const string getAndSetMessage = "Gets or sets";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(analyzeNode, SyntaxKind.PropertyDeclaration);

        private void analyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated())
                return;

            var property = (PropertyDeclarationSyntax)context.Node;

            // Accessor list might be null if arrow-head notation
            var publicAccessors = property.AccessorList?.Accessors
                .Where(a => !a.Modifiers.Any(m => m.Kind() == SyntaxKind.PrivateKeyword || m.Kind() == SyntaxKind.ProtectedKeyword || m.Kind() == SyntaxKind.InternalKeyword))
                ?? Enumerable.Empty<AccessorDeclarationSyntax>();

            if (property.AccessorList != null && !publicAccessors.Any())
                // Do not analyze erroneous or private properties
                return;

            var trivias = property.GetLeadingTrivia();
            var documentation = trivias.Where(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia);

            if (!documentation.Any())
                // Do not analyze properties withotu documentation
                return;

            var relevantDocumentation = documentation.Last();
            var documentationSyntax = (DocumentationCommentTriviaSyntax)relevantDocumentation.GetStructure();
            var summarySyntax = documentationSyntax.Content.OfType<XmlElementSyntax>().FirstOrDefault(xml => xml.StartTag.Name.ToString() == "summary");
            var textSyntax = summarySyntax?.Content.OfType<XmlTextSyntax>().FirstOrDefault();
            var textOrNull = textSyntax?.TextTokens.FirstOrDefault(t => t.Kind() == SyntaxKind.XmlTextLiteralToken);

            if (textOrNull == null)
                // Do not analyze properties without summary
                return;

            var actualText = textOrNull.Value;
            var expectedText = default(string);
            if (property.AccessorList == null)
            {
                expectedText = getMessage;
            }
            else if (publicAccessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration))
            {
                if (publicAccessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration))
                    expectedText = getAndSetMessage;
                else
                    expectedText = getMessage;
            }
            else
            {
                expectedText = setMessage;
            }

            if (!actualText.Text.Trim().StartsWith(expectedText))
                context.ReportDiagnostic(Diagnostic.Create(rule, actualText.GetLocation(), property.Identifier.Text, expectedText));
        }
    }
}
