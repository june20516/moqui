using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class CollisionMovementTests
    {
        private const float WallDistance = 20f;
        private const float PenetrationTolerance = 1e-3f;

        [Test]
        public void FlyIntoWall_StopsBeforeSurfaceWithZeroNormalVelocity()
        {
            var simulation = WithWorld(WallAhead(Vector3.Zero), Vector3.Zero);

            Run(simulation, Forward, SecondsToTicks(2f));

            float radius = simulation.Player.CollisionRadius;
            Assert.That(simulation.Player.Position.Z, Is.LessThanOrEqualTo(WallDistance - radius));
            Assert.That(simulation.Player.Position.Z, Is.GreaterThan(WallDistance - radius - 0.05f));
            Assert.That(simulation.Player.Velocity.Z, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void DiagonalIntoWall_SlidesAlongSurface()
        {
            var simulation = WithWorld(WallAhead(Vector3.Zero), Vector3.Zero);
            var diagonal = new PlayerCommand { Move = new Vector2(1f, 1f) };

            Run(simulation, diagonal, SecondsToTicks(2f));

            // 벽에 막힌 뒤에도 벽을 따라 오른쪽(+X)으로 계속 움직인다.
            Assert.That(simulation.Player.Position.X, Is.GreaterThan(50f));
            Assert.That(simulation.Player.Velocity.X, Is.GreaterThan(0f));
        }

        [Test]
        public void VariedInputsInBox_NeverPenetrate()
        {
            var world = Room(Vector3.Zero, new Vector3(30f, 20f, 30f));
            world.Add(CollisionShape.Box("pillar", new Vector3(8, 0, 8), new Vector3(3, 20, 3), Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.6f), ShapeFlags.Obstacle));
            world.Add(CollisionShape.Capsule("arm", new Vector3(-10, -5, 0), new Vector3(-5, 5, 5), 2f, ShapeFlags.Obstacle));
            var simulation = WithWorld(world, Vector3.Zero);

            for (int i = 0; i < 1200; i++)
            {
                float yaw = (i * 37) % 360;
                var command = new PlayerCommand
                {
                    Move = new Vector2(i % 3 - 1, 1f),
                    Vertical = (i / 90) % 3 - 1,
                    LookYaw = yaw,
                };
                simulation.Step(command);
                AssertNoPenetration(simulation, i);
            }
        }

        [Test]
        public void SpawnOverlappingWall_IsPushedOut()
        {
            var world = WallAhead(Vector3.Zero);
            var simulation = WithWorld(world, new Vector3(0, 0, WallDistance - 0.1f));

            simulation.Step(PlayerCommand.None);

            AssertNoPenetration(simulation, 0);
        }

        [TestCaseSource(nameof(WorldRangeOrigins))]
        public void WorldRange_AnyPositionWithin500u_MovesAndCollides(Vector3 origin)
        {
            var free = WithWorld(new CollisionWorld(), origin);
            Run(free, Forward, SecondsToTicks(1f));
            Assert.That(free.Player.Position.Z - origin.Z, Is.EqualTo(56.4f).Within(56.4f * 0.01f), "free flight distance");

            var blocked = WithWorld(WallAhead(origin), origin);
            Run(blocked, Forward, SecondsToTicks(2f));
            float travelled = blocked.Player.Position.Z - origin.Z;
            float radius = blocked.Player.CollisionRadius;
            Assert.That(travelled, Is.LessThanOrEqualTo(WallDistance - radius));
            Assert.That(travelled, Is.GreaterThan(WallDistance - radius - 0.05f));
        }

        private static IEnumerable<Vector3> WorldRangeOrigins()
        {
            float[] values = { -500f, 0f, 500f - WallDistance - 2f };
            foreach (float x in values)
            {
                foreach (float y in values)
                {
                    foreach (float z in values)
                    {
                        yield return new Vector3(x, y, z);
                    }
                }
            }
        }

        private static CollisionWorld WallAhead(Vector3 origin)
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", origin + new Vector3(0, 0, WallDistance + 1f), new Vector3(200, 200, 1), ShapeFlags.Obstacle));
            return world;
        }

        private static CollisionWorld Room(Vector3 center, Vector3 halfSize)
        {
            const float thickness = 1f;
            var world = new CollisionWorld();
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 normal = axis == 0 ? Vector3.UnitX : axis == 1 ? Vector3.UnitY : Vector3.UnitZ;
                    float offset = (axis == 0 ? halfSize.X : axis == 1 ? halfSize.Y : halfSize.Z) + thickness;
                    Vector3 extents = halfSize + new Vector3(thickness * 2f);
                    extents = axis == 0 ? new Vector3(thickness, extents.Y, extents.Z) : axis == 1 ? new Vector3(extents.X, thickness, extents.Z) : new Vector3(extents.X, extents.Y, thickness);
                    world.Add(CollisionShape.Box($"wall{axis}{sign}", center + (normal * (sign * offset)), extents, ShapeFlags.Obstacle));
                }
            }

            return world;
        }

        private static void AssertNoPenetration(GameSimulation simulation, int tick)
        {
            foreach (var shape in simulation.World.Shapes)
            {
                float distance = ShapeGeometry.Closest(shape, simulation.Player.Position).Distance;
                Assert.That(distance, Is.GreaterThanOrEqualTo(simulation.Player.CollisionRadius - PenetrationTolerance), $"tick {tick}: penetrated {shape.Id}");
            }
        }
    }
}
