# AssetImportRules (Unity 6)

Rule-driven Unity 6 Editor pipeline and UI Toolkit compliance workbench for Sprites, UI textures, general textures, 3D Models, and Audio clips. It applies team-configured import rules on import via `AssetPostprocessor` and audits existing project assets with non-destructive dry-run property diffs.

## Features

- **Non-destructive property overrides (`RuleOverride<T>`):** Every importer field in a rule has an explicit enable flag so rules modify only opted-in settings and preserve artist choices elsewhere.
- **Built-in rules for Sprites, UI, Textures, 3D Models, and Audio:**
  - **Textures, Sprites, and UI:** `textureType`, `spriteImportMode`, `spritePixelsPerUnit`, `mipmapEnabled`, `isReadable`, `filterMode`, `wrapMode`, `alphaIsTransparency`, `maxTextureSize`, `textureCompression`, `crunchedCompression`, `npotScale`, and per-platform overrides (`Android`, `iPhone`, `Standalone`).
  - **3D Models:** `globalScale`, `useFileScale`, `meshCompression`, `isReadable`, `optimizeMeshPolygons`, `optimizeMeshVertices`, `importCameras`, `importLights`, `importVisibility`, `importBlendShapes`, `animationType`, `importAnimation`, `materialImportMode`, and `generateSecondaryUV`.
  - **Audio Clips:** `forceToMono`, `loadInBackground`, `ambisonic`, `loadType` (`DecompressOnLoad`, `CompressedInMemory`, `Streaming`), `compressionFormat` (`PCM`, `ADPCM`, `Vorbis`), `quality`, `sampleRateSetting`, and `sampleRateOverride`.
- **Flexible path matching (`AssetPathMatcher`):** Target assets by folder prefix, glob patterns (`Assets/**/UI/**`, `Assets/**/*_Normal.*`), file extensions, global ignore patterns, and per-asset exemption labels (`ImportRulesIgnore`).
- **UI Toolkit workbench (`Tools > AssetImportRules > Import Policy Workbench`):** Create starter policies (`Mobile 2D/UI/Audio`, `PC 3D URP`, `Pixel Art`), run dry-run compliance scans with exact `Current -> Expected` diffs, apply batch re-imports, test path patterns live, and export Markdown or JSON reports.
- **Extensible rule engine (`IAssetImportRule`):** Implement `IAssetImportRule` in any Editor assembly to add studio-specific checks; `TypeCache` discovers custom rules automatically.
- **Optional pre-build guard:** Enable `ImportAuditBuildGuard` in the workbench to warn or fail builds when non-compliant assets exist.

## Installation

1. Open **Window > Package Manager** in Unity 6 (`6000.0+`).
2. Click **+** (top-left) > **Install package from git URL...**
3. Enter:
   ```text
   https://github.com/haruchanz64/AssetImportRules.git
   ```

## Quick start

1. Open **Tools > AssetImportRules > Import Policy Workbench**.
2. Click **Create Mobile 2D/UI/Audio Policy** (or create a custom policy via **Assets > Create > AssetImportRules > Import Policy**).
3. Switch to **Compliance Audit** and click **Scan Project (Dry Run)** to inspect non-compliant assets without modifying them.
4. Click **Fix** on an individual asset or **Apply & Reimport All** to enforce the policy.
5. Add the `ImportRulesIgnore` Unity Asset Label to any hero asset that should be exempt from automatic overrides.

## License

[MIT](LICENSE.md)
