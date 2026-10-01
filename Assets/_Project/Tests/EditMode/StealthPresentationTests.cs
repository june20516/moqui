using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Input;
using Moqui.Unity.Presentation;
using Moqui.Unity.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Moqui.Unity.Tests
{
    public class StealthPresentationTests
    {
        private const float FrameTime = 1f / 60f;

        private Tuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = TuningLoader.Load(new UnityDataSource());
        }

        [Test]
        public void Vignette_EnterShadow_Reaches04In03Seconds()
        {
            var fader = new ShadowVignetteFader(_tuning);
            float transition = _tuning.GetFloat("shadow.transitionTime");
            float target = _tuning.GetFloat("shadow.vignetteIntensity");

            float halfway = Run(fader, true, transition * 0.5f);
            float full = Run(fader, true, transition * 0.5f);

            Assert.That(target, Is.EqualTo(0.4f));
            Assert.That(transition, Is.EqualTo(0.3f));
            Assert.That(halfway, Is.EqualTo(target * 0.5f).Within(0.02f), "linear interpolation");
            Assert.That(full, Is.EqualTo(target).Within(1e-4f));

            float left = Run(fader, false, transition);
            Assert.That(left, Is.EqualTo(0f).Within(1e-4f), "fades back out over the same time");
        }

        [Test]
        public void Vignette_Component_DrivesVolumeVignetteIntensity()
        {
            var go = new GameObject("Vignette");
            try
            {
                go.AddComponent<Volume>();
                var vignette = go.AddComponent<ShadowVignette>();
                for (int i = 0; i < Mathf.CeilToInt(0.3f / FrameTime); i++)
                {
                    vignette.Tick(_tuning, true, FrameTime);
                }

                var volume = go.GetComponent<Volume>();
                Assert.That(volume.isGlobal, Is.True);
                Assert.That(volume.sharedProfile.TryGet(out Vignette effect), Is.True);
                Assert.That(effect.intensity.overrideState, Is.True);
                Assert.That(effect.intensity.value, Is.EqualTo(0.4f).Within(1e-4f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FirstPersonAttached_LookIntoWall_StaysWithinLimitOfNormal()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new System.Numerics.Vector3(0, 100, 50), new System.Numerics.Vector3(100, 100, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = new GameSimulation(GameSettings.FromTuning(_tuning), world, new System.Numerics.Vector3(0, 100, 48f));
            simulation.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Vector3 normal = simulation.Player.Up.ToUnity();

            var cameraSettings = new CameraSettings(_tuning);
            var controller = new CameraController(cameraSettings, new CameraPoseSolver(cameraSettings, world), new MemoryPreferenceStore());
            controller.Toggle();
            var look = new LookState(cameraSettings.FirstPersonAttachedLookLimit + 90f, yaw: 0f, pitch: 0f);
            float limit = cameraSettings.FirstPersonAttachedLookLimit;

            // 벽(+Z) 쪽을 보려고 계속 돌리고, 위아래로도 크게 흔든다.
            for (int i = 0; i < 240; i++)
            {
                look.Rotate(i % 2 == 0 ? 7f : -3f, i % 3 == 0 ? 11f : -9f);
                controller.ConstrainLook(look, normal);
                CameraPose pose = controller.Update(FrameTime, simulation.Player.Position.ToUnity(), look.Yaw, look.Pitch, normal);

                Assert.That(Vector3.Angle(normal, LookConstraint.Direction(look.Yaw, look.Pitch)), Is.LessThanOrEqualTo(limit + 0.05f), $"frame {i}");
                if (controller.Transition >= 1f)
                {
                    Assert.That(Vector3.Dot(pose.Rotation * Vector3.up, normal), Is.GreaterThan(0f), $"frame {i}: camera up leans to the surface normal");
                }
            }
        }

        [Test]
        public void ThirdPersonAttached_LookNotConstrained()
        {
            var cameraSettings = new CameraSettings(_tuning);
            var controller = new CameraController(cameraSettings, new CameraPoseSolver(cameraSettings, new CollisionWorld()), new MemoryPreferenceStore());
            var look = new LookState(85f, yaw: 0f, pitch: 0f);

            controller.ConstrainLook(look, Vector3.back);

            Assert.That(look.Yaw, Is.EqualTo(0f));
            Assert.That(look.Pitch, Is.EqualTo(0f));
        }

        private static float Run(ShadowVignetteFader fader, bool inShadow, float seconds)
        {
            float value = fader.Intensity;
            for (int i = 0; i < Mathf.RoundToInt(seconds / FrameTime); i++)
            {
                value = fader.Update(inShadow, FrameTime);
            }

            return value;
        }
    }
}
