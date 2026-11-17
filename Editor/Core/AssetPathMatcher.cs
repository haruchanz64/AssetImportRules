using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AssetImportRules.Editor.Core
{
    /// <summary>
    /// Pure C# asset path matcher supporting folder prefixes, glob patterns (*, **, ?),
    /// extension filters, and exclusion patterns.
    /// </summary>
    public static class AssetPathMatcher
    {
        /// <summary>
        /// Normalizes an asset path or pattern by converting backslashes to forward slashes and trimming whitespace.
        /// </summary>
        /// <param name="path">Raw asset path or pattern string.</param>
        /// <returns>Normalized forward-slash path, or <see cref="string.Empty"/> when null or empty.</returns>
        public static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/').Trim();
        }

        /// <summary>
        /// Evaluates whether <paramref name="assetPath"/> matches <paramref name="pathPattern"/> after applying
        /// optional exclusion patterns and case-insensitive file extension filters.
        /// </summary>
        /// <param name="assetPath">Project-relative asset path to test.</param>
        /// <param name="pathPattern">Folder prefix or glob pattern (<c>*</c>, <c>**</c>, <c>?</c>). Blank matches all paths.</param>
        /// <param name="extensions">Optional allowed file extensions (with or without leading dot).</param>
        /// <param name="excludePatterns">Optional exclusion patterns evaluated before inclusion matching.</param>
        /// <returns><c>true</c> if the path is not excluded, passes extension filtering, and matches <paramref name="pathPattern"/>.</returns>
        public static bool Matches(
            string assetPath,
            string pathPattern,
            IReadOnlyList<string> extensions = null,
            IReadOnlyList<string> excludePatterns = null)
        {
            string norm = NormalizePath(assetPath);
            if (string.IsNullOrEmpty(norm)) return false;

            if (excludePatterns != null)
            {
                for (int i = 0; i < excludePatterns.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(excludePatterns[i]) &&
                        MatchesSinglePattern(norm, excludePatterns[i]))
                    {
                        return false;
                    }
                }
            }

            if (extensions != null && extensions.Count > 0)
            {
                string ext = Path.GetExtension(norm);
                bool extMatch = false;
                for (int i = 0; i < extensions.Count; i++)
                {
                    string targetExt = (extensions[i] ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(targetExt)) continue;
                    if (!targetExt.StartsWith(".", StringComparison.Ordinal)) targetExt = "." + targetExt;
                    if (string.Equals(ext, targetExt, StringComparison.OrdinalIgnoreCase))
                    {
                        extMatch = true;
                        break;
                    }
                }
                if (!extMatch) return false;
            }

            if (string.IsNullOrWhiteSpace(pathPattern))
            {
                return true;
            }

            return MatchesSinglePattern(norm, pathPattern);
        }

        /// <summary>
        /// Tests a normalized forward-slash asset path against a single folder prefix or glob pattern (case-insensitive).
        /// </summary>
        /// <param name="normalizedAssetPath">Forward-slash normalized asset path.</param>
        /// <param name="rawPattern">Folder prefix or glob pattern supporting <c>*</c>, <c>**</c>, and <c>?</c>.</param>
        /// <returns><c>true</c> if the path matches the pattern; otherwise <c>false</c>.</returns>
        public static bool MatchesSinglePattern(string normalizedAssetPath, string rawPattern)
        {
            string pattern = NormalizePath(rawPattern);
            if (string.IsNullOrEmpty(pattern)) return false;

            // Plain folder prefix (no glob wildcards)
            if (!pattern.Contains("*") && !pattern.Contains("?"))
            {
                string prefix = pattern.EndsWith("/", StringComparison.Ordinal) ? pattern : pattern + "/";
                return normalizedAssetPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(normalizedAssetPath, pattern, StringComparison.OrdinalIgnoreCase);
            }

            string regex = GlobToRegex(pattern);
            return Regex.IsMatch(normalizedAssetPath, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Converts a normalized glob expression (<c>**</c>, <c>*</c>, <c>?</c>) into an anchored regular expression.
        /// </summary>
        private static string GlobToRegex(string glob)
        {
            // Escape regex metacharacters first, then translate glob tokens
            string escaped = Regex.Escape(glob);
            escaped = escaped.Replace(@"\*\*/", "(?:.*/)?");
            escaped = escaped.Replace(@"\*\*", ".*");
            escaped = escaped.Replace(@"\*", "[^/]*");
            escaped = escaped.Replace(@"\?", "[^/]");
            return "^" + escaped + "$";
        }
    }
}
