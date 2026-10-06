using System;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class AttachTests
    {
        private static PlayerCommand Attach => new PlayerCommand { AttachPressed = true };

        /// <summary>벽 옆면·천장 아랫면처럼 방향과 무관하게 붙고, 붙어 있는 동안 표면에서 떨어지지 않는다 (spec/03, M12).</summary>
        [TestCase("ceiling", 0f, 108.5f, 0f, 0f, -1f, 0f)]
        [TestCase("wallSide", 8.5f, 50f, 0f, -1f, 0f, 0f)]
        public void Attach_CeilingAndWallSide_StaysOnSurface(string name, float x, float y, float z, float nx, float ny, float nz)
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("ceiling", new Vector3(0, 110, 0), new Vector3(200, 2, 200), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            world.Add(CollisionShape.Box("wallSide", new Vector3(10, 50, 0), new Vector3(2, 100, 200), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(x, y, z));

            simulation.Step(Attach);
            Run(simulation, PlayerCommand.None, SecondsToTicks(2f));

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached), name);
            Assert.That(Vector3.Distance(simulation.Player.Up, new Vector3(nx, ny, nz)), Is.LessThan(1e-3f), $"{name} normal");
            Assert.That(world.ClosestSurface(simulation.Player.Position, 1f, ShapeFlags.Attachable, out var surface), Is.True);
            Assert.That(Vector3.Distance(simulation.Player.Position, surface.Point), Is.EqualTo(simulation.Player.CollisionRadius + SphereMover.Skin).Within(1e-3f), $"{name} stays on the surface");
        }

        [Test]
        public void Attach_WithinRange_AttachesAlignedToNormal()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0, 0, 10), new Vector3(50, 50, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 0, 9f - 1.5f));

            simulation.Step(Attach);

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            float angle = MathF.Acos(Math.Clamp(Vector3.Dot(simulation.Player.Up, -Vector3.UnitZ), -1f, 1f)) * 180f / MathF.PI;
            Assert.That(angle, Is.LessThan(5f));
            Assert.That(simulation.Player.Velocity, Is.EqualTo(Vector3.Zero));
            Assert.That(simulation.Events.OfType<PlayerAttached>().Single().ShapeId, Is.EqualTo("wall"));
        }

        /// <summary>F 착지는 attach.snapRange 안의 가장 가까운 표면으로 미끄러져 붙고, 그보다 멀면 붙지 않는다 (gulf §2, D-066).</summary>
        [Test]
        public void Attach_SnapsWithinSnapRange_NotBeyond()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0, 0, 10), new Vector3(50, 50, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            float snap = Settings.Attach.SnapRange;
            var near = WithWorld(world, new Vector3(0, 0, 9f - (snap - 0.5f)));
            var far = WithWorld(world, new Vector3(0, 0, 9f - (snap + 0.5f)));

            near.Step(Attach);
            far.Step(Attach);

            Assert.That(near.Player.State, Is.EqualTo(PlayerState.Attached));
            Assert.That(near.Player.Position.Z, Is.EqualTo(9f - near.Player.CollisionRadius - SphereMover.Skin).Within(1e-3f), "placed on the surface (face at z = 9)");
            Assert.That(far.Player.State, Is.EqualTo(PlayerState.Flying));
        }

        /// <summary>정밀 비행으로 표면 쪽으로 날다 닿으면 F 없이 내려앉는다. 일반 비행·스치듯 지나가기는 붙지 않는다 (gulf §2, D-066).</summary>
        [TestCase(true, 0f, 1f, true)]
        [TestCase(false, 0f, 1f, false)]
        [TestCase(true, 1f, 0f, false)]
        public void PrecisionFlight_IntoSurface_AutoLands(bool precision, float moveX, float moveY, bool lands)
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0, 0, 10), new Vector3(200, 200, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 0, 9f - 3f));
            var command = new PlayerCommand { Move = new Vector2(moveX, moveY), PrecisionHeld = precision };

            Run(simulation, command, SecondsToTicks(1.5f));

            Assert.That(simulation.Player.State == PlayerState.Attached, Is.EqualTo(lands), "still holding the key toward the wall keeps it attached");
            if (lands)
            {
                // 키를 놓았다가 다시 누르면 평소처럼 이동 입력으로 뗀다.
                simulation.Step(PlayerCommand.None);
                simulation.Step(command);
                Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying), "after releasing, move detaches again");

                // 표면에서 멀어지는 입력은 놓지 않아도 바로 뗀다.
                var again = WithWorld(world, new Vector3(0, 0, 9f - 3f));
                Run(again, command, SecondsToTicks(1.5f));
                again.Step(new PlayerCommand { Move = new Vector2(0f, -1f), PrecisionHeld = true });
                Assert.That(again.Player.State, Is.EqualTo(PlayerState.Flying));
            }
        }

        [Test]
        public void Attach_NonAttachableSurface_DoesNotAttach()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("glass", new Vector3(0, 0, 10), new Vector3(50, 50, 1), ShapeFlags.Obstacle | ShapeFlags.Glass));
            var simulation = WithWorld(world, new Vector3(0, 0, 8f));

            simulation.Step(Attach);

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
        }

        [Test]
        public void Detach_AttachPressedAgain_LeavesAlongNormalBy2u()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0, 0, 10), new Vector3(50, 50, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 0, 8f));
            simulation.Step(Attach);
            float attachedZ = simulation.Player.Position.Z;

            simulation.Step(Attach);

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            Assert.That(attachedZ - simulation.Player.Position.Z, Is.EqualTo(Settings.Attach.DetachOffset).Within(1e-3f));
        }

        [Test]
        public void Detach_MoveInput_Leaves()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0, 0, 10), new Vector3(50, 50, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 0, 8f));
            simulation.Step(Attach);

            simulation.Step(new PlayerCommand { Move = new Vector2(-1f, 0f) });

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            Assert.That(simulation.Events.OfType<PlayerDetached>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Attached_ToHumanSkin_ReportsSkinSite()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            TestHumans.PlaceNearPart(simulation, "forearmR");

            simulation.Step(Attach);

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Assert.That(simulation.Events.OfType<PlayerAttached>().Single().IsSkinSite, Is.True);
        }

        [Test]
        public void Attached_ShapeMoves_PlayerFollowsSameSurfacePoint()
        {
            var world = new CollisionWorld();
            var capsule = CollisionShape.Capsule("arm", new Vector3(0, 0, 0), new Vector3(0, 20, 0), 4f, ShapeFlags.Obstacle | ShapeFlags.Attachable);
            world.Add(capsule);
            var simulation = WithWorld(world, new Vector3(5f, 10f, 0f));
            simulation.Step(Attach);
            Vector3 before = simulation.Player.Position;

            // 팔을 10u 옮기고 z축으로 30° 돌린다.
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 6f);
            Vector3 offset = new Vector3(0, 0, 10);
            capsule.SetSegment(offset, offset + Vector3.Transform(new Vector3(0, 20, 0), rotation));
            simulation.Step(PlayerCommand.None);

            float distanceToSurface = ShapeGeometry.Closest(capsule, simulation.Player.Position).Distance;
            Assert.That(distanceToSurface, Is.EqualTo(simulation.Player.CollisionRadius + SphereMover.Skin).Within(1e-3f));
            Vector3 expected = offset + Vector3.Transform(before, rotation);
            Assert.That(Vector3.Distance(simulation.Player.Position, expected), Is.LessThan(0.01f));
        }
    }
}
