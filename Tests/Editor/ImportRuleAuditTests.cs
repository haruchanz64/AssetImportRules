using System;
using System.IO;
using AssetImportRules.Editor.Core;
using AssetImportRules.Editor.Pipeline;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetImportRules.Tests
{
    public sealed class ImportRuleAuditTests
    {
        [Test]
        public void TextureRuleAuditAndApply_DetectsDiffsFixesAssetAndExportsReport()
        {
            string tempFolder = $"Assets/Temp_AssetImportRulesTest_{Guid.NewGuid():N}";
            string uiFolder = $"{tempFolder}/UI";
            string texAssetPath = $"{uiFolder}/test_btn.png";

            AssetImportPolicyAsset policy = null;
            try
            {
                Directory.CreateDirectory(uiFolder);
                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                File.WriteAllBytes(texAssetPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                AssetDatabase.ImportAsset(texAssetPath, ImportAssetOptions.ForceSynchronousImport);

                // Deliberately misconfigure importer (Default texture, mipmaps ON, readable ON, maxSize 4096)
                var importer = AssetImporter.GetAtPath(texAssetPath) as TextureImporter;
                Assert.IsNotNull(importer);
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = true;
                importer.isReadable = true;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();

                policy = ScriptableObject.CreateInstance<AssetImportPolicyAsset>();
                policy.enforcementMode = ImportEnforcementMode.AuditOnly;
                policy.ApplyMobile2DAndUiPreset();

                // 1. Dry-run audit detects non-compliance
                var beforeReport = AssetComplianceAuditor.RunDryRunAudit(policy, tempFolder);
                Assert.AreEqual(1, beforeReport.matchedAssets);
                Assert.AreEqual(1, beforeReport.nonCompliantAssets);
                Assert.AreEqual(0, beforeReport.complianceScore);
                Assert.GreaterOrEqual(beforeReport.nonCompliantItems[0].diffs.Count, 3);

                string md = ImportAuditExporter.ToMarkdown(beforeReport);
                string json = ImportAuditExporter.ToJson(beforeReport);
                Assert.That(md, Does.Contain("test_btn.png"));
                Assert.That(json, Does.Contain("complianceScore"));

                // 2. Apply policy & re-import fixes all diffs
                int fixedCount = AssetComplianceAuditor.ApplyPolicyToAssetPaths(policy, new[] { texAssetPath });
                Assert.AreEqual(1, fixedCount);

                var afterReport = AssetComplianceAuditor.RunDryRunAudit(policy, tempFolder);
                Assert.AreEqual(0, afterReport.nonCompliantAssets);
                Assert.AreEqual(100, afterReport.complianceScore);
            }
            finally
            {
                if (policy != null) UnityEngine.Object.DestroyImmediate(policy);
                if (AssetDatabase.IsValidFolder(tempFolder))
                {
                    AssetDatabase.DeleteAsset(tempFolder);
                }
                else if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }
    }
}
