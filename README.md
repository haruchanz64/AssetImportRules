# AssetImportRules

Rule-driven Unity 6 Editor pipeline and UI Toolkit compliance workbench for Sprites, UI textures, general textures, 3D Models, and Audio clips. It enforces team-configured `AssetImportPolicyAsset` rules automatically via `AssetPostprocessor`, audits existing project assets with non-destructive `Current -> Expected` property diffs, and supports custom `IAssetImportRule` extensions discovered via `TypeCache`.

## Features

- **Non-destructive property overrides (`RuleOverride<T>`):** Every importer field in a rule carries an explicit `enabled` flag (`RuleOverride<T>.On(...)` / `RuleOverride<T>.Off(...)`) so rules modify only opted-in settings and preserve artist choices on everything else.
- **Built-in rules for Textures/Sprites/UI, 3D Models, and Audio:**
  - **Textures, Sprites, and UI (`TextureImportRuleConfig` / `TextureRuleProcessor`):** `textureType`, `spriteImportMode`, `spritePixelsPerUnit`, `mipmapEnabled`, `isReadable`, `filterMode`, `wrapMode`, `alphaIsTransparency`, `maxTextureSize`, `textureCompression`, `crunchedCompression`, `npotScale`, and optional per-platform overrides (`PlatformTextureOverrideConfig` for targets such as `Android`, `iPhone`, or `Standalone`).
  - **3D Models (`ModelImportRuleConfig` / `ModelRuleProcessor`):** `globalScale`, `useFileScale`, `meshCompression`, `isReadable`, `optimizeMeshPolygons`, `optimizeMeshVertices`, `importCameras`, `importLights`, `importVisibility`, `importBlendShapes`, `animationType`, `importAnimation`, `materialImportMode`, and `generateSecondaryUV`.
  - **Audio Clips (`AudioImportRuleConfig` / `AudioRuleProcessor`):** `forceToMono`, `loadInBackground`, `ambisonic`, and default sample settings (`loadType`, `compressionFormat`, `quality`, `sampleRateSetting`, and `sampleRateOverride`).
- **Folder prefix and glob path matching (`AssetPathMatcher`):** Target assets by plain folder prefixes, glob patterns (`*`, `**`, `?`), case-insensitive file extension lists, per-rule `excludePatterns`, global ignore patterns (`**/Editor/**`, `**/Gizmos/**`, `**/*_raw.*` by default), and Unity Asset Labels (`ImportRulesIgnore` by default).
- **Starter policy presets (`AssetImportPolicyAsset`):** One-click generators for `Mobile 2D/UI/Audio`, `PC 3D URP`, and `Pixel Art` project defaults.
- **UI Toolkit workbench (`AssetImportRulesWindow`):** Dockable window (`Tools > HaruChanZ64 > Assets > AssetImportRules > Import Policy Workbench`) with three tabs: **Policy & Presets**, **Compliance Audit** (dry-run scan, category filtering, per-asset Ping/Fix, batch **Apply & Reimport All**, Markdown clipboard copy, JSON export), and **Path Rule Tester**.
- **Extensible rule engine (`IAssetImportRule`):** Implement `IAssetImportRule` in any Editor assembly to add studio-specific naming checks or custom importer logic; `UnityEditor.TypeCache` discovers concrete types automatically.
- **Optional pre-build compliance guard (`ImportAuditBuildGuard`):** Implements `IPreprocessBuildWithReport` (`callbackOrder = 15`, disabled by default in `EditorPrefs`) to warn or fail player builds (`BuildFailedException`) when non-compliant assets exist.

## Requirements

- Unity `6000.0` or newer
- Editor-only package (`AssetImportRules.Editor` compiles only for the `Editor` platform; zero runtime footprint)
- Zero required external dependencies

## Installation

### Unity Package Manager (Git URL)

1. Open **Window > Package Manager** in Unity 6 (`6000.0+`).
2. Click **+** (top-left) and choose **Install package from git URL...**.
3. Enter:

```text
https://github.com/haruchanz64/AssetImportRules.git
```

### Local package

