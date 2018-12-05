using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniAnalyzers.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumCodeFixProvider)), Shared]
    public class EnumCodeFixProvider : CodeFixProvider
    {
        private const string title = "Put 0 value member first";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EnumAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics.Where(d => d.Id == EnumAnalyzer.DiagnosticId))
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<EnumDeclarationSyntax>().First();

                context.RegisterCodeFix(CodeAction.Create(title, c => ReorderEnum(context.Document, declaration, c), equivalenceKey: nameof(EnumCodeFixProvider)), diagnostic);
            }
        }

        private async Task<Document> ReorderEnum(Document document, EnumDeclarationSyntax enumDeclaration, CancellationToken cancellationToken)
        {
            var oldMembers = enumDeclaration.Members;
            var zeroValue = oldMembers.Where(m =>
            {
                var value = m.EqualsValue?.Value;

                if (value != null && value.Kind() == SyntaxKind.NumericLiteralExpression && (int)((LiteralExpressionSyntax)value).Token.Value == 0)
                    return true;

                return false;
            }).First();

            var newMembers = oldMembers.Remove(zeroValue).Insert(0, zeroValue);

            var newEnum = enumDeclaration.WithMembers(newMembers);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(enumDeclaration, newEnum);

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}