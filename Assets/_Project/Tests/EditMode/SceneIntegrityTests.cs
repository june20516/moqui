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

        [Test]
        public void BuildSettings_ScreenScenesInFlowOrder()
        {
            var paths = System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
            var expected = System.Array.ConvertAll(SandboxSceneBuilder.BuildOrder, SandboxSceneBuilder.ScenePath);
            Assert.That(paths, Is.EqualTo(expected));
        }
    }
}
