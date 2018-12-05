using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using MiniAnalyzers.Tools;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniAnalyzers.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingleLineCommentSentenceCodeFix)), Shared]
    public class SingleLineCommentSentenceCodeFix : CodeFixProvider
    {
        public const string DiagnosticId = SingleLineCommentSentenceAnalyzer.DiagnosticId;

        private const string title = "Make first letter upper case";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var commentTrivia = root.FindTrivia(diagnosticSpan.Start);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => replaceWithUpperCaseComment(context.Document, commentTrivia, c),
                    equivalenceKey: nameof(SingleLineCommentSentenceCodeFix)),
                    diagnostic: diagnostic);
        }

        private async Task<Document> replaceWithUpperCaseComment(Document document, SyntaxTrivia commentTrivia, CancellationToken c)
        {
            var plainText = commentTrivia.ToString();
            var contentText = commentTrivia.GetCommentContent().Trim();
            var firstChar = contentText[0];
            var newFirstChar = char.ToUpperInvariant(firstChar);
            var newPlainText = plainText.ReplaceFirst(firstChar, newFirstChar);

            var newTrivia = SyntaxFactory.Comment(newPlainText)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceTrivia(commentTrivia, newTrivia);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}
