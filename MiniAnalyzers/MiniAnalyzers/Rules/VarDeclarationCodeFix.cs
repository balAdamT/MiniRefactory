using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniAnalyzers.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VarDeclarationCodeFix)), Shared]
    public class VarDeclarationCodeFix : CodeFixProvider
    {
        public const string DiagnosticId = VarDeclarationAnalyzer.DiagnosticId;

        private const string title = "Replace explicit type declaration with var keyword";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var variableDeclaration = root.FindToken(diagnosticSpan.Start).Parent.FirstAncestorOrSelf<VariableDeclarationSyntax>();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => replaceTypeWithvarAsync(context.Document, variableDeclaration, c),
                    equivalenceKey: nameof(VarDeclarationCodeFix)),
                    diagnostic: diagnostic);
        }

        private async Task<Document> replaceTypeWithvarAsync(Document document, VariableDeclarationSyntax declaration, CancellationToken c)
        {
            var oldIdentifier = declaration.Type;
            var newIdentifier = SyntaxFactory.IdentifierName("var")
                  .WithLeadingTrivia(oldIdentifier.GetLeadingTrivia())
                  .WithTrailingTrivia(oldIdentifier.GetTrailingTrivia())
                  .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(oldIdentifier, newIdentifier);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}
