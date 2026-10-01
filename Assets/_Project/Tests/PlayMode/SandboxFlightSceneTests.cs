using System.Collections;
using Moqui.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    /// <summary>Sandbox_Flight 씬이 Play 모드에서 오류 없이 시뮬레이션과 카메라를 구동하는지 확인한다.</summary>
    public class SandboxFlightSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Flight.unity";
        private const int WarmupFrames = 30;

        [UnityTest]
        public IEnumerator SandboxFlight_Play_RunsSimulationAndPlacesCameraNearPlayer()
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

            var runner = Object.FindFirstObjectByType<SimulationRunner>();
            Assert.That(runner, Is.Not.Null);
            Assert.That(runner.IsRunning, Is.True);
            Assert.That(runner.Driver.Simulation.Tick, Is.GreaterThan(0), "simulation advanced");

            var player = GameObject.Find("Player");
            float cameraDistance = Vector3.Distance(Camera.main.transform.position, player.transform.position);
            Assert.That(cameraDistance, Is.InRange(1f, 10f), "third-person camera follows the player");
        }
    }
}
