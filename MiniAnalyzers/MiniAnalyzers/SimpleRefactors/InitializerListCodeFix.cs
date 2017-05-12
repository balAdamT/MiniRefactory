using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace MiniAnalyzers.SimpleRefactors
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InitializerListCodeFix)), Shared]
    public class InitializerListCodeFix : CodeFixProvider
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

            foreach (var diagnostic in context.Diagnostics.Where(d => d.Id == DiagnosticId))
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var initializerNode = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InitializerExpressionSyntax>().First();

                context.RegisterCodeFix(CodeAction.Create(title, c => AddMissingVariables(context.Document, initializerNode, c), equivalenceKey: title), diagnostic);
            }
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
                switch(declarationSyntax.Kind())
                {
                    case SyntaxKind.VariableDeclarator:
                        //TODO refactor
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