using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class HumanMotionTests
    {
        private static PlayerCommand Attach => new PlayerCommand { AttachPressed = true };

        [Test]
        public void Actions_ScheduledEvery4To9Seconds_MoveParts()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind, human: TestHumans.Seated(actions: new[] { TestHumans.Scroll }));
            var forearm = simulation.Human.Shapes["forearmR"];
            Vector3 restTip = forearm.PointB;
            var starts = new List<int>();
            float maxLift = 0f;
            int restChecks = 0;
            bool wasActive = false;

            for (int i = 0; i < SecondsToTicks(30f); i++)
            {
                simulation.Step(PlayerCommand.None);
                if (simulation.Human.CurrentAction != null && simulation.Human.ActionStartTick == simulation.Tick - 1)
                {
                    starts.Add(simulation.Human.ActionStartTick);
                }

                maxLift = System.Math.Max(maxLift, forearm.PointB.Y - restTip.Y);
                bool active = simulation.Human.CurrentAction != null;
                if (wasActive && !active)
                {
                    Assert.That(Vector3.Distance(forearm.PointB, restTip), Is.LessThan(1e-3f), "returns to rest after the action");
                    restChecks++;
                }

                wasActive = active;
            }

            Assert.That(starts.Count, Is.GreaterThanOrEqualTo(3));
            var range = Settings.HumanMotion.ActionInterval;
            foreach (int gap in starts.Zip(starts.Skip(1), (a, b) => b - a))
            {
                Assert.That(gap * GameSimulation.DeltaTime, Is.InRange(range.Min - 0.02f, range.Max + 0.02f));
            }

            Assert.That(maxLift, Is.EqualTo(3f).Within(0.05f), "tip reaches the motion peak");
            Assert.That(restChecks, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Attached_DuringSlowAction_FollowsPartWithoutDislodging()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero, human: TestHumans.Seated(actions: new[] { TestHumans.Scroll }));
            TestHumans.DisableReactions(simulation);
            TestHumans.PlaceNearPart(simulation, "forearmR");
            simulation.Step(Attach);
            var forearm = simulation.Human.Shapes["forearmR"];
            Vector3 start = simulation.Player.Position;
            simulation.HumanSystem.Motion.Start(simulation.Human, TestHumans.Scroll, simulation.Tick);

            float moved = 0f;
            for (int i = 0; i < SecondsToTicks(TestHumans.Scroll.Duration * 0.5f); i++)
            {
                simulation.Step(PlayerCommand.None);
                Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached), $"tick {i}");
                float gap = ShapeGeometry.Closest(forearm, simulation.Player.Position).Distance;
                Assert.That(gap, Is.EqualTo(simulation.Player.CollisionRadius + SphereMover.Skin).Within(0.02f), $"tick {i}: stays on the surface");
                moved = System.Math.Max(moved, Vector3.Distance(simulation.Player.Position, start));
            }

            Assert.That(moved, Is.GreaterThan(0.5f), "the mosquito moved with the forearm");
        }

        [Test]
        public void Actions_KeepHappeningWhilePlayerIsAttached()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero, human: TestHumans.Seated(actions: new[] { TestHumans.Scroll }));
            TestHumans.DisableReactions(simulation);
            TestHumans.PlaceNearPart(simulation, "calfR");
            simulation.Step(Attach);

            int startedWhileAttached = 0;
            for (int i = 0; i < SecondsToTicks(20f); i++)
            {
                simulation.Step(PlayerCommand.None);
                if (simulation.Human.CurrentAction != null && simulation.Human.ActionStartTick == simulation.Tick - 1 && simulation.Player.State == PlayerState.Attached)
                {
                    startedWhileAttached++;
                }
            }

            Assert.That(startedWhileAttached, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void FastAction_PartFasterThanDislodgeSpeed_DislodgesWithoutDeath()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero, human: TestHumans.Seated(actions: new[] { TestHumans.GrabTissue }));
            TestHumans.DisableReactions(simulation);
            TestHumans.PlaceNearPart(simulation, "forearmR");
            simulation.Step(Attach);
            simulation.HumanSystem.Motion.Start(simulation.Human, TestHumans.GrabTissue, simulation.Tick);

            PlayerDislodged dislodged = null;
            for (int i = 0; i < SecondsToTicks(TestHumans.GrabTissue.Duration) && dislodged == null; i++)
            {
                simulation.Step(PlayerCommand.None);
                dislodged = simulation.Events.OfType<PlayerDislodged>().SingleOrDefault();
            }

            Assert.That(dislodged, Is.Not.Null);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dislodged));
            Assert.That(simulation.Player.Anchor, Is.Null);
            Vector3 releasedAt = simulation.Player.Position;
            float deceleration = Settings.Flight.Speed / Settings.Flight.DecelTime;
            float expectedSpeed = System.MathF.Sqrt(2f * deceleration * Settings.HumanMotion.DislodgePush);
            Assert.That(simulation.Player.Velocity.Length(), Is.EqualTo(expectedSpeed).Within(1e-3f), "push speed slides exactly dislodgePush under normal deceleration");
            Assert.That(Vector3.Dot(Vector3.Normalize(simulation.Player.Velocity), dislodged.Direction), Is.GreaterThan(0.999f), "pushed along the part motion");

            // 경직 동안 입력이 무시되고, 감속하며 dislodgePush만큼 밀린 뒤 Flying으로 돌아온다.
            Run(simulation, new PlayerCommand { Move = new Vector2(0f, -1f) }, SecondsToTicks(Settings.HumanMotion.DislodgeStun) - 1);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dislodged), "input ignored during stun");
            Run(simulation, PlayerCommand.None, SecondsToTicks(0.5f));
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            // 팔이 계속 같은 방향으로 휘둘러지면 모기를 더 밀 수 있으므로 최소 거리만 본다.
            Assert.That(Vector3.Distance(simulation.Player.Position, releasedAt), Is.GreaterThanOrEqualTo(Settings.HumanMotion.DislodgePush - 0.05f));
            Assert.That(simulation.Events.OfType<PlayerDied>(), Is.Empty);
        }

        [Test]
        public void Frenzy_NoNewActionStarts()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind, human: TestHumans.Seated(actions: new[] { TestHumans.Scroll }));
            TestHumans.Provoke(simulation, 100f);

            int started = 0;
            for (int i = 0; i < SecondsToTicks(7f); i++)
            {
                simulation.Step(PlayerCommand.None);
                if (simulation.Human.CurrentAction != null && simulation.Human.ActionStartTick == simulation.Tick - 1)
                {
                    started++;
                }
            }

            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));
            Assert.That(started, Is.EqualTo(0));
        }

        [Test]
        public void SameSeed_MotionAndReactionsReplayExactly()
        {
            var first = Record(TestHumans.DefaultSeed);
            var second = Record(TestHumans.DefaultSeed);
            var other = Record(TestHumans.DefaultSeed + 5);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(other, Is.Not.EqualTo(first));
        }

        private static List<string> Record(ulong seed)
        {
            var human = TestHumans.Seated(actions: new[] { TestHumans.Scroll, TestHumans.GrabTissue });
            var simulation = TestHumans.Simulation(Vector3.Zero, human: human, seed: seed);
            TestHumans.PlaceNearPart(simulation, "calfL");
            simulation.Step(Attach);
            var log = new List<string>();
            for (int i = 0; i < SecondsToTicks(25f); i++)
            {
                simulation.Step(PlayerCommand.None);
                var tip = simulation.Human.Shapes["forearmR"].PointB;
                log.Add($"{simulation.Tick}:{simulation.Human.CurrentAction?.Name}:{tip.X:R},{tip.Y:R},{tip.Z:R}:{simulation.Player.State}");
                log.AddRange(simulation.Events.Select(e => $"{e.GetType().Name}@{e.Tick}"));
            }

            return log;
        }
    }
}
