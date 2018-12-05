namespace MiniAnalyzers.Tools
{
    public static class StringExtensions
    {
        public static string FirstCharacterToUpper(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsUpper(str, 0))
                return str;

            return char.ToUpperInvariant(str[0]) + str.Substring(1);
        }

        public static string FirstCharacterToLower(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str, 0))
                return str;

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static string ReplaceFirst(this string text, char search, char replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + 1);
        }
    }
}
