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

        /// <summary>
        /// Loads the active <see cref="AssetImportPolicyAsset"/> stored in <see cref="EditorPrefs"/>,
        /// falling back to the first policy asset found in the project via <see cref="AssetDatabase"/>.
        /// </summary>
        /// <returns>The active policy asset, or <c>null</c> if none exists in the project.</returns>
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

        /// <summary>
        /// Persists the GUID of <paramref name="policy"/> in <see cref="EditorPrefs"/> as the active project policy,
        /// or clears the saved preference when <paramref name="policy"/> is <c>null</c>.
        /// </summary>
        /// <param name="policy">Policy asset to activate, or <c>null</c> to clear the saved selection.</param>
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

        /// <summary>
        /// Discovers and instantiates all concrete <see cref="IAssetImportRule"/> implementations with a public
        /// parameterless constructor via <see cref="TypeCache"/>, sorted by ascending <see cref="IAssetImportRule.Priority"/>.
        /// </summary>
        /// <returns>Priority-ordered list of custom rule instances.</returns>
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

        /// <summary>
        /// Checks whether <paramref name="assetPath"/> is exempted by <paramref name="policy"/> via
        /// <see cref="AssetImportPolicyAsset.globalIgnorePatterns"/> or the configured <see cref="AssetImportPolicyAsset.exemptAssetLabel"/>.
        /// </summary>
        /// <param name="assetPath">Project-relative asset path to inspect.</param>
        /// <param name="policy">Policy defining global ignore patterns and the exempt label.</param>
        /// <returns><c>true</c> if <paramref name="policy"/> is <c>null</c> or the asset is exempt; otherwise <c>false</c>.</returns>
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

        /// <summary>
        /// Returns all enabled texture rules in <paramref name="policy"/> matching <paramref name="assetPath"/>,
        /// sorted by ascending <see cref="TextureImportRuleConfig.priority"/>.
        /// </summary>
        /// <param name="assetPath">Project-relative asset path.</param>
        /// <param name="policy">Policy containing candidate texture rules.</param>
        /// <returns>Priority-ordered list of matching texture rules.</returns>
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

        /// <summary>
        /// Returns all enabled 3D model rules in <paramref name="policy"/> matching <paramref name="assetPath"/>,
        /// sorted by ascending <see cref="ModelImportRuleConfig.priority"/>.
        /// </summary>
        /// <param name="assetPath">Project-relative asset path.</param>
        /// <param name="policy">Policy containing candidate model rules.</param>
        /// <returns>Priority-ordered list of matching model rules.</returns>
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

        /// <summary>
        /// Returns all enabled audio rules in <paramref name="policy"/> matching <paramref name="assetPath"/>,
        /// sorted by ascending <see cref="AudioImportRuleConfig.priority"/>.
        /// </summary>
        /// <param name="assetPath">Project-relative asset path.</param>
        /// <param name="policy">Policy containing candidate audio rules.</param>
        /// <returns>Priority-ordered list of matching audio rules.</returns>
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

        /// <summary>
        /// Scans Textures, 3D Models, and AudioClips under <paramref name="rootFolder"/> against <paramref name="policy"/>
        /// and discovered <see cref="IAssetImportRule"/> implementations without modifying any asset.
        /// </summary>
        /// <param name="policy">Import policy to evaluate. Returns an empty 100-score report when <c>null</c>.</param>
        /// <param name="rootFolder">Project-relative folder scope; defaults to <c>"Assets"</c> when null or whitespace.</param>
        /// <returns>Populated <see cref="ImportComplianceReport"/> with computed score and per-asset diffs.</returns>
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

        /// <summary>
        /// Applies <paramref name="policy"/> and discovered custom rules to each non-exempt path in
        /// <paramref name="assetPaths"/> inside a single <see cref="AssetDatabase.StartAssetEditing"/> batch,
        /// calling <see cref="AssetImporter.SaveAndReimport"/> only on modified importers.
        /// </summary>
        /// <param name="policy">Policy whose rules should be enforced.</param>
        /// <param name="assetPaths">Project-relative asset paths to update.</param>
        /// <returns>Number of assets whose importer settings changed and were re-imported.</returns>
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

        /// <summary>
        /// Applies all matching built-in rules (in ascending priority order) followed by matching
        /// <paramref name="customRules"/> directly to <paramref name="importer"/> without saving or re-importing.
        /// </summary>
        /// <param name="assetPath">Project-relative path of the asset being processed.</param>
        /// <param name="importer">Target Unity <see cref="AssetImporter"/>.</param>
        /// <param name="policy">Policy supplying built-in rules.</param>
        /// <param name="customRules">Optional pre-discovered custom rules sorted by priority.</param>
        /// <returns><c>true</c> if any rule mutated the importer; otherwise <c>false</c>.</returns>
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
