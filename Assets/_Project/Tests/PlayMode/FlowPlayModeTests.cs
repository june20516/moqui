using System.Collections;
using Moqui.Core.Meta;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Simulation;
using Moqui.Unity.UI;
using Moqui.Unity.UI.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    /// <summary>화면 흐름 PlayMode (spec/08): 게임패드만으로 Stage 1 시작, Pause 중 틱·입력 정지.</summary>
    public class FlowPlayModeTests : InputTestFixture
    {
        private const int SettleFrames = 10;
        private const int MaxLoadFrames = 300;

        /// <summary>프레임 수가 아니라 실제 시간으로 기다린다 (배치 모드 프레임은 틱 하나보다 짧을 수 있다).</summary>
        private const float WaitSeconds = 0.5f;


        public override void Setup()
        {
            base.Setup();

            // 실제 save.json·PlayerPrefs를 건드리지 않도록 메모리 세션으로 바꾼다.
            TestSessions.UseMemorySession(id => id == "stage01" || id == "stage02");
        }

        public override void TearDown()
        {
            GameSession.Replace(null);
            StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
            StageBootstrap.RequestedSkills = SkillLoadout.None;
            Time.timeScale = 1f;
            base.TearDown();
        }

        private static IEnumerator WaitForScene(string name)
        {
            for (int i = 0; i < MaxLoadFrames && SceneManager.GetActiveScene().name != name; i++)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name));
            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator GamepadOnly_TitleToStage1()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync(ScreenId.Title.ToString(), LoadSceneMode.Single);
            yield return WaitForScene(ScreenId.Title.ToString());

            PressAndRelease(gamepad.buttonSouth);
            yield return WaitForScene(ScreenId.StageSelect.ToString());

            PressAndRelease(gamepad.buttonSouth);
            yield return WaitForScene(ScreenId.Stage.ToString());

            var bootstrap = Object.FindAnyObjectByType<StageBootstrap>();
            Assert.That(bootstrap.Level.Id, Is.EqualTo("stage01"));
        }

        [UnityTest]
        public IEnumerator Pause_StopsTicksAndDropsInput()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            StageBootstrap.RequestedLevelId = "stage01";
            yield return SceneManager.LoadSceneAsync(ScreenId.Stage.ToString(), LoadSceneMode.Single);
            yield return WaitForScene(ScreenId.Stage.ToString());

            var runner = Object.FindAnyObjectByType<SimulationRunner>();
            var screen = Object.FindAnyObjectByType<StageScreen>();
            var simulation = runner.Driver.Simulation;
            int dashTick = simulation.Player.LastDashStartTick;

            screen.Pause();
            Assert.That(screen.IsPaused, Is.True);
            int pausedTick = simulation.Tick;
            PressAndRelease(keyboard.leftShiftKey);
            yield return new WaitForSecondsRealtime(WaitSeconds);

            Assert.That(simulation.Tick, Is.EqualTo(pausedTick), "no ticks while paused");

            screen.ResumeButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(WaitSeconds);

            Assert.That(simulation.Tick, Is.GreaterThan(pausedTick), "ticks resume");
            Assert.That(simulation.Player.LastDashStartTick, Is.EqualTo(dashTick), "dash pressed during pause is not delivered");
        }
    

        [UnityTest]
        public IEnumerator Text_GlyphsAvailableAfterSceneChange()
        {
            yield return SceneManager.LoadSceneAsync(ScreenId.Title.ToString(), LoadSceneMode.Single);
            yield return WaitForScene(ScreenId.Title.ToString());
            yield return SceneManager.LoadSceneAsync(ScreenId.StageSelect.ToString(), LoadSceneMode.Single);
            yield return WaitForScene(ScreenId.StageSelect.ToString());

            var screen = Object.FindAnyObjectByType<StageSelectScreen>();
            var label = UiFactory.LabelOf(screen.StageButtons[0]);
            // 텍스트는 캔버스 배율을 곱한 크기로 글리프를 요청하므로, 글꼴 텍스처가 비워진 초기 상태(4×4)가 아닌지와 보이는 글자 수로 확인한다.
            Assert.That(label.mainTexture.width, Is.GreaterThan(4), "font texture populated");
            Assert.That(label.cachedTextGenerator.characterCountVisible, Is.GreaterThan(0), "label has visible characters");
        }
    }
}
