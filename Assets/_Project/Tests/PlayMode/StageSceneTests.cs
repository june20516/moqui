using System.Collections;
using System.Linq;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Audio;
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
        public IEnumerator Stage_Play_BuildsRequestedLevel([ValueSource(typeof(CatalogLevels), nameof(CatalogLevels.LevelIds))] string levelId)
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

                AssertAllRenderersUseProjectShaders(levelId);

                // 소리 (spec/10): 스테이지 음악과 레벨 환경음이 켜지고, 음악 볼륨 설정이 바로 반영된다.
                var output = AudioOutput.Instance;
                Assert.That(output, Is.Not.Null, "audio output created");
                Assert.That(Object.FindObjectsByType<AudioListener>().Length, Is.EqualTo(1), "exactly one audio listener (on the output)");
                Assert.That(output.MusicId, Is.EqualTo(AudioIds.BgmStage));
                Assert.That(output.IsLoopActive(AudioIds.AmbienceForLevel(levelId)), Is.True, "ambience");
                Assert.That(output.IsLoopActive(AudioIds.WingLoop), Is.True, "wing loop while hovering");
                float previousMusic = AudioVolumes.Music;
                AudioVolumes.Music = 0.25f;
                yield return null;
                float expected = AudioCatalog.Load().Find(AudioIds.BgmStage).Volume * 0.25f;
                Assert.That(output.MusicVolume, Is.EqualTo(expected).Within(1e-4f), "music volume setting");
                AudioVolumes.Music = previousMusic;

                var hud = Object.FindAnyObjectByType<HudController>();
                var hudView = hud.GetComponent<HudView>();
                Assert.That(hud.LastState, Is.Not.Null, "HUD updates every frame");
                if (bootstrap.Level.Tutorial.Count > 0)
                {
                    Assert.That(bootstrap.Tutorial, Is.Not.Null);
                    Assert.That(bootstrap.Tutorial.CurrentStep, Is.EqualTo(bootstrap.Level.Tutorial[0]));
                    Assert.That(hudView.TutorialText.text, Is.Not.Empty, "first tutorial hint is shown");
                }
                else
                {
                    Assert.That(bootstrap.Tutorial, Is.Null);
                    Assert.That(hudView.TutorialText.text, Is.Empty, "no hint on stages without a tutorial");
                }
            }
            finally
            {
                PlayerPrefs.SetString(TutorialHints.PreferenceKey, previousHints);
                StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
            }
        }

        /// <summary>화풍 통일 (spec/10): 모든 렌더러가 프로젝트 셰이더(Moqui/*)를 쓰고, 셰이더가 지원된다 (마젠타 없음).</summary>
        private static void AssertAllRenderersUseProjectShaders(string levelId)
        {
            var renderers = Object.FindObjectsByType<Renderer>();
            Assert.That(renderers, Is.Not.Empty);
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    string label = $"{levelId}/{renderer.name}";
                    Assert.That(material, Is.Not.Null, label);
                    Assert.That(material.shader.name, Does.StartWith("Moqui/"), label);
                    Assert.That(material.shader.isSupported, Is.True, label);
                }
            }
        }
    }
}
