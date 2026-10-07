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

        /// <summary>천장에 붙으면 피벗이 법선(아래) 쪽으로 가고, 카메라 구는 천장 안으로 들어가지 않는다 (M13).</summary>
        [Test]
        public void ThirdPerson_AttachedToCeiling_PivotFollowsSurfaceNormal()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("ceiling", new System.Numerics.Vector3(0f, 252f, 0f), new System.Numerics.Vector3(200f, 2f, 200f), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var controller = NewController(world, new MemoryPreferenceStore());
            var player = new Vector3(0f, 250f - 0.42f, 0f);

            CameraPose pose = RunFor(controller, _settings.PivotBlendTime + 0.1f, player, 0f, 0f, Vector3.down);
            CameraPose worldUpPivot = new CameraPoseSolver(_settings, world).ThirdPerson(player, 0f, 0f);

            Assert.That(pose.Position.y, Is.EqualTo(player.y - _settings.HeightOffset).Within(0.01f), "pivot moved below the body along the ceiling normal");
            Assert.That(worldUpPivot.Position.y, Is.LessThanOrEqualTo(250f - _settings.CollisionRadius + Tolerance), "world-up pivot is pinned against the ceiling");
            Assert.That(Vector3.Distance(pose.Position, player), Is.GreaterThan(_settings.HidePlayerDistance), "the body stays visible");
        }

        /// <summary>막히면 즉시 당기고, 다시 트이면 camera.returnSpeed로 천천히 물러난다 (M13, 화면 튐 방지).</summary>
        [Test]
        public void ThirdPerson_Blocked_PullsInAtOnce_ReturnsAtReturnSpeed()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new System.Numerics.Vector3(0f, 0f, -3f), new System.Numerics.Vector3(50f, 50f, 0.5f), ShapeFlags.Obstacle));
            var controller = NewController(world, new MemoryPreferenceStore());
            var open = new Vector3(0f, 0f, 20f);
            var nearWall = Vector3.zero;
            Vector3 pivotOffset = Vector3.up * _settings.HeightOffset;

            controller.Update(FrameTime, open, 0f, 0f);
            CameraPose blocked = controller.Update(FrameTime, nearWall, 0f, 0f);
            float blockedDistance = Vector3.Distance(blocked.Position, nearWall + pivotOffset);
            Assert.That(blockedDistance, Is.LessThan(3f), "pulled in on the same frame");

            CameraPose returning = controller.Update(FrameTime, open, 0f, 0f);
            float returningDistance = Vector3.Distance(returning.Position, open + pivotOffset);
            Assert.That(returningDistance, Is.EqualTo(blockedDistance + (_settings.ReturnSpeed * FrameTime)).Within(Tolerance));

            CameraPose settled = RunFor(controller, _settings.Distance / _settings.ReturnSpeed, open, 0f, 0f);
            Assert.That(Vector3.Distance(settled.Position, open + pivotOffset), Is.EqualTo(_settings.Distance).Within(Tolerance));
        }

        /// <summary>3인칭 카메라가 몸에 너무 가까우면 모키를 숨긴다. 트인 곳에서는 보인다 (M13, 검은 화면 방지).</summary>
        [Test]
        public void ThirdPerson_CameraCrampedAgainstBody_HidesPlayer()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("ceiling", new System.Numerics.Vector3(0f, 252f, 0f), new System.Numerics.Vector3(200f, 2f, 200f), ShapeFlags.Obstacle));
            var controller = NewController(world, new MemoryPreferenceStore());

            RunFor(controller, 0.5f, new Vector3(0f, 100f, 0f), 0f, 0f);
            Assert.That(controller.PlayerHidden, Is.False);

            // 천장에 바짝 붙어 위를 올려다보면 뒤쪽(아래)로는 트여 있다 → 보임. 내려다보면 뒤쪽이 천장이라 카메라가 몸에 몰린다 → 숨김.
            RunFor(controller, 0.5f, new Vector3(0f, 250f - 0.42f, 0f), 0f, -80f);
            Assert.That(controller.PlayerHidden, Is.True);
        }

        /// <summary>
        /// 툰 외곽선 두께가 시야 거리에 비례한다(비율 &lt; 1): 껍질이 늘 카메라보다 표면 쪽에 있어, 표면에 바짝 붙은 카메라가
        /// 외곽선 뒷면에 덮여 까매지지 않는다 (M13).
        /// </summary>
        [Test]
        public void ToonOutline_WidthScalesWithViewDistance()
        {
            var shader = Shader.Find("Moqui/Toon");
            Assert.That(shader, Is.Not.Null);
            int index = shader.FindPropertyIndex("_OutlineDistanceRatio");
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            float ratio = shader.GetPropertyDefaultFloatValue(index);
            Assert.That(ratio, Is.GreaterThan(0f).And.LessThan(0.5f));
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

            visibility.SetHidden(true);
            Assert.That(bodyRenderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly));

            visibility.SetHidden(false);
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

        /// <summary>움직이는 몸에 붙어 있으면 3인칭 거리가 camera.ridingDistanceMul까지 늘고, 멈추면 돌아온다 (플레이 피드백 2026-10-07).</summary>
        [Test]
        public void Riding_PullsTheCameraBack_ThenReturns()
        {
            var controller = NewController(new CollisionWorld(), new MemoryPreferenceStore());
            for (int i = 0; i < 120; i++)
            {
                controller.Update(FrameTime, Vector3.zero, 0f, 0f, null, riding: true);
            }

            Assert.That(controller.DistanceMultiplier, Is.EqualTo(_settings.RidingDistanceMul).Within(1e-4f));
            for (int i = 0; i < 120; i++)
            {
                controller.Update(FrameTime, Vector3.zero, 0f, 0f, null, riding: false);
            }

            Assert.That(controller.DistanceMultiplier, Is.EqualTo(1f).Within(1e-4f));
        }

        private CameraController NewController(CollisionWorld world, IPreferenceStore store)
        {
            return new CameraController(_settings, new CameraPoseSolver(_settings, world), store);
        }

        private static CameraPose RunFor(CameraController controller, float seconds, Vector3 player, float yaw, float pitch, Vector3? surfaceNormal = null)
        {
            CameraPose pose = default;
            int frames = Mathf.CeilToInt(seconds / FrameTime);
            for (int i = 0; i < frames; i++)
            {
                pose = controller.Update(FrameTime, player, yaw, pitch, surfaceNormal);
            }

            return pose;
        }
    }
}