Add the package path to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.haruchanz64.asset-import-rules": "file:../Plugins/AssetImportRules"
  }
}
```

## Quick start

1. Open **Tools > HaruChanZ64 > Assets > AssetImportRules > Import Policy Workbench**.
2. In the **Policy & Presets** tab, click **Create Mobile 2D/UI/Audio Policy**, **Create PC 3D URP Policy**, or **Create Pixel Art Policy** (or create a blank policy via **Assets > Create > AssetImportRules > Import Policy** and assign it to **Policy Asset**).
3. Review or edit rules directly in the embedded Inspector, and set `Enforcement Mode` (`AutoApplyOnImport`, `AuditOnly`, or `Disabled`).
4. Switch to **Compliance Audit** and click **Scan Project (Dry Run)** to view the compliance score and exact `Current -> Expected` diffs without modifying any assets.
5. Click **Fix** on individual rows or **Apply & Reimport All** to enforce matching rules in a single `AssetDatabase.StartAssetEditing()` batch.
6. Use the **Path Rule Tester** tab to test sample asset paths against global ignore patterns and built-in rule priorities before rolling out changes.
7. Add the `ImportRulesIgnore` Unity Asset Label (or match a `globalIgnorePatterns` entry such as `**/*_raw.*`) on any hero asset that should be exempt from automatic overrides.

## Core concepts

### Policy resolution and enforcement pipeline

- **Active policy resolution (`AssetComplianceAuditor.GetActivePolicy`):** Reads the selected policy GUID from `EditorPrefs` (`AssetImportRules.ActivePolicyGuid`). If unset or missing, it falls back to the first `AssetImportPolicyAsset` found via `AssetDatabase.FindAssets("t:AssetImportPolicyAsset")`.
- **Automatic import hook (`AssetImportRulesPostprocessor`):** Hooks `OnPreprocessTexture`, `OnPreprocessModel`, and `OnPreprocessAudio`. When the active policy uses `ImportEnforcementMode.AutoApplyOnImport` and the asset is not exempt, all matching built-in rules run in ascending `priority` order, followed by discovered `IAssetImportRule` extensions in ascending `Priority` order. Because rules run in ascending order, higher priority values apply later and win on overlapping properties.
- **Dry-run audit vs. batch apply:** `AssetComplianceAuditor.RunDryRunAudit(policy, rootFolder)` inspects `t:Texture t:Model t:AudioClip` assets without modifying any importer and computes `complianceScore = round(compliantAssets / matchedAssets * 100)` (or `100` when zero assets match). `AssetComplianceAuditor.ApplyPolicyToAssetPaths(policy, paths)` wraps updates in `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` and calls `SaveAndReimport()` only on importers that actually changed.

### Path matching and exemptions

`AssetPathMatcher.Matches` normalizes backslashes to forward slashes and evaluates rules in three stages:

1. **Exemptions and exclusions:** `AssetComplianceAuditor.IsAssetExempt` first checks `policy.globalIgnorePatterns` and `policy.exemptAssetLabel`. Each rule then evaluates its own `excludePatterns`.
2. **Extensions:** If `extensions` is non-empty, the asset extension must match one entry (case-insensitive, with or without leading `.`).
3. **Path pattern:** Plain folder prefixes (no `*` or `?`) match the folder and its descendants; glob patterns translate `**/` (zero or more directories), `**` (any characters), `*` (single segment), and `?` (single non-slash character) into an anchored case-insensitive regex.

## Code examples

### 1. Implement a custom `IAssetImportRule`

Place a concrete class implementing `IAssetImportRule` with a public parameterless constructor inside any Editor assembly. `AssetComplianceAuditor.DiscoverCustomRules()` discovers it automatically through `TypeCache`:

```csharp
using System.Collections.Generic;
using AssetImportRules.Editor.Core;
using UnityEditor;

public sealed class MaxLightmappedModelScaleRule : IAssetImportRule
{
    public string RuleId => "STUDIO_MODEL_UV2";
    public string DisplayName => "Environment Models Generate Lightmap UVs";
    public int Priority => 40;

    public bool Matches(string assetPath, AssetImporter importer)
    {
        return importer is ModelImporter &&
               AssetPathMatcher.Matches(assetPath, "Assets/Environment/**", new[] { ".fbx" });
    }

    public void Audit(string assetPath, AssetImporter importer, List<ImportDiffEntry> diffs)
    {
        var model = (ModelImporter)importer;
        if (!model.generateSecondaryUV)
        {
            diffs.Add(new ImportDiffEntry
            {
                ruleName = DisplayName,
                propertyName = "generateSecondaryUV",
                currentValue = "False",
                expectedValue = "True"
            });
        }
    }

