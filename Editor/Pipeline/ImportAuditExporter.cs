using System;
using System.Text;
using AssetImportRules.Editor.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AssetImportRules.Editor.Pipeline
{
    /// <summary>
    /// Serializes <see cref="ImportComplianceReport"/> instances to JSON and Markdown summaries.
    /// </summary>
    public static class ImportAuditExporter
    {
        /// <summary>
        /// Serializes <paramref name="report"/> to JSON via <see cref="JsonUtility.ToJson(object, bool)"/>.
        /// </summary>
        /// <param name="report">Compliance report to serialize.</param>
        /// <param name="prettyPrint">When <c>true</c>, formats JSON with indentation and line breaks.</param>
        /// <returns>JSON representation of the report.</returns>
        public static string ToJson(ImportComplianceReport report, bool prettyPrint = true)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            return JsonUtility.ToJson(report, prettyPrint);
        }

        /// <summary>
        /// Formats <paramref name="report"/> as a Markdown document containing summary metrics
        /// and a table of non-compliant assets with <c>current -&gt; expected</c> diffs.
        /// </summary>
        /// <param name="report">Compliance report to format.</param>
        /// <returns>Markdown report text.</returns>
        public static string ToMarkdown(ImportComplianceReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            var sb = new StringBuilder(1024);
            sb.AppendLine($"# Asset Import Compliance Report ({report.policyName})");
            sb.AppendLine();
            sb.AppendLine($"- **Generated (UTC):** `{report.generatedUtc}`");
            sb.AppendLine($"- **Root Folder:** `{report.rootFolder}`");
            sb.AppendLine($"- **Compliance Score:** **{report.complianceScore} / 100**");
            sb.AppendLine($"- **Assets:** {report.totalScannedAssets} scanned | {report.matchedAssets} matched | {report.nonCompliantAssets} non-compliant | {report.exemptedAssets} exempt");
            sb.AppendLine();

            if (report.nonCompliantItems.Count == 0)
            {
                sb.AppendLine("All matched assets comply with the active import policy.");
                return sb.ToString();
            }

            sb.AppendLine("| Asset | Category | Rules | Diffs |");
            sb.AppendLine("| :--- | :--- | :--- | :--- |");
            for (int i = 0; i < report.nonCompliantItems.Count; i++)
            {
                var item = report.nonCompliantItems[i];
                var diffParts = new StringBuilder();
                for (int d = 0; d < item.diffs.Count; d++)
                {
                    if (d > 0) diffParts.Append("; ");
                    var diff = item.diffs[d];
                    diffParts.Append(diff.propertyName).Append(": `").Append(diff.currentValue).Append("` -> `").Append(diff.expectedValue).Append('`');
                }

                sb.AppendLine($"| `{item.assetPath}` | {item.category} | {item.matchedRuleNames} | {diffParts} |");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Optional pre-build compliance guard (disabled by default via EditorPrefs).
    /// </summary>
    public sealed class ImportAuditBuildGuard : IPreprocessBuildWithReport
    {
        private const string EnabledKey = "AssetImportRules.PreBuildEnabled";
        private const string FailKey = "AssetImportRules.PreBuildFailOnNonCompliant";

        /// <inheritdoc/>
        public int callbackOrder => 15;

        /// <summary>
        /// Gets or sets whether the pre-build compliance audit runs before Unity builds (stored in <see cref="EditorPrefs"/>).
        /// </summary>
        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, false);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        /// <summary>
        /// Gets or sets whether non-compliant assets throw a <see cref="BuildFailedException"/> instead of logging a warning.
        /// </summary>
        public static bool FailOnNonCompliant
        {
            get => EditorPrefs.GetBool(FailKey, false);
            set => EditorPrefs.SetBool(FailKey, value);
        }

        /// <summary>
        /// Runs a dry-run audit against the active policy before a player build when <see cref="Enabled"/> is <c>true</c>
        /// and the active policy is not <see cref="ImportEnforcementMode.Disabled"/>.
        /// </summary>
        /// <param name="report">Unity build report for the pending build.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!Enabled) return;
            var policy = AssetComplianceAuditor.GetActivePolicy();
            if (policy == null || policy.enforcementMode == ImportEnforcementMode.Disabled) return;

            var audit = AssetComplianceAuditor.RunDryRunAudit(policy);
            if (audit.nonCompliantAssets > 0)
            {
                string msg = $"[AssetImportRules] Pre-build audit found {audit.nonCompliantAssets} non-compliant asset(s) (Score: {audit.complianceScore}/100).";
                if (FailOnNonCompliant)
                {
                    throw new BuildFailedException(msg);
                }
                Debug.LogWarning(msg);
            }
        }
    }
}
