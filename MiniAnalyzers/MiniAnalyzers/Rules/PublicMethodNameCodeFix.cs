using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using MiniAnalyzers.Tools;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniAnalyzers.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrivateMethodNameCodeFix)), Shared]
    public class PublicMethodNameCodeFix : CodeFixProvider
    {
        public const string DiagnosticId = PublicMethodNameAnalyzer.DiagnosticId;

        private const string title = "Replace first letter with uppercase";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => renameMethod(context.Document.Project.Solution, methodDeclaration, c),
                    equivalenceKey: nameof(PublicMethodNameCodeFix),
                    diagnostic: diagnostic);
        }

        private async Task<Solution> renameMethod(Solution solution, MethodDeclarationSyntax methodDeclaration, CancellationToken c)
        {
            var document = solution.GetDocument(methodDeclaration.SyntaxTree);
            var semanticModel = await document.GetSemanticModelAsync(c);

            var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration, c);

            var options = solution.Workspace.Options;
            var newName = symbol.Name.FirstCharacterToUpper();

            var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, newName, options);

            return newSolution;
        }


    }
}
