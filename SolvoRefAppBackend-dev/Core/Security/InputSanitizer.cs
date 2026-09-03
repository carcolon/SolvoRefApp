using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace Core.Security
{
    public static class InputSanitizer
    {
        private static readonly HtmlSanitizer PlainTextSanitizer = new();
        private static readonly HtmlSanitizer HtmlFragmentSanitizer = CreateHtmlFragmentSanitizer();
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex UnsafeControlCharsRegex = new("[\\u0000-\\u0008\\u000B\\u000C\\u000E-\\u001F\\u007F]", RegexOptions.Compiled);

        public static string SanitizePlainText(string? value, bool preserveNewLines = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sanitized = WebUtility.HtmlDecode(value).Trim();
            sanitized = PlainTextSanitizer.Sanitize(sanitized);
            sanitized = HtmlTagRegex.Replace(sanitized, string.Empty);
            sanitized = sanitized.Replace("<", string.Empty).Replace(">", string.Empty);
            sanitized = UnsafeControlCharsRegex.Replace(sanitized, string.Empty);

            if (!preserveNewLines)
            {
                sanitized = sanitized.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
                sanitized = Regex.Replace(sanitized, "\\s+", " ");
            }

            return WebUtility.HtmlDecode(sanitized).Trim();
        }

        public static string SanitizeHtmlFragment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sanitized = WebUtility.HtmlDecode(value).Trim();
            sanitized = HtmlFragmentSanitizer.Sanitize(sanitized);
            sanitized = UnsafeControlCharsRegex.Replace(sanitized, string.Empty);

            return sanitized.Trim();
        }

        private static HtmlSanitizer CreateHtmlFragmentSanitizer()
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.UnionWith(["p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "span"]);
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedSchemes.Clear();
            return sanitizer;
        }
    }
}
