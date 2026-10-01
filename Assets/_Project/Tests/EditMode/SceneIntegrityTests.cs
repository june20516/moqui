using Moqui.Unity.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>생성한 씬에 누락 스크립트가 없다 (MonoBehaviour는 파일명과 클래스명이 같아야 씬에 붙는다).</summary>
    public class SceneIntegrityTests
    {
        [TestCase(SandboxSceneBuilder.StageScenePath)]
        [TestCase(SandboxSceneBuilder.FlightScenePath)]
        [TestCase(SandboxSceneBuilder.HumanScenePath)]
        [TestCase(SandboxSceneBuilder.WaterScenePath)]
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
    }
}
