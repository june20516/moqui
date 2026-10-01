using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation;
using Moqui.Unity.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Moqui.Unity.Tests
{
    public class CameraTests
    {
        private const float Tolerance = 1e-3f;
        private const float FrameTime = 1f / 60f;

        private CameraSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new CameraSettings(TuningLoader.Load(new UnityDataSource()));
        }

        [Test]
        public void DefaultView_NoSavedData_IsThirdPerson()
        {
            var controller = NewController(new CollisionWorld(), new MemoryPreferenceStore());

            Assert.That(controller.View, Is.EqualTo(CameraViewMode.ThirdPerson));
        }

        [Test]
        public void ThirdPerson_NearClipAndFov_MatchTuning()
        {
            var controller = NewController(new CollisionWorld(), new MemoryPreferenceStore());

            CameraPose pose = controller.Update(FrameTime, Vector3.zero, 0f, 0f);
            var camera = new GameObject("TestCamera").AddComponent<Camera>();
            pose.ApplyTo(camera);

            Assert.That(camera.nearClipPlane, Is.EqualTo(_settings.NearClip).Within(1e-6f));
            Assert.That(camera.fieldOfView, Is.EqualTo(_settings.Fov).Within(Tolerance));
            Object.DestroyImmediate(camera.gameObject);
        }

        [Test]
        public void ThirdPerson_WallBehindPlayer_CameraDoesNotPassThroughWall()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new System.Numerics.Vector3(0f, 0f, -3f), new System.Numerics.Vector3(50f, 50f, 0.5f), ShapeFlags.Obstacle));
            var solver = new CameraPoseSolver(_settings, world);
            Vector3 player = Vector3.zero;

            CameraPose pose = solver.ThirdPerson(player, 0f, 10f);

            Vector3 pivot = player + (Vector3.up * _settings.HeightOffset);
            Vector3 toPivot = pivot - pose.Position;
            bool blocked = world.Raycast(pose.Position.ToCore(), toPivot.ToCore(), toPivot.magnitude, ShapeFlags.Obstacle, out _);
            Assert.That(blocked, Is.False, "nothing between camera and pivot");
            Assert.That(pose.Position.z, Is.GreaterThan(-2.5f + _settings.CollisionRadius - Tolerance), "camera sphere stays in front of the wall");
            Assert.That(Vector3.Distance(pose.Position, pivot), Is.LessThan(_settings.Distance));
        }

        [Test]
        public void ThirdPerson_NoObstacle_UsesFullDistance()
        {
            var solver = new CameraPoseSolver(_settings, new CollisionWorld());

            CameraPose pose = solver.ThirdPerson(Vector3.zero, 0f, 0f);

            Vector3 pivot = Vector3.up * _settings.HeightOffset;
            Assert.That(Vector3.Distance(pose.Position, pivot), Is.EqualTo(_settings.Distance).Within(Tolerance));
        }

        [Test]
        public void Toggle_AfterSwitchTime_ReachesFirstPersonTargetThenBack()
        {
            var world = new CollisionWorld();
            var solver = new CameraPoseSolver(_settings, world);
            var controller = new CameraController(_settings, solver, new MemoryPreferenceStore());
            Vector3 player = new Vector3(3f, 4f, 5f);
            const float yaw = 30f;
            const float pitch = -20f;

            controller.Toggle();
            CameraPose pose = RunFor(controller, _settings.SwitchTime, player, yaw, pitch);
            CameraPose firstPerson = solver.FirstPerson(player, yaw, pitch);

            Assert.That(controller.View, Is.EqualTo(CameraViewMode.FirstPerson));
            Assert.That(Vector3.Distance(pose.Position, firstPerson.Position), Is.LessThan(Tolerance));
            Assert.That(pose.Fov, Is.EqualTo(_settings.FirstPersonFov).Within(Tolerance));
            Assert.That(pose.NearClip, Is.EqualTo(_settings.FirstPersonNearClip).Within(1e-6f));

            controller.Toggle();
            pose = RunFor(controller, _settings.SwitchTime, player, yaw, pitch);
            CameraPose thirdPerson = solver.ThirdPerson(player, yaw, pitch);

            Assert.That(Vector3.Distance(pose.Position, thirdPerson.Position), Is.LessThan(Tolerance));
            Assert.That(pose.Fov, Is.EqualTo(_settings.Fov).Within(Tolerance));
        }

        [Test]
        public void Toggle_BeforeAndAfter_YawPitchUnchanged()
        {
            var controller = NewController(new CollisionWorld(), new MemoryPreferenceStore());
            const float yaw = 123f;
            const float pitch = 40f;

            CameraPose before = RunFor(controller, 0.1f, Vector3.zero, yaw, pitch);
            controller.Toggle();
            CameraPose after = RunFor(controller, _settings.SwitchTime, Vector3.zero, yaw, pitch);

            Assert.That(Quaternion.Angle(before.Rotation, after.Rotation), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(after.Rotation, CameraPoseSolver.LookRotation(yaw, pitch)), Is.LessThan(0.01f));
        }

        [Test]
        public void SelectedView_NewControllerWithSameStore_IsRestored()
        {
            var store = new MemoryPreferenceStore();
            var first = NewController(new CollisionWorld(), store);

            first.Toggle();
            var restarted = NewController(new CollisionWorld(), store);

            Assert.That(restarted.View, Is.EqualTo(CameraViewMode.FirstPerson));
        }

        [Test]
        public void SelectedView_PlayerPrefsStore_SurvivesRestart()
        {
            string previous = PlayerPrefs.GetString(CameraController.ViewPreferenceKey, null);
            try
            {
                PlayerPrefs.DeleteKey(CameraController.ViewPreferenceKey);
                NewController(new CollisionWorld(), new PlayerPrefsStore()).Toggle();

                var restarted = NewController(new CollisionWorld(), new PlayerPrefsStore());

                Assert.That(restarted.View, Is.EqualTo(CameraViewMode.FirstPerson));
            }
            finally
            {
                if (previous == null)
                {
                    PlayerPrefs.DeleteKey(CameraController.ViewPreferenceKey);
                }
                else
                {
                    PlayerPrefs.SetString(CameraController.ViewPreferenceKey, previous);
                }
            }
        }

        [Test]
        public void FirstPerson_PlayerRenderers_ShadowsOnlyThenRestored()
        {
            var root = new GameObject("Player");
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(root.transform);
            var bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.shadowCastingMode = ShadowCastingMode.On;
            var visibility = root.AddComponent<PlayerViewVisibility>();

            visibility.SetFirstPerson(true);
            Assert.That(bodyRenderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly));

            visibility.SetFirstPerson(false);
            Assert.That(bodyRenderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
            Object.DestroyImmediate(root);
        }

        [TestCase(0f, 0f)]
        [TestCase(170f, 60f)]
        [TestCase(-90f, -80f)]
        [TestCase(45f, 85f)]
        public void AttachedLook_AnyInput_StaysWithinLimitOfNormal(float yaw, float pitch)
        {
            Vector3 normal = Vector3.up;
            float limit = _settings.FirstPersonAttachedLookLimit;

            LookConstraint.ClampToCone(ref yaw, ref pitch, normal, limit);

            Assert.That(Vector3.Angle(normal, LookConstraint.Direction(yaw, pitch)), Is.LessThanOrEqualTo(limit + 0.01f));
        }

        [Test]
        public void AttachedLook_InsideCone_Unchanged()
        {
            float yaw = 20f;
            float pitch = 5f;

            LookConstraint.ClampToCone(ref yaw, ref pitch, Vector3.forward, _settings.FirstPersonAttachedLookLimit);

            Assert.That(yaw, Is.EqualTo(20f));
            Assert.That(pitch, Is.EqualTo(5f));
        }

        private CameraController NewController(CollisionWorld world, IPreferenceStore store)
        {
            return new CameraController(_settings, new CameraPoseSolver(_settings, world), store);
        }

        private static CameraPose RunFor(CameraController controller, float seconds, Vector3 player, float yaw, float pitch)
        {
            CameraPose pose = default;
            int frames = Mathf.CeilToInt(seconds / FrameTime);
            for (int i = 0; i < frames; i++)
            {
                pose = controller.Update(FrameTime, player, yaw, pitch);
            }

            return pose;
        }
    }
}
