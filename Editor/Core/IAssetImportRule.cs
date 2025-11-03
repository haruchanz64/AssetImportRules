using System;
using System.Collections.Generic;
using UnityEditor;

namespace AssetImportRules.Editor.Core
{
    public enum AssetRuleCategory
    {
        TextureOrSprite = 0,
        Model = 1,
        Audio = 2,
        Custom = 3
    }

    [Serializable]
    public sealed class ImportDiffEntry
    {
        public string ruleName;
        public string propertyName;
        public string currentValue;
        public string expectedValue;
    }

    [Serializable]
    public sealed class AssetAuditItem
    {
        public string assetPath;
        public AssetRuleCategory category;
        public string matchedRuleNames;
        public List<ImportDiffEntry> diffs = new List<ImportDiffEntry>();
    }

    [Serializable]
    public sealed class ImportComplianceReport
    {
        public string generatedUtc;
        public string policyName;
        public string rootFolder;
        public int totalScannedAssets;
        public int matchedAssets;
        public int compliantAssets;
        public int nonCompliantAssets;
        public int exemptedAssets;
        public int complianceScore = 100;
        public List<AssetAuditItem> nonCompliantItems = new List<AssetAuditItem>();

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
    /// shader remapping, custom importers). Auto-discovered via TypeCache.
    /// </summary>
    public interface IAssetImportRule
    {
        string RuleId { get; }
        string DisplayName { get; }
        int Priority { get; }

        bool Matches(string assetPath, AssetImporter importer);
        void Audit(string assetPath, AssetImporter importer, List<ImportDiffEntry> diffs);
        bool Apply(string assetPath, AssetImporter importer);
    }
}
