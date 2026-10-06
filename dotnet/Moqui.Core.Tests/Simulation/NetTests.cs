using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>모기장 (spec/06, M14).</summary>
    public class NetTests
    {
        private const ShapeFlags NetFlags = ShapeFlags.Glass | ShapeFlags.Net | ShapeFlags.Attachable;

        /// <summary>그물 벽(z = 100, 두께 2) 가운데에 틈(지름 약 20)이 있는 세계.</summary>
        private static CollisionWorld NetWall()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("net_left", new Vector3(-60f, 100f, 100f), new Vector3(50f, 100f, 1f), NetFlags));
            world.Add(CollisionShape.Box("net_right", new Vector3(60f, 100f, 100f), new Vector3(50f, 100f, 1f), NetFlags));
            world.Add(CollisionShape.Box("net_top", new Vector3(0f, 155f, 100f), new Vector3(10f, 45f, 1f), NetFlags));
            world.Add(CollisionShape.Box("net_bottom", new Vector3(0f, 45f, 100f), new Vector3(10f, 45f, 1f), NetFlags));
            world.Add(CollisionShape.Box("net_gap", new Vector3(0f, 100f, 100f), new Vector3(10f, 10f, 4f), ShapeFlags.NetGap));
            return world;
        }

        [Test]
        public void Net_BlocksBody_NotSight()
        {
            var simulation = TestHumans.Simulation(new Vector3(-60f, 100f, 80f), NetWall());
            TestHumans.DisableReactions(simulation);
            Run(simulation, new PlayerCommand { Move = new Vector2(0f, 1f) }, SecondsToTicks(1f));

            Assert.That(simulation.Player.Position.Z, Is.LessThan(99f - simulation.Player.CollisionRadius + 0.1f), "the net stops the body");
            bool blocked = simulation.World.Raycast(new Vector3(-60f, 100f, 50f), Vector3.UnitZ, 100f, ShapeFlags.Obstacle, out _);
            Assert.That(blocked, Is.False, "sight passes through the net");
        }

        [TestCase(false, 1)]
        [TestCase(true, 0)]
        public void Gap_FlyingThroughWithoutPrecision_Rustles(bool precision, int expectedNoises)
        {
            var simulation = TestHumans.Simulation(new Vector3(0f, 100f, 60f), NetWall());
            TestHumans.DisableReactions(simulation);
            int noises = 0;
            for (int i = 0; i < SecondsToTicks(3f); i++)
            {
                simulation.Step(new PlayerCommand { Move = new Vector2(0f, 1f), PrecisionHeld = precision });
                noises += simulation.Events.OfType<NoiseEmitted>().Count(noise => noise.Source == NoiseSource.Net);
            }

            Assert.That(simulation.Player.Position.Z, Is.GreaterThan(110f), "passed through the gap");
            Assert.That(noises, Is.EqualTo(expectedNoises));
        }
    }
}
