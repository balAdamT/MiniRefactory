using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MiniAnalyzers.Tools;
using System.Collections.Immutable;
using System.Linq;

namespace MiniAnalyzers.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SingleLineCommentSentenceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BA00004";

        private static readonly DiagnosticDescriptor rule_StartsWithUpper = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Comments should start with upper case letter",
            messageFormat: "Comment \"{0}\" should start with an upper case letter.",
            category: "Commenting",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule_StartsWithUpper);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxTreeAction(analyzeTree);

        private void analyzeTree(SyntaxTreeAnalysisContext context)
        {
            if (context.IsGenerated())
                return;

            var tree = context.Tree;

            if (ignoreTree(tree))
                return;

            var root = tree.GetRoot();
            var comments = root.DescendantTrivia().Where(t => t.Kind() == SyntaxKind.SingleLineCommentTrivia);

            foreach (var commentNode in comments)
            {
                // Checking whitespacing is not the responsibility of this analyzer.
                var text = commentNode.GetCommentContent().Trim();

                if (string.IsNullOrEmpty(text))

                    continue;

                if (ignoreComment(text))
                    continue;

                if (char.IsLower(text[0]))
                    context.ReportDiagnostic(Diagnostic.Create(rule_StartsWithUpper, commentNode.GetLocation(), text));
            }
        }

        private bool ignoreTree(SyntaxTree tree)
        {
            var filePath = tree.FilePath;
            if (filePath.EndsWith("AssemblyInfo.cs"))
                return true;

            return false;
        }

        private bool ignoreComment(string text)
        {
            // Do not check xml comments (After trimming, only one slash remains).
            if (text.StartsWith(@"/"))
                return true;

            // Do not check TODO comments.
            if (text.StartsWith(@"TODO"))
                return true;

            return false;
        }
    }
}
