using System.Collections.Generic;
using AssetImportRules.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace AssetImportRules.Editor.Pipeline
{
    public static class ModelRuleProcessor
    {
        public static void Audit(ModelImporter importer, ModelImportRuleConfig rule, List<ImportDiffEntry> diffs)
        {
            if (importer == null || rule == null) return;

            CheckFloat(diffs, rule.ruleName, "globalScale", importer.globalScale, rule.globalScale);
            Check(diffs, rule.ruleName, "useFileScale", importer.useFileScale, rule.useFileScale);
            Check(diffs, rule.ruleName, "meshCompression", importer.meshCompression, rule.meshCompression);
            Check(diffs, rule.ruleName, "isReadable", importer.isReadable, rule.isReadable);
            Check(diffs, rule.ruleName, "optimizeMeshPolygons", importer.optimizeMeshPolygons, rule.optimizeMeshPolygons);
            Check(diffs, rule.ruleName, "optimizeMeshVertices", importer.optimizeMeshVertices, rule.optimizeMeshVertices);
            Check(diffs, rule.ruleName, "importCameras", importer.importCameras, rule.importCameras);
            Check(diffs, rule.ruleName, "importLights", importer.importLights, rule.importLights);
            Check(diffs, rule.ruleName, "importVisibility", importer.importVisibility, rule.importVisibility);
            Check(diffs, rule.ruleName, "importBlendShapes", importer.importBlendShapes, rule.importBlendShapes);
            Check(diffs, rule.ruleName, "animationType", importer.animationType, rule.animationType);
            Check(diffs, rule.ruleName, "importAnimation", importer.importAnimation, rule.importAnimation);
            Check(diffs, rule.ruleName, "materialImportMode", importer.materialImportMode, rule.materialImportMode);
            Check(diffs, rule.ruleName, "generateSecondaryUV", importer.generateSecondaryUV, rule.generateSecondaryUV);
        }

        public static bool Apply(ModelImporter importer, ModelImportRuleConfig rule)
        {
            if (importer == null || rule == null) return false;
            bool changed = false;

            if (rule.globalScale.enabled && !Mathf.Approximately(importer.globalScale, rule.globalScale.value))
            {
                importer.globalScale = rule.globalScale.value;
                changed = true;
            }
            if (rule.useFileScale.enabled && importer.useFileScale != rule.useFileScale.value)
            {
                importer.useFileScale = rule.useFileScale.value;
                changed = true;
            }
            if (rule.meshCompression.enabled && importer.meshCompression != rule.meshCompression.value)
            {
                importer.meshCompression = rule.meshCompression.value;
                changed = true;
            }
            if (rule.isReadable.enabled && importer.isReadable != rule.isReadable.value)
            {
                importer.isReadable = rule.isReadable.value;
                changed = true;
            }
            if (rule.optimizeMeshPolygons.enabled && importer.optimizeMeshPolygons != rule.optimizeMeshPolygons.value)
            {
                importer.optimizeMeshPolygons = rule.optimizeMeshPolygons.value;
                changed = true;
            }
            if (rule.optimizeMeshVertices.enabled && importer.optimizeMeshVertices != rule.optimizeMeshVertices.value)
            {
                importer.optimizeMeshVertices = rule.optimizeMeshVertices.value;
                changed = true;
            }
            if (rule.importCameras.enabled && importer.importCameras != rule.importCameras.value)
            {
                importer.importCameras = rule.importCameras.value;
                changed = true;
            }
            if (rule.importLights.enabled && importer.importLights != rule.importLights.value)
            {
                importer.importLights = rule.importLights.value;
                changed = true;
            }
            if (rule.importVisibility.enabled && importer.importVisibility != rule.importVisibility.value)
            {
                importer.importVisibility = rule.importVisibility.value;
                changed = true;
            }
            if (rule.importBlendShapes.enabled && importer.importBlendShapes != rule.importBlendShapes.value)
            {
                importer.importBlendShapes = rule.importBlendShapes.value;
                changed = true;
            }
            if (rule.animationType.enabled && importer.animationType != rule.animationType.value)
            {
                importer.animationType = rule.animationType.value;
                changed = true;
            }
            if (rule.importAnimation.enabled && importer.importAnimation != rule.importAnimation.value)
            {
                importer.importAnimation = rule.importAnimation.value;
                changed = true;
            }
            if (rule.materialImportMode.enabled && importer.materialImportMode != rule.materialImportMode.value)
            {
                importer.materialImportMode = rule.materialImportMode.value;
                changed = true;
            }
            if (rule.generateSecondaryUV.enabled && importer.generateSecondaryUV != rule.generateSecondaryUV.value)
            {
                importer.generateSecondaryUV = rule.generateSecondaryUV.value;
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
                    currentValue = current.ToString("F3"),
                    expectedValue = expected.value.ToString("F3")
                });
            }
        }
    }
}
