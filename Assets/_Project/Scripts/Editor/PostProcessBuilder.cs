using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Moqui.Unity.Editor
{
    /// <summary>
    /// 밤 실내 후처리 (spec/10): 약한 Bloom(스탠드·TV 빛 번짐) + Color Grading(톤 매핑, 대비·채도, 그림자 남보라·하이라이트 따뜻하게).
    /// Shadow Zone 비네트는 `ShadowVignette`가 따로 맡는다. 수치는 이 에셋 값이 정본(표현 전용).
    /// </summary>
    public static class PostProcessBuilder
    {
        public const string ProfilePath = "Assets/_Project/Rendering/PostProcess_Night.asset";
        public const string VolumeName = "PostProcess";

        /// <summary>"약한" Bloom의 상한 (테스트 기준).</summary>
        public const float MaxBloomIntensity = 0.5f;

        private const float BloomIntensity = 0.35f;
        private const float BloomThreshold = 0.95f;
        private const float BloomScatter = 0.65f;
        private const float Contrast = 12f;
        private const float Saturation = 8f;
        private static readonly Vector4 ShadowsTint = new Vector4(0.92f, 0.9f, 1.08f, 0f);
        private static readonly Vector4 HighlightsTint = new Vector4(1.06f, 1.0f, 0.94f, 0f);

        /// <summary>현재 씬에 전역 후처리 Volume을 만든다.</summary>
        public static Volume AddVolume()
        {
            var volume = new GameObject(VolumeName).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = LoadOrCreateProfile();
            return volume;
        }

        public static VolumeProfile LoadOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath));
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.intensity.Override(BloomIntensity);
            bloom.threshold.Override(BloomThreshold);
            bloom.scatter.Override(BloomScatter);

            var tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var colorAdjustments = GetOrAdd<ColorAdjustments>(profile);
            colorAdjustments.contrast.Override(Contrast);
            colorAdjustments.saturation.Override(Saturation);

            var tones = GetOrAdd<ShadowsMidtonesHighlights>(profile);
            tones.shadows.Override(ShadowsTint);
            tones.highlights.Override(HighlightsTint);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>프로필 안의 효과 컴포넌트는 서브 에셋으로 저장해야 다시 열 때 남는다.</summary>
        private static T GetOrAdd<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            if (profile.TryGet(out T existing))
            {
                return existing;
            }

            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
