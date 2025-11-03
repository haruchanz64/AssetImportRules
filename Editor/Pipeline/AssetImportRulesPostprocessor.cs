using AssetImportRules.Editor.Core;
using UnityEditor;

namespace AssetImportRules.Editor.Pipeline
{
    /// <summary>
    /// Automatically enforces the active AssetImportPolicyAsset during Texture, Model, and Audio imports
    /// when EnforcementMode is set to AutoApplyOnImport.
    /// </summary>
    public sealed class AssetImportRulesPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture() => TryApply(assetImporter);
        private void OnPreprocessModel() => TryApply(assetImporter);
        private void OnPreprocessAudio() => TryApply(assetImporter);

        private void TryApply(AssetImporter importer)
        {
            if (importer == null) return;
            var policy = AssetComplianceAuditor.GetActivePolicy();
            if (policy == null || policy.enforcementMode != ImportEnforcementMode.AutoApplyOnImport)
            {
                return;
            }

            if (AssetComplianceAuditor.IsAssetExempt(assetPath, policy))
            {
                return;
            }

            var customRules = AssetComplianceAuditor.DiscoverCustomRules();
            AssetComplianceAuditor.ApplyRulesToImporter(assetPath, importer, policy, customRules);
        }
    }
}
