using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportRules.Editor.Core
{
    /// <summary>
    /// Controls how <see cref="AssetImportRulesPostprocessor"/> and build guards enforce the active import policy.
    /// </summary>
    public enum ImportEnforcementMode
    {
        /// <summary>Automatically applies matching rules during Unity asset import preprocessing.</summary>
        AutoApplyOnImport = 0,
        /// <summary>Skips automatic mutation on import while still allowing dry-run compliance audits and build checks.</summary>
        AuditOnly = 1,
        /// <summary>Disables automatic import enforcement and pre-build audit checks.</summary>
        Disabled = 2
    }

    /// <summary>
    /// Optional per-platform texture override entry applied via <see cref="TextureImporter.SetPlatformTextureSettings"/>.
    /// </summary>
    [Serializable]
    public sealed class PlatformTextureOverrideConfig
    {
        /// <summary>Unity build target platform name (for example <c>"Android"</c>, <c>"iPhone"</c>, or <c>"Standalone"</c>).</summary>
        public string platformName = "Android";
        /// <summary>Whether this platform override is audited and applied.</summary>
        public bool enabled = false;
        /// <summary>Maximum texture dimension in pixels enforced for the target platform.</summary>
        public int maxTextureSize = 1024;
        /// <summary>Target GPU texture compression format for the platform override.</summary>
        public TextureImporterFormat format = TextureImporterFormat.ASTC_6x6;
        /// <summary>Platform texture compression quality in the range [0, 100].</summary>
        [Range(0, 100)] public int compressionQuality = 50;
    }

    /// <summary>
    /// Rule configuration for matching and overriding <see cref="TextureImporter"/> properties on textures, sprites, and UI assets.
    /// </summary>
    [Serializable]
    public sealed class TextureImportRuleConfig
    {
        /// <summary>Human-readable rule label shown in audit diffs and compliance reports.</summary>
        public string ruleName = "UI & Sprites";
        /// <summary>Whether this rule participates in path matching, audits, and import enforcement.</summary>
        public bool enabled = true;
        /// <summary>Execution priority; matching rules run in ascending priority order so higher values win on overlapping properties.</summary>
        public int priority = 10;
        /// <summary>Folder prefix or glob pattern matched against project-relative asset paths.</summary>
        public string pathPattern = "Assets/UI/**";
        /// <summary>Allowed file extensions (case-insensitive, with or without leading dot). Empty matches all extensions.</summary>
        public List<string> extensions = new List<string> { ".png", ".jpg", ".jpeg", ".tga", ".psd" };
        /// <summary>Per-rule exclusion patterns that prevent matching even when <see cref="pathPattern"/> matches.</summary>
        public List<string> excludePatterns = new List<string>();

        /// <summary>Override for <see cref="TextureImporter.textureType"/>.</summary>
        [Header("Importer Overrides")]
        public RuleOverride<TextureImporterType> textureType = RuleOverride<TextureImporterType>.On(TextureImporterType.Sprite);
        /// <summary>Override for <see cref="TextureImporter.spriteImportMode"/>.</summary>
        public RuleOverride<SpriteImportMode> spriteImportMode = RuleOverride<SpriteImportMode>.Off(SpriteImportMode.Single);
        /// <summary>Override for <see cref="TextureImporter.spritePixelsPerUnit"/> in pixels per world unit.</summary>
        public RuleOverride<float> spritePixelsPerUnit = RuleOverride<float>.Off(100f);
        /// <summary>Override for <see cref="TextureImporter.mipmapEnabled"/>.</summary>
        public RuleOverride<bool> mipmapEnabled = RuleOverride<bool>.On(false);
        /// <summary>Override for <see cref="TextureImporter.isReadable"/> (CPU read/write access).</summary>
        public RuleOverride<bool> isReadable = RuleOverride<bool>.On(false);
        /// <summary>Override for <see cref="TextureImporter.filterMode"/>.</summary>
        public RuleOverride<FilterMode> filterMode = RuleOverride<FilterMode>.Off(FilterMode.Bilinear);
        /// <summary>Override for <see cref="TextureImporter.wrapMode"/>.</summary>
        public RuleOverride<TextureWrapMode> wrapMode = RuleOverride<TextureWrapMode>.Off(TextureWrapMode.Clamp);
        /// <summary>Override for <see cref="TextureImporter.alphaIsTransparency"/>.</summary>
        public RuleOverride<bool> alphaIsTransparency = RuleOverride<bool>.On(true);
        /// <summary>Override for <see cref="TextureImporter.maxTextureSize"/> in pixels.</summary>
        public RuleOverride<int> maxTextureSize = RuleOverride<int>.On(1024);
        /// <summary>Override for <see cref="TextureImporter.textureCompression"/>.</summary>
        public RuleOverride<TextureImporterCompression> textureCompression = RuleOverride<TextureImporterCompression>.On(TextureImporterCompression.Compressed);
        /// <summary>Override for <see cref="TextureImporter.crunchedCompression"/>.</summary>
        public RuleOverride<bool> crunchedCompression = RuleOverride<bool>.Off(false);
        /// <summary>Override for <see cref="TextureImporter.npotScale"/>.</summary>
        public RuleOverride<TextureImporterNPOTScale> npotScale = RuleOverride<TextureImporterNPOTScale>.Off(TextureImporterNPOTScale.None);

        /// <summary>Optional platform-specific texture size and format overrides.</summary>
        [Header("Per-Platform Overrides (Optional)")]
        public List<PlatformTextureOverrideConfig> platformOverrides = new List<PlatformTextureOverrideConfig>();
    }

    /// <summary>
    /// Rule configuration for matching and overriding <see cref="ModelImporter"/> properties on 3D model assets.
    /// </summary>
    [Serializable]
    public sealed class ModelImportRuleConfig
    {
        /// <summary>Human-readable rule label shown in audit diffs and compliance reports.</summary>
        public string ruleName = "3D Models";
        /// <summary>Whether this rule participates in path matching, audits, and import enforcement.</summary>
        public bool enabled = true;
        /// <summary>Execution priority; matching rules run in ascending priority order so higher values win on overlapping properties.</summary>
        public int priority = 10;
        /// <summary>Folder prefix or glob pattern matched against project-relative asset paths.</summary>
        public string pathPattern = "Assets/Models/**";
        /// <summary>Allowed model file extensions (case-insensitive). Empty matches all extensions.</summary>
        public List<string> extensions = new List<string> { ".fbx", ".obj", ".dae", ".blend" };
        /// <summary>Per-rule exclusion patterns that prevent matching even when <see cref="pathPattern"/> matches.</summary>
        public List<string> excludePatterns = new List<string>();

        /// <summary>Override for <see cref="ModelImporter.globalScale"/>.</summary>
        [Header("Importer Overrides")]
        public RuleOverride<float> globalScale = RuleOverride<float>.Off(1f);
        /// <summary>Override for <see cref="ModelImporter.useFileScale"/>.</summary>
        public RuleOverride<bool> useFileScale = RuleOverride<bool>.On(true);
        /// <summary>Override for <see cref="ModelImporter.meshCompression"/>.</summary>
        public RuleOverride<ModelImporterMeshCompression> meshCompression = RuleOverride<ModelImporterMeshCompression>.On(ModelImporterMeshCompression.Medium);
        /// <summary>Override for <see cref="ModelImporter.isReadable"/> (CPU mesh read/write access).</summary>
        public RuleOverride<bool> isReadable = RuleOverride<bool>.On(false);
        /// <summary>Override for <see cref="ModelImporter.optimizeMeshPolygons"/>.</summary>
        public RuleOverride<bool> optimizeMeshPolygons = RuleOverride<bool>.On(true);
        /// <summary>Override for <see cref="ModelImporter.optimizeMeshVertices"/>.</summary>
        public RuleOverride<bool> optimizeMeshVertices = RuleOverride<bool>.On(true);
        /// <summary>Override for <see cref="ModelImporter.importCameras"/>.</summary>
        public RuleOverride<bool> importCameras = RuleOverride<bool>.On(false);
        /// <summary>Override for <see cref="ModelImporter.importLights"/>.</summary>
        public RuleOverride<bool> importLights = RuleOverride<bool>.On(false);
        /// <summary>Override for <see cref="ModelImporter.importVisibility"/>.</summary>
        public RuleOverride<bool> importVisibility = RuleOverride<bool>.Off(false);
        /// <summary>Override for <see cref="ModelImporter.importBlendShapes"/>.</summary>
        public RuleOverride<bool> importBlendShapes = RuleOverride<bool>.Off(false);
        /// <summary>Override for <see cref="ModelImporter.animationType"/>.</summary>
        public RuleOverride<ModelImporterAnimationType> animationType = RuleOverride<ModelImporterAnimationType>.Off(ModelImporterAnimationType.None);
        /// <summary>Override for <see cref="ModelImporter.importAnimation"/>.</summary>
        public RuleOverride<bool> importAnimation = RuleOverride<bool>.Off(false);
        /// <summary>Override for <see cref="ModelImporter.materialImportMode"/>.</summary>
        public RuleOverride<ModelImporterMaterialImportMode> materialImportMode = RuleOverride<ModelImporterMaterialImportMode>.Off(ModelImporterMaterialImportMode.ImportStandard);
        /// <summary>Override for <see cref="ModelImporter.generateSecondaryUV"/> (lightmap UV generation).</summary>
        public RuleOverride<bool> generateSecondaryUV = RuleOverride<bool>.Off(false);
    }

    /// <summary>
    /// Rule configuration for matching and overriding <see cref="AudioImporter"/> and default sample settings on audio clips.
    /// </summary>
    [Serializable]
    public sealed class AudioImportRuleConfig
    {
        /// <summary>Human-readable rule label shown in audit diffs and compliance reports.</summary>
        public string ruleName = "Audio Clips";
        /// <summary>Whether this rule participates in path matching, audits, and import enforcement.</summary>
        public bool enabled = true;
        /// <summary>Execution priority; matching rules run in ascending priority order so higher values win on overlapping properties.</summary>
        public int priority = 10;
        /// <summary>Folder prefix or glob pattern matched against project-relative asset paths.</summary>
        public string pathPattern = "Assets/Audio/**";
        /// <summary>Allowed audio file extensions (case-insensitive). Empty matches all extensions.</summary>
        public List<string> extensions = new List<string> { ".wav", ".ogg", ".mp3", ".aiff" };
        /// <summary>Per-rule exclusion patterns that prevent matching even when <see cref="pathPattern"/> matches.</summary>
        public List<string> excludePatterns = new List<string>();

        /// <summary>Override for <see cref="AudioImporter.forceToMono"/>.</summary>
        [Header("Importer Overrides")]
        public RuleOverride<bool> forceToMono = RuleOverride<bool>.Off(true);
        /// <summary>Override for <see cref="AudioImporter.loadInBackground"/>.</summary>
        public RuleOverride<bool> loadInBackground = RuleOverride<bool>.Off(false);
        /// <summary>Override for <see cref="AudioImporter.ambisonic"/>.</summary>
        public RuleOverride<bool> ambisonic = RuleOverride<bool>.On(false);
        /// <summary>Override for <see cref="AudioImporterSampleSettings.loadType"/>.</summary>
        public RuleOverride<AudioClipLoadType> loadType = RuleOverride<AudioClipLoadType>.On(AudioClipLoadType.DecompressOnLoad);
        /// <summary>Override for <see cref="AudioImporterSampleSettings.compressionFormat"/>.</summary>
        public RuleOverride<AudioCompressionFormat> compressionFormat = RuleOverride<AudioCompressionFormat>.On(AudioCompressionFormat.Vorbis);
        /// <summary>Override for <see cref="AudioImporterSampleSettings.quality"/>, clamped to [0, 1] when applied.</summary>
        public RuleOverride<float> quality = RuleOverride<float>.On(0.7f);
        /// <summary>Override for <see cref="AudioImporterSampleSettings.sampleRateSetting"/>.</summary>
        public RuleOverride<AudioSampleRateSetting> sampleRateSetting = RuleOverride<AudioSampleRateSetting>.On(AudioSampleRateSetting.OptimizeSampleRate);
        /// <summary>Override for <see cref="AudioImporterSampleSettings.sampleRateOverride"/> in Hz, clamped to a minimum of 8000 Hz.</summary>
        public RuleOverride<int> sampleRateOverride = RuleOverride<int>.Off(44100);
    }

    /// <summary>
    /// Project import policy asset configuring non-destructive rules for Sprites, UI, Textures,
    /// 3D Models, and Audio clips.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetImportPolicy", menuName = "AssetImportRules/Import Policy", order = 100)]
    public sealed class AssetImportPolicyAsset : ScriptableObject
    {
        /// <summary>Controls whether rules auto-apply on import, run in audit-only mode, or stay disabled.</summary>
        public ImportEnforcementMode enforcementMode = ImportEnforcementMode.AutoApplyOnImport;
        /// <summary>Unity Asset Label that exempts an individual asset from all built-in and custom import rules.</summary>
        public string exemptAssetLabel = "ImportRulesIgnore";
        /// <summary>Glob or folder patterns excluded globally before any rule matching runs.</summary>
        public List<string> globalIgnorePatterns = new List<string>
        {
            "**/Editor/**",
            "**/Gizmos/**",
            "**/*_raw.*"
        };

        /// <summary>Configured rules evaluated against <see cref="TextureImporter"/> assets.</summary>
        public List<TextureImportRuleConfig> textureRules = new List<TextureImportRuleConfig>();
        /// <summary>Configured rules evaluated against <see cref="ModelImporter"/> assets.</summary>
        public List<ModelImportRuleConfig> modelRules = new List<ModelImportRuleConfig>();
        /// <summary>Configured rules evaluated against <see cref="AudioImporter"/> assets.</summary>
        public List<AudioImportRuleConfig> audioRules = new List<AudioImportRuleConfig>();

        /// <summary>
        /// Populates <see cref="textureRules"/>, <see cref="modelRules"/>, and <see cref="audioRules"/>
        /// with a mobile-oriented starter preset (ASTC 6x6 Android/iPhone UI overrides, 3D prop defaults,
        /// ADPCM SFX, and streaming Vorbis music). Replaces existing rule lists.
        /// </summary>
        public void ApplyMobile2DAndUiPreset()
        {
            textureRules = new List<TextureImportRuleConfig>
            {
                new TextureImportRuleConfig
                {
                    ruleName = "UI Sprites (Mobile)",
                    priority = 20,
                    pathPattern = "Assets/**/UI/**",
                    textureType = RuleOverride<TextureImporterType>.On(TextureImporterType.Sprite),
                    mipmapEnabled = RuleOverride<bool>.On(false),
                    isReadable = RuleOverride<bool>.On(false),
                    alphaIsTransparency = RuleOverride<bool>.On(true),
                    maxTextureSize = RuleOverride<int>.On(1024),
                    textureCompression = RuleOverride<TextureImporterCompression>.On(TextureImporterCompression.Compressed),
                    platformOverrides = new List<PlatformTextureOverrideConfig>
                    {
                        new PlatformTextureOverrideConfig { platformName = "Android", enabled = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_6x6 },
                        new PlatformTextureOverrideConfig { platformName = "iPhone", enabled = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_6x6 }
                    }
                },
                new TextureImportRuleConfig
                {
                    ruleName = "Gameplay Sprites",
                    priority = 10,
                    pathPattern = "Assets/**/Sprites/**",
                    textureType = RuleOverride<TextureImporterType>.On(TextureImporterType.Sprite),
                    mipmapEnabled = RuleOverride<bool>.On(false),
                    isReadable = RuleOverride<bool>.On(false),
                    maxTextureSize = RuleOverride<int>.On(2048),
                    textureCompression = RuleOverride<TextureImporterCompression>.On(TextureImporterCompression.Compressed)
                }
            };

            modelRules = new List<ModelImportRuleConfig>
            {
                new ModelImportRuleConfig
                {
                    ruleName = "3D Props & Environment",
                    priority = 10,
                    pathPattern = "Assets/**/Models/**",
                    meshCompression = RuleOverride<ModelImporterMeshCompression>.On(ModelImporterMeshCompression.Medium),
                    isReadable = RuleOverride<bool>.On(false),
                    importCameras = RuleOverride<bool>.On(false),
                    importLights = RuleOverride<bool>.On(false),
                    optimizeMeshPolygons = RuleOverride<bool>.On(true),
                    optimizeMeshVertices = RuleOverride<bool>.On(true)
                }
            };

            audioRules = new List<AudioImportRuleConfig>
            {
                new AudioImportRuleConfig
                {
                    ruleName = "SFX Clips (Low Latency)",
                    priority = 20,
                    pathPattern = "Assets/**/Audio/SFX/**",
                    forceToMono = RuleOverride<bool>.On(true),
                    loadType = RuleOverride<AudioClipLoadType>.On(AudioClipLoadType.DecompressOnLoad),
                    compressionFormat = RuleOverride<AudioCompressionFormat>.On(AudioCompressionFormat.ADPCM),
                    sampleRateSetting = RuleOverride<AudioSampleRateSetting>.On(AudioSampleRateSetting.OptimizeSampleRate)
                },
                new AudioImportRuleConfig
                {
                    ruleName = "Music & Ambience (Streaming)",
                    priority = 20,
                    pathPattern = "Assets/**/Audio/Music/**",
                    forceToMono = RuleOverride<bool>.On(false),
                    loadInBackground = RuleOverride<bool>.On(true),
                    loadType = RuleOverride<AudioClipLoadType>.On(AudioClipLoadType.Streaming),
                    compressionFormat = RuleOverride<AudioCompressionFormat>.On(AudioCompressionFormat.Vorbis),
                    quality = RuleOverride<float>.On(0.7f)
                }
            };
        }

        /// <summary>
        /// Replaces <see cref="textureRules"/> with a pixel-art sprite rule (Point filtering, Clamp wrap,
        /// 16 PPU, Uncompressed, no mipmaps, and no NPOT scaling). Leaves model and audio rules unchanged.
        /// </summary>
        public void ApplyPixelArtPreset()
        {
            textureRules = new List<TextureImportRuleConfig>
            {
                new TextureImportRuleConfig
                {
                    ruleName = "Pixel Art Sprites",
                    priority = 20,
                    pathPattern = "Assets/**/Sprites/**",
                    textureType = RuleOverride<TextureImporterType>.On(TextureImporterType.Sprite),
                    spritePixelsPerUnit = RuleOverride<float>.On(16f),
                    mipmapEnabled = RuleOverride<bool>.On(false),
                    isReadable = RuleOverride<bool>.On(false),
                    filterMode = RuleOverride<FilterMode>.On(FilterMode.Point),
                    wrapMode = RuleOverride<TextureWrapMode>.On(TextureWrapMode.Clamp),
                    textureCompression = RuleOverride<TextureImporterCompression>.On(TextureImporterCompression.Uncompressed),
                    npotScale = RuleOverride<TextureImporterNPOTScale>.On(TextureImporterNPOTScale.None)
                }
            };
        }

        /// <summary>
        /// Populates rules for PC 3D URP projects by starting from <see cref="ApplyMobile2DAndUiPreset"/>,
        /// raising UI sprite max size to 2048 px without mobile platform overrides, and adding a high-priority
        /// <c>*_Normal.*</c> normal map rule.
        /// </summary>
        public void ApplyPc3DUrpPreset()
        {
            ApplyMobile2DAndUiPreset();
            if (textureRules.Count > 0)
            {
                textureRules[0].ruleName = "UI Sprites (PC)";
                textureRules[0].maxTextureSize = RuleOverride<int>.On(2048);
                textureRules[0].platformOverrides.Clear();
            }

            textureRules.Add(new TextureImportRuleConfig
            {
                ruleName = "3D Normal Maps",
                priority = 30,
                pathPattern = "Assets/**/*_Normal.*",
                textureType = RuleOverride<TextureImporterType>.On(TextureImporterType.NormalMap),
                mipmapEnabled = RuleOverride<bool>.On(true),
                isReadable = RuleOverride<bool>.On(false),
                maxTextureSize = RuleOverride<int>.On(2048),
                textureCompression = RuleOverride<TextureImporterCompression>.On(TextureImporterCompression.CompressedHQ)
            });
        }
    }
}
