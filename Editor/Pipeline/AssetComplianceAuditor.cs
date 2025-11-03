using System;
using System.Collections.Generic;
using AssetImportRules.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AssetImportRules.Editor.Pipeline
{
    /// <summary>
    /// Resolves the active AssetImportPolicyAsset, discovers custom IAssetImportRule extensions,
    /// performs dry-run compliance audits, and executes safe batch re-imports.
    /// </summary>
    public static class AssetComplianceAuditor
    {
        private const string ActivePolicyGuidKey = "AssetImportRules.ActivePolicyGuid";

        public static AssetImportPolicyAsset GetActivePolicy()
        {
            string guid = EditorPrefs.GetString(ActivePolicyGuidKey, string.Empty);
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var loaded = AssetDatabase.LoadAssetAtPath<AssetImportPolicyAsset>(path);
                    if (loaded != null) return loaded;
                }
            }

            string[] allGuids = AssetDatabase.FindAssets("t:AssetImportPolicyAsset");
            if (allGuids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<AssetImportPolicyAsset>(AssetDatabase.GUIDToAssetPath(allGuids[0]));
            }

            return null;
        }

        public static void SetActivePolicy(AssetImportPolicyAsset policy)
        {
            if (policy == null)
            {
                EditorPrefs.DeleteKey(ActivePolicyGuidKey);
                return;
            }

            string path = AssetDatabase.GetAssetPath(policy);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                EditorPrefs.SetString(ActivePolicyGuidKey, guid);
            }
        }

        public static List<IAssetImportRule> DiscoverCustomRules()
        {
            var rules = new List<IAssetImportRule>();
            var types = TypeCache.GetTypesDerivedFrom<IAssetImportRule>();
            for (int i = 0; i < types.Count; i++)
            {
                Type t = types[i];
                if (t.IsAbstract || t.IsInterface || t.GetConstructor(Type.EmptyTypes) == null) continue;
                try
                {
                    if (Activator.CreateInstance(t) is IAssetImportRule rule)
                    {
                        rules.Add(rule);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AssetImportRules] Failed to instantiate custom rule '{t.FullName}': {ex.Message}");
                }
            }

            rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            return rules;
        }

        public static bool IsAssetExempt(string assetPath, AssetImportPolicyAsset policy)
        {
            if (policy == null) return true;
            string norm = AssetPathMatcher.NormalizePath(assetPath);

            if (policy.globalIgnorePatterns != null)
            {
                for (int i = 0; i < policy.globalIgnorePatterns.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(policy.globalIgnorePatterns[i]) &&
                        AssetPathMatcher.MatchesSinglePattern(norm, policy.globalIgnorePatterns[i]))
                    {
                        return true;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(policy.exemptAssetLabel))
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(norm);
                if (obj != null)
                {
                    string[] labels = AssetDatabase.GetLabels(obj);
                    for (int i = 0; i < labels.Length; i++)
                    {
                        if (string.Equals(labels[i], policy.exemptAssetLabel, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static List<TextureImportRuleConfig> MatchTextureRules(string assetPath, AssetImportPolicyAsset policy)
        {
            var matched = new List<TextureImportRuleConfig>();
            if (policy == null || policy.textureRules == null) return matched;

            for (int i = 0; i < policy.textureRules.Count; i++)
            {
                var r = policy.textureRules[i];
                if (r != null && r.enabled && AssetPathMatcher.Matches(assetPath, r.pathPattern, r.extensions, r.excludePatterns))
                {
                    matched.Add(r);
                }
            }

            matched.Sort((a, b) => a.priority.CompareTo(b.priority));
            return matched;
        }

        public static List<ModelImportRuleConfig> MatchModelRules(string assetPath, AssetImportPolicyAsset policy)
        {
            var matched = new List<ModelImportRuleConfig>();
            if (policy == null || policy.modelRules == null) return matched;

            for (int i = 0; i < policy.modelRules.Count; i++)
            {
                var r = policy.modelRules[i];
                if (r != null && r.enabled && AssetPathMatcher.Matches(assetPath, r.pathPattern, r.extensions, r.excludePatterns))
                {
                    matched.Add(r);
                }
            }

            matched.Sort((a, b) => a.priority.CompareTo(b.priority));
            return matched;
        }

        public static List<AudioImportRuleConfig> MatchAudioRules(string assetPath, AssetImportPolicyAsset policy)
        {
            var matched = new List<AudioImportRuleConfig>();
            if (policy == null || policy.audioRules == null) return matched;

            for (int i = 0; i < policy.audioRules.Count; i++)
            {
                var r = policy.audioRules[i];
                if (r != null && r.enabled && AssetPathMatcher.Matches(assetPath, r.pathPattern, r.extensions, r.excludePatterns))
                {
                    matched.Add(r);
                }
            }

            matched.Sort((a, b) => a.priority.CompareTo(b.priority));
            return matched;
        }

        public static ImportComplianceReport RunDryRunAudit(AssetImportPolicyAsset policy, string rootFolder = "Assets")
        {
            var report = new ImportComplianceReport
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                policyName = policy != null ? policy.name : "(None)",
                rootFolder = string.IsNullOrWhiteSpace(rootFolder) ? "Assets" : rootFolder
            };

            if (policy == null) return report;

            var customRules = DiscoverCustomRules();
            string[] searchFolders = { report.rootFolder };
            string[] guids = AssetDatabase.FindAssets("t:Texture t:Model t:AudioClip", searchFolders);
            var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || !visitedPaths.Add(path)) continue;

                report.totalScannedAssets++;

                if (IsAssetExempt(path, policy))
                {
                    report.exemptedAssets++;
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                var diffs = new List<ImportDiffEntry>();
                var ruleNames = new List<string>();
                AssetRuleCategory category = AssetRuleCategory.Custom;
                bool anyMatched = false;

                if (importer is TextureImporter texImp)
                {
                    category = AssetRuleCategory.TextureOrSprite;
                    var matched = MatchTextureRules(path, policy);
                    for (int m = 0; m < matched.Count; m++)
                    {
                        anyMatched = true;
                        ruleNames.Add(matched[m].ruleName);
                        TextureRuleProcessor.Audit(texImp, matched[m], diffs);
                    }
                }
                else if (importer is ModelImporter modImp)
                {
                    category = AssetRuleCategory.Model;
                    var matched = MatchModelRules(path, policy);
                    for (int m = 0; m < matched.Count; m++)
                    {
                        anyMatched = true;
                        ruleNames.Add(matched[m].ruleName);
                        ModelRuleProcessor.Audit(modImp, matched[m], diffs);
                    }
                }
                else if (importer is AudioImporter audImp)
                {
                    category = AssetRuleCategory.Audio;
                    var matched = MatchAudioRules(path, policy);
                    for (int m = 0; m < matched.Count; m++)
                    {
                        anyMatched = true;
                        ruleNames.Add(matched[m].ruleName);
                        AudioRuleProcessor.Audit(audImp, matched[m], diffs);
                    }
                }

                for (int c = 0; c < customRules.Count; c++)
                {
                    if (customRules[c].Matches(path, importer))
                    {
                        anyMatched = true;
                        ruleNames.Add(customRules[c].DisplayName);
                        customRules[c].Audit(path, importer, diffs);
                    }
                }

                if (!anyMatched) continue;
                report.matchedAssets++;

                if (diffs.Count > 0)
                {
                    report.nonCompliantItems.Add(new AssetAuditItem
                    {
                        assetPath = path,
                        category = category,
                        matchedRuleNames = string.Join(", ", ruleNames),
                        diffs = diffs
                    });
                }
            }

            report.ComputeScore();
            return report;
        }

        public static int ApplyPolicyToAssetPaths(AssetImportPolicyAsset policy, IEnumerable<string> assetPaths)
        {
            if (policy == null || assetPaths == null) return 0;

            var customRules = DiscoverCustomRules();
            int updatedCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in assetPaths)
                {
                    if (string.IsNullOrEmpty(path) || IsAssetExempt(path, policy)) continue;
                    var importer = AssetImporter.GetAtPath(path);
                    if (importer == null) continue;

                    bool modified = ApplyRulesToImporter(path, importer, policy, customRules);
                    if (modified)
                    {
                        importer.SaveAndReimport();
                        updatedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return updatedCount;
        }

        internal static bool ApplyRulesToImporter(
            string assetPath,
            AssetImporter importer,
            AssetImportPolicyAsset policy,
            IReadOnlyList<IAssetImportRule> customRules)
        {
            bool changed = false;

            if (importer is TextureImporter texImp)
            {
                var matched = MatchTextureRules(assetPath, policy);
                for (int i = 0; i < matched.Count; i++)
                {
                    if (TextureRuleProcessor.Apply(texImp, matched[i])) changed = true;
                }
            }
            else if (importer is ModelImporter modImp)
            {
                var matched = MatchModelRules(assetPath, policy);
                for (int i = 0; i < matched.Count; i++)
                {
                    if (ModelRuleProcessor.Apply(modImp, matched[i])) changed = true;
                }
            }
            else if (importer is AudioImporter audImp)
            {
                var matched = MatchAudioRules(assetPath, policy);
                for (int i = 0; i < matched.Count; i++)
                {
                    if (AudioRuleProcessor.Apply(audImp, matched[i])) changed = true;
                }
            }

            if (customRules != null)
            {
                for (int i = 0; i < customRules.Count; i++)
                {
                    if (customRules[i].Matches(assetPath, importer) && customRules[i].Apply(assetPath, importer))
                    {
                        changed = true;
                    }
                }
            }

            return changed;
        }
    }
}
