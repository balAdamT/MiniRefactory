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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InitializerListCodeFixProvider)), Shared]
    public class InitializerListCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add missing variables to initializer list!";
        public const string DiagnosticId = InitializerListAnalyzer.DiagnosticId;
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var initializerNode = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<InitializerExpressionSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddMissingVariables(context.Document, initializerNode, c),
                    equivalenceKey: nameof(InitializerListCodeFixProvider)),
                diagnostic);
        }

        private async Task<Document> AddMissingVariables(Document document, InitializerExpressionSyntax initializerNode, CancellationToken c)
        {
            var model = await document.GetSemanticModelAsync();
            var type = model.GetTypeInfo(initializerNode.Parent).Type;
            var all = model.LookupSymbols(initializerNode.SpanStart, type).Where(m => m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property);
            var foundOnes = initializerNode.Expressions.Where(e => e is AssignmentExpressionSyntax).Select(e => e as AssignmentExpressionSyntax).Select(e => e.Left.ToString());
            var missingOnes = all.Select(s => s.Name).Except(foundOnes);

            var newOnes = missingOnes.Select(missingName =>
            {
                var fieldOrPropertySymbol = all.Where(s => s.Name == missingName).Single();
                var declarationSyntax = fieldOrPropertySymbol.DeclaringSyntaxReferences.Single().GetSyntax();

                TypeSyntax typeSyntax = null;
                switch (declarationSyntax.Kind())
                {
                    case SyntaxKind.VariableDeclarator:
                        typeSyntax = ((declarationSyntax as VariableDeclaratorSyntax).Parent as VariableDeclarationSyntax).Type;
                        break;
                    case SyntaxKind.FieldDeclaration:
                        typeSyntax = (declarationSyntax as PropertyDeclarationSyntax).Type;
                        break;
                }

                var name = SyntaxFactory.IdentifierName(missingName);
                var defaultValue = SyntaxFactory.DefaultExpression(typeSyntax);

                return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, name, defaultValue);
            });

            var newInitializerList = initializerNode.Expressions.AddRange(newOnes);
            var newInitializerNode = initializerNode.WithExpressions(newInitializerList);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(initializerNode, newInitializerNode);

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}