    public bool Apply(string assetPath, AssetImporter importer)
    {
        var model = (ModelImporter)importer;
        if (model.generateSecondaryUV) return false;

        model.generateSecondaryUV = true;
        return true;
    }
}
```

### 2. Run a scripted compliance audit and export reports (CI / batchmode)

Call `AssetComplianceAuditor` and `ImportAuditExporter` from an Editor script or Unity `-executeMethod` CI entrypoint:

```csharp
using System.IO;
using AssetImportRules.Editor.Core;
using AssetImportRules.Editor.Pipeline;
using UnityEditor;
using UnityEngine;

public static class ImportComplianceCi
{
    public static void RunAuditAndExport()
    {
        AssetImportPolicyAsset policy = AssetComplianceAuditor.GetActivePolicy();
        if (policy == null)
        {
            Debug.LogError("No AssetImportPolicyAsset found in project.");
            return;
        }

        ImportComplianceReport report = AssetComplianceAuditor.RunDryRunAudit(policy, "Assets");
        File.WriteAllText("import_compliance.md", ImportAuditExporter.ToMarkdown(report));
        File.WriteAllText("import_compliance.json", ImportAuditExporter.ToJson(report, prettyPrint: true));

        Debug.Log($"Import compliance: {report.complianceScore}/100 ({report.nonCompliantAssets} non-compliant).");
    }
}
```

### 3. Configure a policy asset and pre-build guard from code

```csharp
using AssetImportRules.Editor.Core;
using AssetImportRules.Editor.Pipeline;
using UnityEditor;
using UnityEngine;

