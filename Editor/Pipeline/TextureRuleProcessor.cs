using System.Collections.Generic;
using AssetImportRules.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AssetImportRules.Editor.Pipeline
{
    /// <summary>
    /// Audits and applies <see cref="TextureImportRuleConfig"/> overrides, including optional per-platform settings,
    /// against Unity <see cref="TextureImporter"/> instances.
    /// </summary>
    public static class TextureRuleProcessor
    {
        /// <summary>
        /// Compares enabled overrides and platform settings on <paramref name="rule"/> against
        /// <paramref name="importer"/> without mutating the importer.
        /// </summary>
        /// <param name="importer">Target texture importer to inspect.</param>
        /// <param name="rule">Texture rule defining expected overrides.</param>
        /// <param name="diffs">Destination list receiving any mismatched property entries.</param>
        public static void Audit(TextureImporter importer, TextureImportRuleConfig rule, List<ImportDiffEntry> diffs)
        {
            if (importer == null || rule == null) return;

            Check(diffs, rule.ruleName, "textureType", importer.textureType, rule.textureType);
            Check(diffs, rule.ruleName, "spriteImportMode", importer.spriteImportMode, rule.spriteImportMode);
            CheckFloat(diffs, rule.ruleName, "spritePixelsPerUnit", importer.spritePixelsPerUnit, rule.spritePixelsPerUnit);
            Check(diffs, rule.ruleName, "mipmapEnabled", importer.mipmapEnabled, rule.mipmapEnabled);
            Check(diffs, rule.ruleName, "isReadable", importer.isReadable, rule.isReadable);
            Check(diffs, rule.ruleName, "filterMode", importer.filterMode, rule.filterMode);
            Check(diffs, rule.ruleName, "wrapMode", importer.wrapMode, rule.wrapMode);
            Check(diffs, rule.ruleName, "alphaIsTransparency", importer.alphaIsTransparency, rule.alphaIsTransparency);
            Check(diffs, rule.ruleName, "maxTextureSize", importer.maxTextureSize, rule.maxTextureSize);
            Check(diffs, rule.ruleName, "textureCompression", importer.textureCompression, rule.textureCompression);
            Check(diffs, rule.ruleName, "crunchedCompression", importer.crunchedCompression, rule.crunchedCompression);
            Check(diffs, rule.ruleName, "npotScale", importer.npotScale, rule.npotScale);

            if (rule.platformOverrides != null)
            {
                for (int i = 0; i < rule.platformOverrides.Count; i++)
                {
                    var p = rule.platformOverrides[i];
                    if (p == null || !p.enabled || string.IsNullOrWhiteSpace(p.platformName)) continue;

                    var current = importer.GetPlatformTextureSettings(p.platformName);
                    if (!current.overridden || current.maxTextureSize != p.maxTextureSize || current.format != p.format)
                    {
                        diffs.Add(new ImportDiffEntry
                        {
                            ruleName = rule.ruleName,
                            propertyName = $"Platform[{p.platformName}]",
                            currentValue = current.overridden ? $"{current.maxTextureSize}px / {current.format}" : "Not Overridden",
                            expectedValue = $"{p.maxTextureSize}px / {p.format}"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Writes all enabled, differing overrides and platform texture settings from <paramref name="rule"/>
        /// onto <paramref name="importer"/>.
        /// </summary>
        /// <param name="importer">Target texture importer to modify.</param>
        /// <param name="rule">Texture rule supplying overrides to apply.</param>
        /// <returns><c>true</c> if any importer or platform setting changed; otherwise <c>false</c>.</returns>
        public static bool Apply(TextureImporter importer, TextureImportRuleConfig rule)
        {
            if (importer == null || rule == null) return false;
            bool changed = false;

            if (rule.textureType.enabled && importer.textureType != rule.textureType.value)
            {
                importer.textureType = rule.textureType.value;
                changed = true;
            }
            if (rule.spriteImportMode.enabled && importer.spriteImportMode != rule.spriteImportMode.value)
            {
                importer.spriteImportMode = rule.spriteImportMode.value;
                changed = true;
            }
            if (rule.spritePixelsPerUnit.enabled && !Mathf.Approximately(importer.spritePixelsPerUnit, rule.spritePixelsPerUnit.value))
            {
                importer.spritePixelsPerUnit = rule.spritePixelsPerUnit.value;
                changed = true;
            }
            if (rule.mipmapEnabled.enabled && importer.mipmapEnabled != rule.mipmapEnabled.value)
            {
                importer.mipmapEnabled = rule.mipmapEnabled.value;
                changed = true;
            }
            if (rule.isReadable.enabled && importer.isReadable != rule.isReadable.value)
            {
                importer.isReadable = rule.isReadable.value;
                changed = true;
            }
            if (rule.filterMode.enabled && importer.filterMode != rule.filterMode.value)
            {
                importer.filterMode = rule.filterMode.value;
                changed = true;
            }
            if (rule.wrapMode.enabled && importer.wrapMode != rule.wrapMode.value)
            {
                importer.wrapMode = rule.wrapMode.value;
                changed = true;
            }
            if (rule.alphaIsTransparency.enabled && importer.alphaIsTransparency != rule.alphaIsTransparency.value)
            {
                importer.alphaIsTransparency = rule.alphaIsTransparency.value;
                changed = true;
            }
            if (rule.maxTextureSize.enabled && importer.maxTextureSize != rule.maxTextureSize.value)
            {
                importer.maxTextureSize = rule.maxTextureSize.value;
                changed = true;
            }
            if (rule.textureCompression.enabled && importer.textureCompression != rule.textureCompression.value)
            {
                importer.textureCompression = rule.textureCompression.value;
                changed = true;
            }
            if (rule.crunchedCompression.enabled && importer.crunchedCompression != rule.crunchedCompression.value)
            {
                importer.crunchedCompression = rule.crunchedCompression.value;
                changed = true;
            }
            if (rule.npotScale.enabled && importer.npotScale != rule.npotScale.value)
            {
                importer.npotScale = rule.npotScale.value;
                changed = true;
            }

            if (rule.platformOverrides != null)
            {
                for (int i = 0; i < rule.platformOverrides.Count; i++)
                {
                    var p = rule.platformOverrides[i];
                    if (p == null || !p.enabled || string.IsNullOrWhiteSpace(p.platformName)) continue;

                    var settings = importer.GetPlatformTextureSettings(p.platformName);
                    if (!settings.overridden ||
                        settings.maxTextureSize != p.maxTextureSize ||
                        settings.format != p.format ||
                        settings.compressionQuality != p.compressionQuality)
                    {
                        settings.name = p.platformName;
                        settings.overridden = true;
                        settings.maxTextureSize = p.maxTextureSize;
                        settings.format = p.format;
                        settings.compressionQuality = p.compressionQuality;
                        importer.SetPlatformTextureSettings(settings);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// Records a diff entry when <paramref name="expected"/> is enabled and differs from <paramref name="current"/>.
        /// </summary>
        private static void Check<T>(List<ImportDiffEntry> diffs, string ruleName, string prop, T current, RuleOverride<T> expected)
        {
            if (!expected.enabled) return;
            if (!EqualityComparer<T>.Default.Equals(current, expected.value))
            {
                diffs.Add(new ImportDiffEntry
                {
                    ruleName = ruleName,
                    propertyName = prop,
                    currentValue = current?.ToString(),
                    expectedValue = expected.value?.ToString()
                });
            }
        }

        /// <summary>
        /// Records a diff entry when <paramref name="expected"/> is enabled and differs from <paramref name="current"/>
        /// using <see cref="Mathf.Approximately"/>.
        /// </summary>
        private static void CheckFloat(List<ImportDiffEntry> diffs, string ruleName, string prop, float current, RuleOverride<float> expected)
        {
            if (!expected.enabled) return;
            if (!Mathf.Approximately(current, expected.value))
            {
                diffs.Add(new ImportDiffEntry
                {
                    ruleName = ruleName,
                    propertyName = prop,
                    currentValue = current.ToString("F2"),
                    expectedValue = expected.value.ToString("F2")
                });
            }
        }
    }
}
