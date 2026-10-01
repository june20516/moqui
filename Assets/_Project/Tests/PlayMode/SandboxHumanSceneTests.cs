using System.Collections;
using Moqui.Unity.Presentation;
using Moqui.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    /// <summary>Sandbox_Human 씬이 Play 모드에서 오류 없이 인간을 구성하고 그리는지 확인한다.</summary>
    public class SandboxHumanSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_Human.unity";
        private const int WarmupFrames = 30;

        [UnityTest]
        public IEnumerator SandboxHuman_Play_BuildsHumanViewFromSimulation()
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
            var humanView = Object.FindAnyObjectByType<HumanView>();
            Assert.That(runner.Driver.Simulation.Human, Is.Not.Null);
            Assert.That(humanView.IsBuilt, Is.True);

            var human = runner.Driver.Simulation.Human;
            Transform head = humanView.transform.Find(human.HeadShape.Id);
            Assert.That(head, Is.Not.Null, "head capsule drawn");
            Assert.That(Vector3.Distance(head.position, human.HeadCenter.ToUnity()), Is.LessThan(0.01f));

            // 머리 회전 (spec/10): 얼굴 앞이 Core 머리 방향과 같다.
            Assert.That(Vector3.Angle(humanView.Face.forward, human.HeadForward.ToUnity()), Is.LessThan(1f), "face follows head direction");
            Assert.That(humanView.Face.Find("EyeL"), Is.Not.Null);
        }
    }
}
