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
        /// <summary>Unity preprocess hook invoked before importing texture and sprite assets.</summary>
        private void OnPreprocessTexture() => TryApply(assetImporter);
        /// <summary>Unity preprocess hook invoked before importing 3D model assets.</summary>
        private void OnPreprocessModel() => TryApply(assetImporter);
        /// <summary>Unity preprocess hook invoked before importing audio clip assets.</summary>
        private void OnPreprocessAudio() => TryApply(assetImporter);

        /// <summary>
        /// Applies the active policy and custom rules to <paramref name="importer"/> when enforcement mode
        /// is <see cref="ImportEnforcementMode.AutoApplyOnImport"/> and the asset is not exempt.
        /// </summary>
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
