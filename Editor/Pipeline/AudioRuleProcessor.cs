using System.Collections.Generic;
using AssetImportRules.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AssetImportRules.Editor.Pipeline
{
    public static class AudioRuleProcessor
    {
        public static void Audit(AudioImporter importer, AudioImportRuleConfig rule, List<ImportDiffEntry> diffs)
        {
            if (importer == null || rule == null) return;

            Check(diffs, rule.ruleName, "forceToMono", importer.forceToMono, rule.forceToMono);
            Check(diffs, rule.ruleName, "loadInBackground", importer.loadInBackground, rule.loadInBackground);
            Check(diffs, rule.ruleName, "ambisonic", importer.ambisonic, rule.ambisonic);

            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            Check(diffs, rule.ruleName, "loadType", s.loadType, rule.loadType);
            Check(diffs, rule.ruleName, "compressionFormat", s.compressionFormat, rule.compressionFormat);
            CheckFloat(diffs, rule.ruleName, "quality", s.quality, rule.quality);
            Check(diffs, rule.ruleName, "sampleRateSetting", s.sampleRateSetting, rule.sampleRateSetting);

            if (rule.sampleRateOverride.enabled && s.sampleRateOverride != (uint)Mathf.Max(8000, rule.sampleRateOverride.value))
            {
                diffs.Add(new ImportDiffEntry
                {
                    ruleName = rule.ruleName,
                    propertyName = "sampleRateOverride",
                    currentValue = s.sampleRateOverride.ToString(),
                    expectedValue = Mathf.Max(8000, rule.sampleRateOverride.value).ToString()
                });
            }
        }

        public static bool Apply(AudioImporter importer, AudioImportRuleConfig rule)
        {
            if (importer == null || rule == null) return false;
            bool changed = false;

            if (rule.forceToMono.enabled && importer.forceToMono != rule.forceToMono.value)
            {
                importer.forceToMono = rule.forceToMono.value;
                changed = true;
            }
            if (rule.loadInBackground.enabled && importer.loadInBackground != rule.loadInBackground.value)
            {
                importer.loadInBackground = rule.loadInBackground.value;
                changed = true;
            }
            if (rule.ambisonic.enabled && importer.ambisonic != rule.ambisonic.value)
            {
                importer.ambisonic = rule.ambisonic.value;
                changed = true;
            }

            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            bool sampleChanged = false;

            if (rule.loadType.enabled && s.loadType != rule.loadType.value)
            {
                s.loadType = rule.loadType.value;
                sampleChanged = true;
            }
            if (rule.compressionFormat.enabled && s.compressionFormat != rule.compressionFormat.value)
            {
                s.compressionFormat = rule.compressionFormat.value;
                sampleChanged = true;
            }
            if (rule.quality.enabled && !Mathf.Approximately(s.quality, Mathf.Clamp01(rule.quality.value)))
            {
                s.quality = Mathf.Clamp01(rule.quality.value);
                sampleChanged = true;
            }
            if (rule.sampleRateSetting.enabled && s.sampleRateSetting != rule.sampleRateSetting.value)
            {
                s.sampleRateSetting = rule.sampleRateSetting.value;
                sampleChanged = true;
            }
            if (rule.sampleRateOverride.enabled)
            {
                uint targetRate = (uint)Mathf.Max(8000, rule.sampleRateOverride.value);
                if (s.sampleRateOverride != targetRate)
                {
                    s.sampleRateOverride = targetRate;
                    sampleChanged = true;
                }
            }

            if (sampleChanged)
            {
                importer.defaultSampleSettings = s;
                changed = true;
            }

            return changed;
        }

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
