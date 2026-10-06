using System;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>걷는 인간 (spec/02 §9, D-058).</summary>
    public class WalkTests
    {
        private const float Pause = 1f;

        private static WalkSettings Walk => Settings.Walk;

        private static Vector3 Root(GameSimulation simulation) => simulation.Human.RootPosition;

        private static GameSimulation Walking(Vector2[] route, CollisionWorld world = null, Vector3? spawn = null)
        {
            var human = TestHumans.Standing(new HumanWalkDefinition(route, new FloatRange(Pause, Pause), true));
            var simulation = TestHumans.Simulation(spawn ?? new Vector3(0f, 200f, -400f), world, human);
            TestHumans.DisableReactions(simulation);
            return simulation;
        }

        private static float HorizontalDistance(Vector3 a, Vector2 b) => Vector2.Distance(new Vector2(a.X, a.Z), b);

        [Test]
        public void Walk_FollowsRouteAtWalkSpeed_PausesAndTurnsAtEachPoint()
        {
            var first = new Vector2(0f, 200f);
            var second = new Vector2(200f, 200f);
            var simulation = Walking(new[] { first, second });
            float height = Root(simulation).Y;

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(Root(simulation).Z, Is.EqualTo(Walk.WalkSpeed).Within(2f), "walks straight ahead at walkSpeed");
            Assert.That(Root(simulation).Y, Is.EqualTo(height), "the pelvis keeps its height");

            Run(simulation, PlayerCommand.None, SecondsToTicks((200f / Walk.WalkSpeed) - 1f + 0.1f));
            Assert.That(HorizontalDistance(Root(simulation), first), Is.LessThan(HumanWalkSystem.ArriveDistance + 0.5f));
            Vector3 arrived = Root(simulation);
            Run(simulation, PlayerCommand.None, SecondsToTicks(Pause * 0.8f));
            Assert.That(Vector3.Distance(Root(simulation), arrived), Is.LessThan(0.1f), "pauses at the point");

            Run(simulation, PlayerCommand.None, SecondsToTicks(Pause * 0.2f + (90f / Walk.TurnSpeed) + 1.5f));
            Assert.That(simulation.Human.BodyYaw, Is.EqualTo(90f).Within(2f), "turned toward the next point (within the arrive tolerance)");
            Assert.That(Root(simulation).X, Is.GreaterThan(arrived.X + 20f), "then walks to it");
        }

        [Test]
        public void Walk_Suspicious_StopsAndStays()
        {
            var simulation = Walking(new[] { new Vector2(0f, 300f) });
            Run(simulation, PlayerCommand.None, SecondsToTicks(0.5f));
            TestHumans.Provoke(simulation, 50f);
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Suspicious));
            Vector3 stopped = Root(simulation);

            for (int i = 0; i < SecondsToTicks(1f); i++)
            {
                TestHumans.Provoke(simulation, 50f);
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(Vector3.Distance(Root(simulation), stopped), Is.LessThan(0.1f));
        }

        [Test]
        public void Walk_FrenzyTargetOutOfReach_ChasesUntilReachable()
        {
            var simulation = Walking(new[] { new Vector2(0f, 0f) });
            var human = simulation.Human;
            var target = new Vector3(0f, 120f, -250f);
            TestHumans.Provoke(simulation, 100f);
            simulation.Step(PlayerCommand.None);
            Assert.That(human.State, Is.EqualTo(AwarenessState.Frenzy));
            human.HasSeenPlayer = true;
            human.LastSeenPosition = target;
            Assert.That(HumanAttackSystem.CanReach(human, target), Is.False);

            bool reached = false;
            for (int i = 0; i < SecondsToTicks(6f) && !reached; i++)
            {
                simulation.Step(PlayerCommand.None);
                human.LastSeenPosition = target;
                reached = HumanAttackSystem.CanReach(human, target);
            }

            Assert.That(reached, Is.True, "walked close enough to reach");
            Vector3 stopped = Root(simulation);
            for (int i = 0; i < SecondsToTicks(0.5f); i++)
            {
                simulation.Step(PlayerCommand.None);
                human.LastSeenPosition = target;
            }

            Assert.That(HorizontalDistance(Root(simulation), new Vector2(target.X, target.Z)), Is.GreaterThan(HorizontalDistance(stopped, new Vector2(target.X, target.Z)) - 2f), "stops once it can reach");
        }

        [Test]
        public void Walk_BlockedByFurniture_DoesNotPassThrough()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("table", new Vector3(0f, 80f, 100f), new Vector3(60f, 10f, 30f), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = Walking(new[] { new Vector2(0f, 300f) }, world);

            Run(simulation, PlayerCommand.None, SecondsToTicks(5f));

            Assert.That(Root(simulation).Z, Is.LessThan(70f - Walk.Radius + 0.5f));
            Assert.That(Root(simulation).Z, Is.GreaterThan(30f), "walked up to it");
        }

        /// <summary>걷는 사람의 팔뚝에 붙은 모기는 함께 실려 가고, 흔들리는 종아리에 붙은 모기는 튕겨 난다.</summary>
        [TestCase("forearmL", false)]
        [TestCase("calfL", true)]
        public void Walk_RiderOnBody_CarriedUnlessOnSwingingLeg(string partId, bool dislodged)
        {
            var simulation = Walking(new[] { new Vector2(0f, 600f) });
            TestHumans.PlaceNearPart(simulation, partId);
            simulation.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Vector3 start = simulation.Player.Position;

            bool thrown = false;
            for (int i = 0; i < SecondsToTicks(3f) && !thrown; i++)
            {
                simulation.Step(PlayerCommand.None);
                thrown = simulation.Events.OfType<PlayerDislodged>().Any();
            }

            Assert.That(thrown, Is.EqualTo(dislodged));
            if (!dislodged)
            {
                Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
                Assert.That(simulation.Player.Position.Z - start.Z, Is.GreaterThan(150f), "carried along");
            }
        }
    }
}
