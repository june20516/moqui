using System.Collections;
using System.Linq;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Simulation;
using Moqui.Unity.UI.Hud;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    /// <summary>Stage 씬이 요청한 레벨 데이터로 시뮬레이션·화이트박스·인간을 구성하는지 확인한다.</summary>
    public class StageSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Stage.unity";
        private const int WarmupFrames = 30;

        [UnityTest]
        public IEnumerator Stage_Play_BuildsRequestedLevel([Values("stage01", "stage02")] string levelId)
        {
            StageBootstrap.RequestedLevelId = levelId;
            string previousHints = PlayerPrefs.GetString(TutorialHints.PreferenceKey, "1");
            PlayerPrefs.SetString(TutorialHints.PreferenceKey, "1");
            try
            {
                yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
                for (int i = 0; i < WarmupFrames; i++)
                {
                    yield return null;
                }

                var bootstrap = Object.FindAnyObjectByType<StageBootstrap>();
                var runner = Object.FindAnyObjectByType<SimulationRunner>();
                var humanView = Object.FindAnyObjectByType<HumanView>();
                Assert.That(bootstrap.Level.Id, Is.EqualTo(levelId));
                Assert.That(runner.IsRunning, Is.True);
                Assert.That(runner.Driver.Simulation.Tick, Is.GreaterThan(0), "simulation advances");
                Assert.That(humanView.IsBuilt, Is.True);

                Transform levelRoot = bootstrap.transform.Find($"Level_{levelId}");
                Assert.That(levelRoot, Is.Not.Null);
                Assert.That(levelRoot.childCount, Is.EqualTo(bootstrap.Level.AllShapes().Count()));

                var hud = Object.FindAnyObjectByType<HudController>();
                var hudView = hud.GetComponent<HudView>();
                Assert.That(hud.LastState, Is.Not.Null, "HUD updates every frame");
                Assert.That(bootstrap.Tutorial, Is.Not.Null);
                Assert.That(bootstrap.Tutorial.CurrentStep, Is.EqualTo(bootstrap.Level.Tutorial[0]));
                Assert.That(hudView.TutorialText.text, Is.Not.Empty, "first tutorial hint is shown");
            }
            finally
            {
                PlayerPrefs.SetString(TutorialHints.PreferenceKey, previousHints);
                StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
            }
        }
    }
}
