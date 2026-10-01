using System.Collections;
using Moqui.Unity.Presentation;
using Moqui.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    /// <summary>Sandbox_Water 씬이 Play 모드에서 오류 없이 물방울을 만들고 그리는지 확인한다.</summary>
    public class SandboxWaterSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Water.unity";
        private const int WarmupFrames = 30;

        [UnityTest]
        public IEnumerator SandboxWater_Play_SpawnsAndDrawsDrops()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Sandbox scenes are editor-only.");
#endif
            for (int i = 0; i < WarmupFrames; i++)
            {
                yield return null;
            }

            var runner = Object.FindAnyObjectByType<SimulationRunner>();
            var waterView = Object.FindAnyObjectByType<WaterView>();
            Assert.That(runner.IsRunning, Is.True);
            Assert.That(runner.Driver.Simulation.Water.SpawnedCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(waterView.VisibleDrops, Is.EqualTo(runner.Driver.Simulation.Water.Drops.Count));
        }
    }
}
