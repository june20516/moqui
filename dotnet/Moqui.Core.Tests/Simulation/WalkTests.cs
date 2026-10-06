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
            Assert.That(Root(simulation).Z, Is.EqualTo(Walk.WalkSpeed * (1f - (Walk.AccelTime * 0.5f))).Within(2f), "speeds up over walkAccelTime, then walks at walkSpeed");
            Assert.That(simulation.Human.WalkSpeed, Is.EqualTo(Walk.WalkSpeed).Within(0.5f));
            Assert.That(Root(simulation).Y, Is.EqualTo(height), "the pelvis keeps its height");

            // 경로점 앞에서 미리 줄여 멈춘다(가감속): 도착할 때까지 진행한다.
            int ticks = 0;
            while (simulation.Human.WalkPauseEndTick <= simulation.Tick && ticks++ < SecondsToTicks(6f))
            {
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(HorizontalDistance(Root(simulation), first), Is.LessThan(HumanWalkSystem.ArriveDistance + 0.5f));
            Assert.That(simulation.Human.WalkSpeed, Is.LessThan(Walk.WalkSpeed * 0.5f), "arrives slowing down, not at full speed");
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
            for (int i = 0; i < SecondsToTicks(Walk.AccelTime + 0.1f); i++)
            {
                TestHumans.Provoke(simulation, 50f);
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(simulation.Human.WalkSpeed, Is.EqualTo(0f), "slows to a stop within walkAccelTime");
            Vector3 stopped = Root(simulation);
            for (int i = 0; i < SecondsToTicks(1f); i++)
            {
                TestHumans.Provoke(simulation, 50f);
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(Vector3.Distance(Root(simulation), stopped), Is.LessThan(0.1f), "and stays");
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
            for (int i = 0; i < SecondsToTicks(1.5f); i++)
            {
                simulation.Step(PlayerCommand.None);
                human.LastSeenPosition = target;
            }

            Assert.That(human.WalkSpeed, Is.EqualTo(0f), "stops (after a few slowing steps) once it can reach");
            Assert.That(HorizontalDistance(Root(simulation), new Vector2(target.X, target.Z)), Is.GreaterThan(30f), "does not walk into the target");
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
