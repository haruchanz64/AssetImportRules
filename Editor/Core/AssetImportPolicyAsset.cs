using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportRules.Editor.Core
{
    public enum ImportEnforcementMode
    {
        AutoApplyOnImport = 0,
        AuditOnly = 1,
        Disabled = 2
    }

    [Serializable]
    public sealed class PlatformTextureOverrideConfig
    {
        public string platformName = "Android";
        public bool enabled = false;
        public int maxTextureSize = 1024;
        public TextureImporterFormat format = TextureImporterFormat.ASTC_6x6;
        [Range(0, 100)] public int compressionQuality = 50;
    }

    [Serializable]
    public sealed class TextureImportRuleConfig
    {
        public string ruleName = "UI & Sprites";
        public bool enabled = true;
        public int priority = 10;
        public string pathPattern = "Assets/UI/**";
        public List<string> extensions = new List<string> { ".png", ".jpg", ".jpeg", ".tga", ".psd" };
        public List<string> excludePatterns = new List<string>();

        [Header("Importer Overrides")]
        public RuleOverride<TextureImporterType> textureType = RuleOverride<TextureImporterType>.On(TextureImporterType.Sprite);
        public RuleOverride<SpriteImportMode> spriteImportMode = RuleOverride<SpriteImportMode>.Off(SpriteImportMode.Single);
        public RuleOverride<float> spritePixelsPerUnit = RuleOverride<float>.Off(100f);
        public RuleOverride<bool> mipmapEnabled = RuleOverride<bool>.On(false);
        public RuleOverride<bool> isReadable = RuleOverride<bool>.On(false);
        public RuleOverride<FilterMode> filterMode = RuleOverride<FilterMode>.Off(FilterMode.Bilinear);
        public RuleOverride<TextureWrapMode> wrapMode = RuleOverride<TextureWrapMode>.Off(TextureWrapMode.Clamp);
        public RuleOverride<bool> alphaIsTransparency = RuleOverride<bool>.On(true);
        public RuleOverride<int> maxTextureSize = RuleOverride<int>.On(1024);
        public RuleOverride<TextureImporterCompression> textureCompression = RuleOverride<TextureImporterCompression>.On(TextureImporterCompression.Compressed);
        public RuleOverride<bool> crunchedCompression = RuleOverride<bool>.Off(false);
        public RuleOverride<TextureImporterNPOTScale> npotScale = RuleOverride<TextureImporterNPOTScale>.Off(TextureImporterNPOTScale.None);

        [Header("Per-Platform Overrides (Optional)")]
        public List<PlatformTextureOverrideConfig> platformOverrides = new List<PlatformTextureOverrideConfig>();
    }

    [Serializable]
    public sealed class ModelImportRuleConfig
    {
        public string ruleName = "3D Models";
        public bool enabled = true;
        public int priority = 10;
        public string pathPattern = "Assets/Models/**";
        public List<string> extensions = new List<string> { ".fbx", ".obj", ".dae", ".blend" };
        public List<string> excludePatterns = new List<string>();

        [Header("Importer Overrides")]
        public RuleOverride<float> globalScale = RuleOverride<float>.Off(1f);
        public RuleOverride<bool> useFileScale = RuleOverride<bool>.On(true);
        public RuleOverride<ModelImporterMeshCompression> meshCompression = RuleOverride<ModelImporterMeshCompression>.On(ModelImporterMeshCompression.Medium);
        public RuleOverride<bool> isReadable = RuleOverride<bool>.On(false);
        public RuleOverride<bool> optimizeMeshPolygons = RuleOverride<bool>.On(true);
        public RuleOverride<bool> optimizeMeshVertices = RuleOverride<bool>.On(true);
        public RuleOverride<bool> importCameras = RuleOverride<bool>.On(false);
        public RuleOverride<bool> importLights = RuleOverride<bool>.On(false);
        public RuleOverride<bool> importVisibility = RuleOverride<bool>.Off(false);
        public RuleOverride<bool> importBlendShapes = RuleOverride<bool>.Off(false);
        public RuleOverride<ModelImporterAnimationType> animationType = RuleOverride<ModelImporterAnimationType>.Off(ModelImporterAnimationType.None);
        public RuleOverride<bool> importAnimation = RuleOverride<bool>.Off(false);
        public RuleOverride<ModelImporterMaterialImportMode> materialImportMode = RuleOverride<ModelImporterMaterialImportMode>.Off(ModelImporterMaterialImportMode.ImportStandard);
        public RuleOverride<bool> generateSecondaryUV = RuleOverride<bool>.Off(false);
    }

    [Serializable]
    public sealed class AudioImportRuleConfig
    {
        public string ruleName = "Audio Clips";
        public bool enabled = true;
        public int priority = 10;
        public string pathPattern = "Assets/Audio/**";
        public List<string> extensions = new List<string> { ".wav", ".ogg", ".mp3", ".aiff" };
        public List<string> excludePatterns = new List<string>();

        [Header("Importer Overrides")]
        public RuleOverride<bool> forceToMono = RuleOverride<bool>.Off(true);
        public RuleOverride<bool> loadInBackground = RuleOverride<bool>.Off(false);
        public RuleOverride<bool> ambisonic = RuleOverride<bool>.On(false);
        public RuleOverride<AudioClipLoadType> loadType = RuleOverride<AudioClipLoadType>.On(AudioClipLoadType.DecompressOnLoad);
        public RuleOverride<AudioCompressionFormat> compressionFormat = RuleOverride<AudioCompressionFormat>.On(AudioCompressionFormat.Vorbis);
        public RuleOverride<float> quality = RuleOverride<float>.On(0.7f);
        public RuleOverride<AudioSampleRateSetting> sampleRateSetting = RuleOverride<AudioSampleRateSetting>.On(AudioSampleRateSetting.OptimizeSampleRate);
        public RuleOverride<int> sampleRateOverride = RuleOverride<int>.Off(44100);
    }

    /// <summary>
    /// Project import policy asset configuring non-destructive rules for Sprites, UI, Textures,
    /// 3D Models, and Audio clips.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetImportPolicy", menuName = "AssetImportRules/Import Policy", order = 100)]
    public sealed class AssetImportPolicyAsset : ScriptableObject
    {
        public ImportEnforcementMode enforcementMode = ImportEnforcementMode.AutoApplyOnImport;
        public string exemptAssetLabel = "ImportRulesIgnore";
        public List<string> globalIgnorePatterns = new List<string>
        {
            "**/Editor/**",
            "**/Gizmos/**",
            "**/*_raw.*"
        };

        public List<TextureImportRuleConfig> textureRules = new List<TextureImportRuleConfig>();
        public List<ModelImportRuleConfig> modelRules = new List<ModelImportRuleConfig>();
        public List<AudioImportRuleConfig> audioRules = new List<AudioImportRuleConfig>();

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
