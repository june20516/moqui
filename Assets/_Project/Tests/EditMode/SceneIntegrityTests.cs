using Moqui.Unity.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Moqui.Unity.Tests
{
    /// <summary>생성한 씬에 누락 스크립트가 없다 (MonoBehaviour는 파일명과 클래스명이 같아야 씬에 붙는다).</summary>
    public class SceneIntegrityTests
    {
        [TestCase(SandboxSceneBuilder.StageScenePath)]
        [TestCase(SandboxSceneBuilder.FlightScenePath)]
        [TestCase(SandboxSceneBuilder.HumanScenePath)]
        [TestCase(SandboxSceneBuilder.WaterScenePath)]
        [TestCase("Assets/_Project/Scenes/Boot.unity")]
        [TestCase("Assets/_Project/Scenes/Title.unity")]
        [TestCase("Assets/_Project/Scenes/StageSelect.unity")]
        [TestCase("Assets/_Project/Scenes/Ending.unity")]
        public void Scene_HasNoMissingScripts(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int missing = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                }
            }

            Assert.That(missing, Is.EqualTo(0), scenePath);
        }

        [TestCase(SandboxSceneBuilder.StageScenePath)]
        [TestCase(SandboxSceneBuilder.FlightScenePath)]
        public void MainCamera_RendersPostProcessing_ForShadowVignette(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Assert.That(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing, Is.True, scenePath);
        }

        /// <summary>후처리 (spec/10): 약한 Bloom과 Color Grading이 켜진 전역 Volume.</summary>
        [TestCase(SandboxSceneBuilder.StageScenePath)]
        [TestCase(SandboxSceneBuilder.FlightScenePath)]
        public void Scene_HasNightPostProcess_WeakBloomAndColorGrading(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var volume = GameObject.Find(PostProcessBuilder.VolumeName).GetComponent<UnityEngine.Rendering.Volume>();
            Assert.That(volume.isGlobal, Is.True);
            var profile = volume.sharedProfile;

            Assert.That(profile.TryGet(out Bloom bloom), Is.True, "bloom");
            Assert.That(bloom.active && bloom.intensity.overrideState, Is.True);
            Assert.That(bloom.intensity.value, Is.GreaterThan(0f).And.AtMost(PostProcessBuilder.MaxBloomIntensity), "weak bloom");
            Assert.That(profile.TryGet(out Tonemapping tonemapping) && tonemapping.active, Is.True, "tonemapping");
            Assert.That(profile.TryGet(out ColorAdjustments colorAdjustments) && colorAdjustments.active, Is.True, "color adjustments");
            Assert.That(profile.TryGet(out ShadowsMidtonesHighlights tones) && tones.active, Is.True, "shadow/highlight tint");
        }

        /// <summary>프로젝트 셰이더가 모두 컴파일된다 (오류 셰이더는 마젠타로 그려진다, spec/10).</summary>
        [Test]
        public void ProjectShaders_CompileWithoutErrors()
        {
            string[] guids = AssetDatabase.FindAssets("t:Shader", new[] { "Assets/_Project/Shaders" });
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False, path);
            }
        }

        [Test]
        public void BuildSettings_ScreenScenesInFlowOrder()
        {
            var paths = System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
            var expected = System.Array.ConvertAll(SandboxSceneBuilder.BuildOrder, SandboxSceneBuilder.ScenePath);
            Assert.That(paths, Is.EqualTo(expected));
        }
    }
}