public static class PolicyBootstrap
{
    public static void CreateAndActivatePolicy()
    {
        var policy = ScriptableObject.CreateInstance<AssetImportPolicyAsset>();
        policy.enforcementMode = ImportEnforcementMode.AutoApplyOnImport;
        policy.ApplyPc3DUrpPreset();

        AssetDatabase.CreateAsset(policy, "Assets/Settings/AssetImportPolicy_PC.asset");
        AssetDatabase.SaveAssets();

        AssetComplianceAuditor.SetActivePolicy(policy);
        ImportAuditBuildGuard.Enabled = true;
        ImportAuditBuildGuard.FailOnNonCompliant = true;
    }
}
```

## Extension contracts and core API

| Type | Namespace | Purpose | Built-in implementation |
| :--- | :--- | :--- | :--- |
| `IAssetImportRule` | `AssetImportRules.Editor.Core` | Extension contract (`Matches`, `Audit`, `Apply`) auto-discovered via `TypeCache` | `SampleNoSpacesInAssetNameRule` (in sample) |
| `AssetImportPolicyAsset` | `AssetImportRules.Editor.Core` | ScriptableObject storing enforcement mode, exemptions, rule lists, and starter presets | `ApplyMobile2DAndUiPreset`, `ApplyPc3DUrpPreset`, `ApplyPixelArtPreset` |
| `RuleOverride<T>` | `AssetImportRules.Editor.Core` | Opt-in value wrapper (`enabled` + `value`) for non-destructive importer overrides | `RuleOverride<T>.On(...)`, `RuleOverride<T>.Off(...)` |
| `AssetPathMatcher` | `AssetImportRules.Editor.Core` | Pure C# normalizer and folder-prefix/glob/extension/exclusion matcher | `NormalizePath`, `Matches`, `MatchesSinglePattern` |
| `ImportComplianceReport` | `AssetImportRules.Editor.Core` | Serializable audit result containing counts, `complianceScore`, and `AssetAuditItem` diffs | `ComputeScore` |
| `AssetComplianceAuditor` | `AssetImportRules.Editor.Pipeline` | Active policy lookup, custom rule discovery, exemption checks, dry-run audits, and batch apply | `GetActivePolicy`, `RunDryRunAudit`, `ApplyPolicyToAssetPaths` |
| `TextureRuleProcessor` / `ModelRuleProcessor` / `AudioRuleProcessor` | `AssetImportRules.Editor.Pipeline` | Type-specific importer audit and apply processors | `Audit`, `Apply` |
| `AssetImportRulesPostprocessor` | `AssetImportRules.Editor.Pipeline` | `AssetPostprocessor` applying active policy on Texture, Model, and Audio import | `OnPreprocessTexture`, `OnPreprocessModel`, `OnPreprocessAudio` |
| `ImportAuditExporter` | `AssetImportRules.Editor.Pipeline` | Serializes `ImportComplianceReport` to Markdown or JSON | `ToMarkdown`, `ToJson` |
| `ImportAuditBuildGuard` | `AssetImportRules.Editor.Pipeline` | Optional `IPreprocessBuildWithReport` hook to warn or fail builds on violations | `Enabled`, `FailOnNonCompliant`, `OnPreprocessBuild` |
| `AssetImportRulesWindow` | `AssetImportRules.Editor.UI` | UI Toolkit workbench for policy authoring, auditing, and path testing | `ShowWindow` |

## Included samples

Import **Custom Import Rule Sample** (`Samples~/CustomRule`) from the Package Manager **Samples** tab:

- `SampleNoSpacesInAssetNameRule`: audit-only `IAssetImportRule` (`RuleId = "TEAM_NAME_01"`, `Priority = 50`) checking that assets under `Assets/Art/**` do not contain spaces in their filenames and suggesting underscore-separated replacements in audit diffs.

## Testing

The package includes EditMode NUnit tests under `Tests/Editor/` runnable from **Window > General > Test Runner** (EditMode tab):

- `Tests/Editor/AssetPathMatcherTests.cs`: verifies folder prefix matching, glob wildcards (`**`, `*`), case-insensitive extension filtering, exclusion patterns, and starter rule generation in `ApplyMobile2DAndUiPreset()`.
- `Tests/Editor/ImportRuleAuditTests.cs`: creates a temporary misconfigured PNG texture under a temporary `Assets/` folder, verifies dry-run non-compliance detection and Markdown/JSON report export, runs `ApplyPolicyToAssetPaths`, and confirms the post-fix audit reaches a `100` compliance score.

## Troubleshooting and limitations

- **Active policy selection and pre-build guard toggles are stored in `EditorPrefs`:** `AssetComplianceAuditor.SetActivePolicy`, `ImportAuditBuildGuard.Enabled`, and `ImportAuditBuildGuard.FailOnNonCompliant` are per-machine Editor preferences rather than fields on `AssetImportPolicyAsset`. On CI runners or fresh checkouts, `GetActivePolicy()` falls back to the first `AssetImportPolicyAsset` found in the project, and `ImportAuditBuildGuard.Enabled` defaults to `false` unless set by your build script.
- **Custom `IAssetImportRule` classes are not showing up:** Ensure the class is in an Editor assembly, is concrete (not `abstract` or an interface), and has a public parameterless constructor. Instantiation exceptions are logged as warnings with the `[AssetImportRules]` prefix.
- **Dry-run audit scope is limited to Textures, Models, and AudioClips:** `RunDryRunAudit` queries `AssetDatabase.FindAssets("t:Texture t:Model t:AudioClip", ...)`, and `AssetImportRulesPostprocessor` hooks `OnPreprocessTexture`, `OnPreprocessModel`, and `OnPreprocessAudio`. Custom `IAssetImportRule` implementations targeting other asset types will not be visited by the built-in scanner or postprocessor.
- **Overlapping rules with conflicting values keep reporting diffs:** `ApplyRulesToImporter` runs matching rules in ascending priority order (higher priority overwrites lower priority), while `RunDryRunAudit` checks every matching rule independently. If two overlapping rules enable the same property with different target values, add an `excludePatterns` entry to the lower-priority rule or disable the conflicting override.
- **Exempting newly imported assets on first import:** `AssetImportRulesPostprocessor` checks `exemptAssetLabel` via `AssetDatabase.LoadMainAssetAtPath` and `AssetDatabase.GetLabels`. Brand-new files do not carry Unity Asset Labels until after their initial import; use `globalIgnorePatterns` (such as `**/*_raw.*`) to exempt assets before first import.
- **Platform texture `compressionQuality` vs. dry-run diffs:** `TextureRuleProcessor.Apply` updates `compressionQuality` when it differs, whereas `TextureRuleProcessor.Audit` checks `overridden`, `maxTextureSize`, and `format` for platform overrides.
- **`ApplyPixelArtPreset()` only replaces `textureRules`:** Unlike `ApplyMobile2DAndUiPreset()` and `ApplyPc3DUrpPreset()`, calling `ApplyPixelArtPreset()` leaves existing `modelRules` and `audioRules` unchanged.
- **Audio `quality` range and `sampleRateOverride` floor:** `AudioRuleProcessor.Apply` clamps `quality` to `[0, 1]` and `sampleRateOverride` to a minimum of `8000` Hz. Keep configured `quality` values within `[0, 1]` so dry-run audits match applied values.

## License

[MIT](LICENSE.md)
