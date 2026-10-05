using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Input;
using Moqui.Unity.Presentation;
using Moqui.Unity.Settings;
using Moqui.Unity.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moqui.Unity.Tests
{
    /// <summary>같은 입력을 3인칭과 1인칭에서 재생하면 플레이어 최종 위치가 같다 (spec/00 시점 독립성).</summary>
    public class ViewIndependenceTests : InputTestFixture
    {
        private const string AssetPath = "Assets/_Project/Input/MoquiControls.inputactions";
        private const float FrameTime = 1f / 60f;

        private readonly List<Object> _created = new List<Object>();
        private Tuning _tuning;

        public override void Setup()
        {
            base.Setup();
            _tuning = TuningLoader.Load(new UnityDataSource());
        }

        public override void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            base.TearDown();
        }

        [Test]
        public void SameInputSequence_ThirdVsFirstPerson_SameFinalPosition()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();

            Vector3 thirdPerson = Play(keyboard, mouse, toggleToFirstPerson: false, out CameraViewMode viewA);
            Vector3 firstPerson = Play(keyboard, mouse, toggleToFirstPerson: true, out CameraViewMode viewB);

            Assert.That(viewA, Is.EqualTo(CameraViewMode.ThirdPerson));
            Assert.That(viewB, Is.EqualTo(CameraViewMode.FirstPerson));
            Assert.That(firstPerson, Is.EqualTo(thirdPerson));
            Assert.That(thirdPerson.magnitude, Is.GreaterThan(10f), "the sequence actually moved the player");
        }

        private Vector3 Play(Keyboard keyboard, Mouse mouse, bool toggleToFirstPerson, out CameraViewMode finalView)
        {
            var asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath));
            _created.Add(asset);
            var collector = new CommandCollector(asset, new LookSettings(_tuning));
            var settings = GameSettings.FromTuning(_tuning);
            var world = new CollisionWorld();
            var driver = new SimulationDriver(new GameSimulation(settings, world, System.Numerics.Vector3.Zero), collector);
            var cameraSettings = new CameraSettings(_tuning);
            var camera = new CameraController(cameraSettings, new CameraPoseSolver(cameraSettings, world), new MemoryPreferenceStore());

            // 준비 프레임: 시점 전환만 한다. 두 실행 모두 이 프레임을 거친다.
            if (toggleToFirstPerson)
            {
                PressAndRelease(keyboard.vKey);
            }

            driver.Frame(FrameTime);
            if (collector.ConsumeToggleView())
            {
                camera.Toggle();
            }

            Press(keyboard.wKey);
            for (int frame = 0; frame < 120; frame++)
            {
                // 입력 업데이트마다 "이번 프레임"과 마우스 delta가 바뀌므로, 눌림(edge) 입력은 프레임의 마지막 입력으로 넣는다.
                Set(mouse.delta, new Vector2(frame < 60 ? 5f : -3f, 0f));
                if (frame == 30)
                {
                    Press(keyboard.spaceKey);
                }

                if (frame == 60)
                {
                    PressAndRelease(mouse.rightButton);
                }

                driver.Frame(FrameTime);
                if (collector.ConsumeToggleView())
                {
                    camera.Toggle();
                }

                camera.Update(FrameTime, driver.InterpolatedPlayerPosition, collector.Look.Yaw, collector.Look.Pitch);
            }

            Release(keyboard.wKey);
            Release(keyboard.spaceKey);
            Set(mouse.delta, Vector2.zero);
            collector.Dispose();
            finalView = camera.View;
            return driver.Simulation.Player.Position.ToUnity();
        }
    }
}
