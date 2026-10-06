using System.Collections;
using Moqui.Core.Meta;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Simulation;
using Moqui.Unity.UI;
using Moqui.Unity.UI.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moqui.Unity.Tests
{
    /// <summary>
    /// 전 흐름 (GOAL D2): Title → StageSelect → Stage 1 → … → Stage 5 → Ending을 장치 입력만으로 진행한다.
    /// 메뉴는 실제 키보드/마우스·게임패드 입력으로 조작하고, 스테이지 클리어는 흡혈 게이지를 채워 유도한다
    /// (스테이지를 실제로 클리어할 수 있는지는 Core 시나리오 봇이 검증한다, D5).
    /// </summary>
    public class FullFlowPlayModeTests : InputTestFixture
    {
        private const int SettleFrames = 10;
        private const int MaxLoadFrames = 600;
        private const int MaxNavigationPresses = 12;
        private const float MaxResultWaitSeconds = 6f;

        public enum Device
        {
            KeyboardMouse,
            Gamepad,
        }


        private ButtonControl _down;
        private ButtonControl _up;
        private ButtonControl _submit;

        public override void Setup()
        {
            base.Setup();
            TestSessions.UseMemorySession();
        }

        public override void TearDown()
        {
            GameSession.Replace(null);
            StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
            StageBootstrap.RequestedSkills = SkillLoadout.None;
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TitleThroughAllStagesToEnding([Values(Device.KeyboardMouse, Device.Gamepad)] Device device)
        {
            Mouse mouse = null;
            if (device == Device.Gamepad)
            {
                var gamepad = InputSystem.AddDevice<Gamepad>();
                _down = gamepad.dpad.down;
                _up = gamepad.dpad.up;
                _submit = gamepad.buttonSouth;
            }
            else
            {
                var keyboard = InputSystem.AddDevice<Keyboard>();
                mouse = InputSystem.AddDevice<Mouse>();
                _down = keyboard.downArrowKey;
                _up = keyboard.upArrowKey;
                _submit = keyboard.enterKey;
            }

            yield return SceneManager.LoadSceneAsync(ScreenId.Title.ToString(), LoadSceneMode.Single);
            yield return WaitForScene(ScreenId.Title);

            var title = Object.FindAnyObjectByType<TitleScreen>();
            if (mouse != null)
            {
                yield return Click(mouse, title.StartButton);
            }
            else
            {
                yield return NavigateAndSubmit(title.StartButton);
            }

            yield return WaitForScene(ScreenId.StageSelect);

            // 스테이지 목록(data/stages.json) 순서대로 전부 (레벨을 추가하면 자동으로 포함, D-061).
            var catalog = GameSession.Current.Catalog;
            foreach (var stage in catalog.Stages)
            {
                string levelId = stage.LevelId;
                var select = Object.FindAnyObjectByType<StageSelectScreen>();
                var button = select.ButtonFor(levelId);
                Assert.That(button, Is.Not.Null, levelId);
                Assert.That(button.interactable, Is.True, $"{levelId} unlocked");
                yield return NavigateAndSubmit(button);
                yield return WaitForScene(ScreenId.Stage);
                Assert.That(Object.FindAnyObjectByType<StageBootstrap>().Level.Id, Is.EqualTo(levelId));

                var screen = Object.FindAnyObjectByType<StageScreen>();
                yield return ClearStage(screen);

                if (!catalog.IsLast(levelId))
                {
                    Assert.That(screen.EndingButton.gameObject.activeSelf, Is.False, $"{levelId}: no ending yet");
                    yield return NavigateAndSubmit(screen.StageSelectButton);
                    yield return WaitForScene(ScreenId.StageSelect);
                }
                else
                {
                    Assert.That(screen.EndingButton.gameObject.activeSelf, Is.True, "first clear of the last stage unlocks the ending");
                    yield return NavigateAndSubmit(screen.EndingButton);
                    yield return WaitForScene(ScreenId.Ending);
                }
            }

            Assert.That(GameSession.Current.Save.Stages.Count, Is.EqualTo(catalog.Stages.Count), "every stage record saved");
        }

        /// <summary>흡혈 게이지를 채워 클리어시키고 Result가 뜰 때까지 기다린다.</summary>
        private static IEnumerator ClearStage(StageScreen screen)
        {
            var runner = Object.FindAnyObjectByType<SimulationRunner>();
            runner.Driver.Simulation.Player.BloodGauge = 100f;
            float waited = 0f;
            while (!screen.ResultPanel.activeSelf && waited < MaxResultWaitSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(screen.ResultPanel.activeSelf, Is.True, "result shown after clear");
            Assert.That(screen.ResultTitle.text, Is.EqualTo("클리어!"));
        }

        /// <summary>방향 입력으로 대상 버튼까지 포커스를 옮긴 뒤 확인 입력을 누른다.</summary>
        private IEnumerator NavigateAndSubmit(Selectable target)
        {
            yield return MoveFocus(target, _down);
            yield return MoveFocus(target, _up);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(target.gameObject), $"focus on {target.name}");
            PressAndRelease(_submit);
            yield return null;
            yield return null;
        }

        private IEnumerator MoveFocus(Selectable target, ButtonControl direction)
        {
            // 누름과 뗌을 같은 업데이트에 넣으면 UI 모듈이 읽을 때 이미 0이라 이동하지 않는다: 한 프레임 누른 뒤 뗀다.
            for (int i = 0; i < MaxNavigationPresses && EventSystem.current.currentSelectedGameObject != target.gameObject; i++)
            {
                Press(direction);
                yield return null;
                Release(direction);
                yield return null;
                yield return null;
            }
        }

        private IEnumerator Click(Mouse mouse, Selectable target)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, target.transform.position);
            Set(mouse.position, point);
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;
            yield return null;
        }

        private static IEnumerator WaitForScene(ScreenId screen)
        {
            string name = screen.ToString();
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
    }
}
