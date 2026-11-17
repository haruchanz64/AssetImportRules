using System.Collections.Generic;
using System.IO;
using AssetImportRules.Editor.Core;
using UnityEditor;

namespace AssetImportRules.Samples
{
    /// <summary>
    /// Example custom IAssetImportRule verifying that imported assets under Assets/Art/
    /// do not contain spaces in their filenames. Auto-discovered via UnityEditor.TypeCache.
    /// </summary>
    public sealed class SampleNoSpacesInAssetNameRule : IAssetImportRule
    {
        /// <inheritdoc/>
        public string RuleId => "TEAM_NAME_01";
        /// <inheritdoc/>
        public string DisplayName => "No Spaces in Art Filenames";
        /// <inheritdoc/>
        public int Priority => 50;

        /// <inheritdoc/>
        public bool Matches(string assetPath, AssetImporter importer)
        {
            return AssetPathMatcher.Matches(assetPath, "Assets/Art/**");
        }

        /// <inheritdoc/>
        public void Audit(string assetPath, AssetImporter importer, List<ImportDiffEntry> diffs)
        {
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (fileName.Contains(" "))
            {
                diffs.Add(new ImportDiffEntry
                {
                    ruleName = DisplayName,
                    propertyName = "fileName",
                    currentValue = fileName,
                    expectedValue = fileName.Replace(' ', '_')
                });
            }
        }

        /// <inheritdoc/>
        public bool Apply(string assetPath, AssetImporter importer)
        {
            // Naming checks are audit-only; rename explicitly via AssetDatabase.RenameAsset if desired.
            return false;
        }
    }
}
