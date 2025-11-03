using System.Collections.Generic;
using AssetImportRules.Editor.Core;
using NUnit.Framework;

namespace AssetImportRules.Tests
{
    public sealed class AssetPathMatcherTests
    {
        [Test]
        public void PathMatcher_MatchesFolderPrefixGlobsExtensionsAndExclusions()
        {
            var pngs = new List<string> { ".png", ".jpg" };
            var excludes = new List<string> { "**/*_raw.*", "**/Editor/**" };

            Assert.IsTrue(AssetPathMatcher.Matches("Assets/UI/Icons/play.png", "Assets/UI", pngs, excludes));
            Assert.IsTrue(AssetPathMatcher.Matches("Assets/Art/UI/HUD/bar.PNG", "Assets/**/UI/**", pngs, excludes));
            Assert.IsFalse(AssetPathMatcher.Matches("Assets/UI/Icons/play_raw.png", "Assets/UI", pngs, excludes));
            Assert.IsFalse(AssetPathMatcher.Matches("Assets/UI/Editor/icon.png", "Assets/UI", pngs, excludes));
            Assert.IsFalse(AssetPathMatcher.Matches("Assets/UI/Icons/play.fbx", "Assets/UI", pngs, excludes));
            Assert.IsTrue(AssetPathMatcher.Matches("Assets/Art/Hero_Normal.tga", "Assets/**/*_Normal.*"));
        }

        [Test]
        public void PolicyPresets_ConfigureTextureModelAndAudioRules()
        {
            var policy = UnityEngine.ScriptableObject.CreateInstance<AssetImportPolicyAsset>();
            try
            {
                policy.ApplyMobile2DAndUiPreset();
                Assert.GreaterOrEqual(policy.textureRules.Count, 2);
                Assert.GreaterOrEqual(policy.modelRules.Count, 1);
                Assert.GreaterOrEqual(policy.audioRules.Count, 2);

                Assert.IsTrue(AssetPathMatcher.Matches(
                    "Assets/Game/Audio/SFX/jump.wav",
                    policy.audioRules[0].pathPattern,
                    policy.audioRules[0].extensions));

                Assert.IsTrue(AssetPathMatcher.Matches(
                    "Assets/Game/Audio/Music/stage1.ogg",
                    policy.audioRules[1].pathPattern,
                    policy.audioRules[1].extensions));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(policy);
            }
        }
    }
}
