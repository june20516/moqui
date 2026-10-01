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

        [Test]
        public void Attach_SurfaceFartherThan2u_DoesNotAttach()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0, 0, 10), new Vector3(50, 50, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 0, 9f - 2.1f));

            simulation.Step(Attach);

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
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
