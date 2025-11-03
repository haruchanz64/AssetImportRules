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
        public static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/').Trim();
        }

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
