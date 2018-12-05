using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace MiniAnalyzers.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EnumAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BA00007";
        private static readonly LocalizableString Title = "Enum has multiple elements with value 0!";
        private static readonly LocalizableString MessageFormat = "Enum has multiple elements with value 0!";
        private static readonly LocalizableString Description = "The first element of an enum has the value of 0 if there is no explicit value but this enum already contains an element with the value 0!";
        private const string Category = "Structure";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);



        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeEnum, SyntaxKind.EnumDeclaration);
        }

        private void AnalyzeEnum(SyntaxNodeAnalysisContext context)
        {
            var enumNode = (EnumDeclarationSyntax)context.Node;

            var enumMembers = enumNode.Members;

            var firstHasNoValue = false;
            var existValueWith0 = false;
            SyntaxNode erroneousNode = null;

            for (var i = 0; i < enumMembers.Count(); ++i)
            {
                var member = enumMembers[i];
                if (i == 0)
                {
                    if (member.EqualsValue == null)
                        firstHasNoValue = true;
                    else
                        return;
                }
                else
                {
                    if (member.EqualsValue != null)
                    {
                        if (member.EqualsValue.Value.Kind() == SyntaxKind.NumericLiteralExpression)
                        {
                            var value = (LiteralExpressionSyntax)member.EqualsValue.Value;
                            var numericValue = (int)value.Token.Value;

                            if (numericValue == 0)
                            {
                                existValueWith0 = true;
                                erroneousNode = member;
                            }

                        }
                    }
                }

                if (firstHasNoValue && existValueWith0)
                {
                    var diagnostic = Diagnostic.Create(Rule, erroneousNode.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }

            }

        }
    }
}
