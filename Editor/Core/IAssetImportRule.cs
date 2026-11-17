using System;
using System.Collections.Generic;
using UnityEditor;

namespace AssetImportRules.Editor.Core
{
    /// <summary>
    /// High-level asset importer category assigned to each audited item in an <see cref="ImportComplianceReport"/>.
    /// </summary>
    public enum AssetRuleCategory
    {
        /// <summary>Asset imported through <see cref="TextureImporter"/> (textures, sprites, and UI images).</summary>
        TextureOrSprite = 0,
        /// <summary>Asset imported through <see cref="ModelImporter"/>.</summary>
        Model = 1,
        /// <summary>Asset imported through <see cref="AudioImporter"/>.</summary>
        Audio = 2,
        /// <summary>Asset matched only by a custom <see cref="IAssetImportRule"/> implementation.</summary>
        Custom = 3
    }

    /// <summary>
    /// Single property mismatch recorded during a dry-run compliance audit.
    /// </summary>
    [Serializable]
    public sealed class ImportDiffEntry
    {
        /// <summary>Display name of the rule that flagged the mismatch.</summary>
        public string ruleName;
        /// <summary>Importer property name or custom check identifier.</summary>
        public string propertyName;
        /// <summary>Formatted value currently stored on the asset importer.</summary>
        public string currentValue;
        /// <summary>Formatted value expected by the matching rule.</summary>
        public string expectedValue;
    }

    /// <summary>
    /// Non-compliant asset summary grouping all property diffs discovered for one asset path.
    /// </summary>
    [Serializable]
    public sealed class AssetAuditItem
    {
        /// <summary>Project-relative path of the non-compliant asset.</summary>
        public string assetPath;
        /// <summary>Importer category for filtering in reports and the workbench UI.</summary>
        public AssetRuleCategory category;
        /// <summary>Comma-separated display names of all rules that matched this asset.</summary>
        public string matchedRuleNames;
        /// <summary>Property diffs recorded across all matched rules for this asset.</summary>
        public List<ImportDiffEntry> diffs = new List<ImportDiffEntry>();
    }

    /// <summary>
    /// Serializable result produced by <see cref="AssetImportRules.Editor.Pipeline.AssetComplianceAuditor.RunDryRunAudit"/>.
    /// </summary>
    [Serializable]
    public sealed class ImportComplianceReport
    {
        /// <summary>UTC timestamp in ISO-8601 round-trip (<c>"O"</c>) format when the audit ran.</summary>
        public string generatedUtc;
        /// <summary>Name of the evaluated <see cref="AssetImportPolicyAsset"/>, or <c>"(None)"</c> when missing.</summary>
        public string policyName;
        /// <summary>Root folder scanned by the audit (defaults to <c>"Assets"</c>).</summary>
        public string rootFolder;
        /// <summary>Total unique Texture, Model, and AudioClip assets discovered under <see cref="rootFolder"/>.</summary>
        public int totalScannedAssets;
        /// <summary>Number of non-exempt assets matched by at least one built-in or custom rule.</summary>
        public int matchedAssets;
        /// <summary>Number of matched assets with zero recorded diffs.</summary>
        public int compliantAssets;
        /// <summary>Number of matched assets with one or more recorded diffs.</summary>
        public int nonCompliantAssets;
        /// <summary>Number of scanned assets skipped due to global ignore patterns or the exempt asset label.</summary>
        public int exemptedAssets;
        /// <summary>Rounded compliance percentage in [0, 100]; defaults to 100 when <see cref="matchedAssets"/> is 0.</summary>
        public int complianceScore = 100;
        /// <summary>Detailed diff records for each non-compliant asset.</summary>
        public List<AssetAuditItem> nonCompliantItems = new List<AssetAuditItem>();

        /// <summary>
        /// Recomputes <see cref="nonCompliantAssets"/>, <see cref="compliantAssets"/>, and
        /// <see cref="complianceScore"/> from <see cref="matchedAssets"/> and <see cref="nonCompliantItems"/>.
        /// </summary>
        public void ComputeScore()
        {
            nonCompliantAssets = nonCompliantItems.Count;
            compliantAssets = Math.Max(0, matchedAssets - nonCompliantAssets);
            complianceScore = matchedAssets > 0
                ? (int)Math.Round((compliantAssets / (double)matchedAssets) * 100d)
                : 100;
        }
    }

    /// <summary>
    /// Implement in any Editor assembly to add custom studio import rules (naming checks,
    /// shader remapping, custom importers). Auto-discovered via TypeCache; concrete implementations
    /// must expose a public parameterless constructor.
    /// </summary>
    public interface IAssetImportRule
    {
        /// <summary>Stable unique identifier for the rule (for example <c>"TEAM_NAME_01"</c>).</summary>
        string RuleId { get; }
        /// <summary>Human-readable rule name displayed in audit reports and the workbench UI.</summary>
        string DisplayName { get; }
        /// <summary>Execution priority; custom rules run in ascending priority order after built-in rules.</summary>
        int Priority { get; }

        /// <summary>
        /// Determines whether this rule applies to the specified asset and importer.
        /// </summary>
        /// <param name="assetPath">Project-relative asset path.</param>
        /// <param name="importer">Active Unity <see cref="AssetImporter"/> instance for the asset.</param>
        /// <returns><c>true</c> if <see cref="Audit"/> and <see cref="Apply"/> should run for this asset.</returns>
        bool Matches(string assetPath, AssetImporter importer);

        /// <summary>
        /// Inspects <paramref name="importer"/> without mutating it and appends any violations to <paramref name="diffs"/>.
        /// </summary>
        /// <param name="assetPath">Project-relative asset path.</param>
        /// <param name="importer">Active Unity <see cref="AssetImporter"/> instance to inspect.</param>
        /// <param name="diffs">Target diff list populated with any non-compliant findings.</param>
        void Audit(string assetPath, AssetImporter importer, List<ImportDiffEntry> diffs);

        /// <summary>
        /// Applies rule changes directly to <paramref name="importer"/> during import or batch re-import.
        /// </summary>
        /// <param name="assetPath">Project-relative asset path.</param>
        /// <param name="importer">Active Unity <see cref="AssetImporter"/> instance to modify.</param>
        /// <returns><c>true</c> if any importer property was modified and requires re-import; otherwise <c>false</c>.</returns>
        bool Apply(string assetPath, AssetImporter importer);
    }
}
