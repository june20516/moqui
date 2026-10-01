using System.Linq;
using Moqui.Core.Data;
using Moqui.Unity.Data;
using Moqui.Unity.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moqui.Unity.Tests
{
    /// <summary>spec/01 입력 매핑을 가상 장치로 검증한다.</summary>
    public class CommandCollectorTests : InputTestFixture
    {
        private const string AssetPath = "Assets/_Project/Input/MoquiControls.inputactions";
        private const float FrameTime = 1f / 60f;

        private InputActionAsset _asset;
        private CommandCollector _collector;
        private LookSettings _lookSettings;

        public override void Setup()
        {
            base.Setup();
            // 원본 에셋을 바꾸지 않도록 복제본을 쓴다.
            _asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath));
            _lookSettings = new LookSettings(TuningLoader.Load(new UnityDataSource()));
            _collector = new CommandCollector(_asset, _lookSettings);
        }

        public override void TearDown()
        {
            _collector.Dispose();
            Object.DestroyImmediate(_asset);
            base.TearDown();
        }

        /// <summary>Press는 입력 업데이트(=프레임)를 1회 돌리므로, 실제 실행처럼 그 프레임에 샘플링한다.</summary>
        private void PressAndSample(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            Press(button);
            _collector.Sample(FrameTime);
        }

        [TestCase("Move", "<Keyboard>/w", "<Gamepad>/leftStick")]
        [TestCase("Vertical", "<Keyboard>/space", "<Gamepad>/rightTrigger")]
        [TestCase("Vertical", "<Keyboard>/leftAlt", "<Gamepad>/leftTrigger")]
        [TestCase("Look", "<Mouse>/delta", "<Gamepad>/rightStick")]
        [TestCase("Dash", "<Mouse>/rightButton", "<Gamepad>/buttonSouth")]
        [TestCase("Precision", "<Keyboard>/leftCtrl", "<Gamepad>/leftShoulder")]
        [TestCase("Attach", "<Keyboard>/f", "<Gamepad>/buttonEast")]
        [TestCase("Suck", "<Mouse>/leftButton", "<Gamepad>/buttonWest")]
        [TestCase("ToggleView", "<Keyboard>/v", "<Gamepad>/rightStickPress")]
        [TestCase("Skill", "<Keyboard>/q", "<Gamepad>/buttonNorth")]
        [TestCase("Pause", "<Keyboard>/escape", "<Gamepad>/start")]
        public void Asset_GameplayAction_HasKeyboardAndGamepadBindings(string actionName, string keyboardPath, string gamepadPath)
        {
            var action = _asset.FindActionMap(CommandCollector.MapName).FindAction(actionName);
            var paths = action.bindings.Select(binding => binding.path).ToList();

            Assert.That(paths, Does.Contain(keyboardPath));
            Assert.That(paths, Does.Contain(gamepadPath));
        }

        [Test]
        public void Gamepad_AllGameplayInputs_ProduceCommand()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            Set(gamepad.leftStick, new Vector2(0f, 1f));
            Set(gamepad.rightTrigger, 1f);
            PressAndSample(gamepad.buttonSouth);
            PressAndSample(gamepad.buttonEast);
            PressAndSample(gamepad.leftShoulder);
            PressAndSample(gamepad.buttonWest);
            PressAndSample(gamepad.buttonNorth);
            _collector.Sample(FrameTime);
            var command = _collector.NextCommand();

            Assert.That(command.Move.Y, Is.EqualTo(1f).Within(0.01f));
            Assert.That(command.Vertical, Is.EqualTo(1f).Within(0.01f));
            Assert.That(command.DashPressed, Is.True);
            Assert.That(command.AttachPressed, Is.True);
            Assert.That(command.PrecisionHeld, Is.True);
            Assert.That(command.SuckHeld, Is.True);
            Assert.That(command.SkillPressed, Is.True);
        }

        [Test]
        public void KeyboardMouse_AllGameplayInputs_ProduceCommand()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();

            PressAndSample(keyboard.wKey);
            PressAndSample(keyboard.dKey);
            PressAndSample(keyboard.leftAltKey);
            PressAndSample(mouse.rightButton);
            PressAndSample(keyboard.fKey);
            PressAndSample(keyboard.leftCtrlKey);
            PressAndSample(keyboard.qKey);
            PressAndSample(mouse.leftButton);
            _collector.Sample(FrameTime);
            var command = _collector.NextCommand();

            Assert.That(command.Move.Y, Is.GreaterThan(0f));
            Assert.That(command.Move.X, Is.GreaterThan(0f));
            Assert.That(command.Vertical, Is.EqualTo(-1f).Within(0.01f));
            Assert.That(command.DashPressed, Is.True);
            Assert.That(command.AttachPressed, Is.True);
            Assert.That(command.PrecisionHeld, Is.True);
            Assert.That(command.SuckHeld, Is.True);
            Assert.That(command.SkillPressed, Is.True);
        }

        [Test]
        public void ButtonPress_LatchedUntilNextCommand_ThenCleared()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            PressAndRelease(gamepad.buttonSouth);
            _collector.Sample(FrameTime);
            _collector.Sample(FrameTime);

            Assert.That(_collector.NextCommand().DashPressed, Is.True, "press between ticks is kept");
            Assert.That(_collector.NextCommand().DashPressed, Is.False, "consumed once");
        }

        [Test]
        public void ToggleViewAndPause_Gamepad_AreConsumedSeparately()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            PressAndSample(gamepad.rightStickButton);
            PressAndSample(gamepad.startButton);
            _collector.Sample(FrameTime);

            Assert.That(_collector.ConsumeToggleView(), Is.True);
            Assert.That(_collector.ConsumeToggleView(), Is.False);
            Assert.That(_collector.ConsumePause(), Is.True);
        }

        [Test]
        public void GamepadStick_FullRightOneSecond_RotatesYawByLookSpeed()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            Set(gamepad.rightStick, new Vector2(1f, 0f));
            for (int i = 0; i < 60; i++)
            {
                _collector.Sample(FrameTime);
            }

            Assert.That(_collector.Look.Yaw, Is.EqualTo(_lookSettings.GamepadLookSpeed).Within(1f));
        }

        [Test]
        public void MouseDelta_AccumulatedUpAndDown_PitchStaysWithinLimit()
        {
            var mouse = InputSystem.AddDevice<Mouse>();

            for (int i = 0; i < 100; i++)
            {
                Set(mouse.delta, new Vector2(0f, 500f));
                _collector.Sample(FrameTime);
                Assert.That(Mathf.Abs(_collector.Look.Pitch), Is.LessThanOrEqualTo(_lookSettings.PitchLimit));
            }

            Assert.That(_collector.Look.Pitch, Is.EqualTo(_lookSettings.PitchLimit));

            for (int i = 0; i < 100; i++)
            {
                Set(mouse.delta, new Vector2(0f, -500f));
                _collector.Sample(FrameTime);
                Assert.That(Mathf.Abs(_collector.Look.Pitch), Is.LessThanOrEqualTo(_lookSettings.PitchLimit));
            }

            Assert.That(_collector.Look.Pitch, Is.EqualTo(-_lookSettings.PitchLimit));
        }

        [Test]
        public void MouseDelta_Horizontal_UsesPixelSensitivity()
        {
            var mouse = InputSystem.AddDevice<Mouse>();

            Set(mouse.delta, new Vector2(100f, 0f));
            _collector.Sample(FrameTime);

            Assert.That(_collector.Look.Yaw, Is.EqualTo(100f * _lookSettings.MouseSensitivity).Within(1e-3f));
        }
    }
}
