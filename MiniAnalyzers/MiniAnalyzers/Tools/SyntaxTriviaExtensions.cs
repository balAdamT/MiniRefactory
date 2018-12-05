using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MiniAnalyzers.Tools
{
    public static class SyntaxTriviaExtensions
    {
        public static string GetCommentContent(this SyntaxTrivia commentNode)
        {
            var contentText = default(string);
            var fullString = commentNode.ToString();

            switch (commentNode.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                    // Remove // from the beginning
                    contentText = fullString.Substring(2);

                    break;
                case SyntaxKind.MultiLineCommentTrivia:
                    // Remove /* from the beginning and */ from the end
                    contentText = fullString.Substring(2, fullString.Length - 4);
                    break;

                default:
                    return string.Empty;
            }

            return contentText;
        }
    }
}
