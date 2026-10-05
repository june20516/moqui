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
    public class SuckTests
    {
        private static PlayerCommand Attach => new PlayerCommand { AttachPressed = true };

        private static PlayerCommand Suck => new PlayerCommand { SuckHeld = true };

        /// <summary>반응을 끄고 partId에 붙은 시뮬레이션.</summary>
        private static GameSimulation AttachedTo(string partId, HumanDefinition human = null)
        {
            var simulation = TestHumans.Simulation(Vector3.Zero, human: human);
            TestHumans.DisableReactions(simulation);
            TestHumans.PlaceNearPart(simulation, partId);
            simulation.Step(Attach);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            return simulation;
        }

        [Test]
        public void SessionRate_Forearm_Linear2To6Over6Seconds()
        {
            var simulation = AttachedTo("forearmR");
            var suck = Settings.Suck;
            float[] checkpoints = { 0f, 1.5f, 3f, 4.5f, 6f, 8f };
            int checkpoint = 0;

            for (int i = 0; i <= SecondsToTicks(8f); i++)
            {
                float elapsed = i * GameSimulation.DeltaTime;
                float before = simulation.Player.BloodGauge;
                simulation.Step(Suck);
                float measured = (simulation.Player.BloodGauge - before) / GameSimulation.DeltaTime;
                if (checkpoint < checkpoints.Length && Math.Abs(elapsed - checkpoints[checkpoint]) < GameSimulation.DeltaTime * 0.5f)
                {
                    float expected = suck.RateStart + ((suck.RateMax - suck.RateStart) * Math.Min(1f, elapsed / suck.RampTime));
                    Assert.That(measured, Is.EqualTo(expected).Within(0.01f), $"t={elapsed:F2}s");
                    checkpoint++;
                }
            }

            Assert.That(checkpoint, Is.EqualTo(checkpoints.Length));
            Assert.That(suck.RateStart, Is.EqualTo(2f));
            Assert.That(suck.RateMax, Is.EqualTo(6f));
            Assert.That(suck.RampTime, Is.EqualTo(6f));
        }

        [TestCase("forearmR", SkinSiteType.Forearm)]
        [TestCase("calfR", SkinSiteType.Calf)]
        [TestCase("head", SkinSiteType.Cheek)]
        public void SiteType_BloodAmountAndItch_FollowTable(string partId, SkinSiteType type)
        {
            var simulation = AttachedTo(partId);
            var site = TestHumans.Site(simulation, partId);
            Assert.That(site.Type, Is.EqualTo(type));

            simulation.Step(Suck);
            float firstTickRate = simulation.Player.BloodGauge / GameSimulation.DeltaTime;
            Run(simulation, Suck, SecondsToTicks(1f) - 1);

            Assert.That(firstTickRate, Is.EqualTo(Settings.Suck.RateStart * Settings.Sites.BloodAmount(type)).Within(1e-3f));
            Assert.That(site.Itch, Is.EqualTo(Settings.Suck.ItchRate * Settings.Sites.Sensitivity(type)).Within(0.05f));
        }

        [Test]
        public void SuckReleasedAndPressedAgain_KeepsAcceleration_DetachResets()
        {
            var simulation = AttachedTo("forearmR");
            Run(simulation, Suck, SecondsToTicks(3f));
            Run(simulation, PlayerCommand.None, SecondsToTicks(2f));

            float before = simulation.Player.BloodGauge;
            simulation.Step(Suck);
            float resumed = (simulation.Player.BloodGauge - before) / GameSimulation.DeltaTime;
            Assert.That(resumed, Is.EqualTo(simulation.Suck.SessionRate(3f, SkinSiteType.Forearm)).Within(0.01f), "acceleration kept while attached");
            Assert.That(resumed, Is.GreaterThan(3.9f));

            simulation.Step(Attach);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            Assert.That(simulation.Events.OfType<SuckSessionEnded>().Count(), Is.EqualTo(1));
            TestHumans.PlaceNearPart(simulation, "forearmR");
            simulation.Step(Attach);
            before = simulation.Player.BloodGauge;
            simulation.Step(Suck);
            float restarted = (simulation.Player.BloodGauge - before) / GameSimulation.DeltaTime;
            Assert.That(restarted, Is.EqualTo(Settings.Suck.RateStart).Within(0.01f), "new attachment = new session");
        }

        [Test]
        public void Suck_NotAttached_NoGain()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind);

            Run(simulation, Suck, SecondsToTicks(2f));

            Assert.That(simulation.Player.BloodGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Suck_AttachedToWall_NoGain()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0, 0, 10), new Vector3(50, 50, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 0, 8f));
            simulation.Step(Attach);

            Run(simulation, Suck, SecondsToTicks(2f));

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Assert.That(simulation.Player.BloodGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Suck_AttachedToClothedBodyPart_NoGain()
        {
            var simulation = AttachedTo("torso");

            Run(simulation, Suck, SecondsToTicks(2f));

            Assert.That(simulation.Player.BloodGauge, Is.EqualTo(0f));
        }

        [Test]
        public void SessionEnd_AmountAtLeast5_BiteMarkAndAwarenessPlus15()
        {
            var simulation = AttachedTo("forearmR");
            Run(simulation, Suck, SecondsToTicks(2.5f));
            Assert.That(simulation.Player.SuckSession.Amount, Is.GreaterThanOrEqualTo(Settings.BiteMark.MinAmount));
            float awarenessBefore = simulation.Human.Awareness;

            simulation.Step(Attach);

            var ended = simulation.Events.OfType<SuckSessionEnded>().Single();
            Assert.That(ended.BiteMark, Is.True);
            Assert.That(simulation.Human.BiteMarkCount, Is.EqualTo(1));
            Assert.That(TestHumans.Site(simulation, "forearmR").HasBiteMark, Is.True);
            Assert.That(simulation.Human.Awareness - awarenessBefore, Is.EqualTo(Settings.BiteMark.AwarenessBump).Within(1e-3f));
            Assert.That(Settings.BiteMark.AwarenessBump, Is.EqualTo(15f));
        }

        /// <summary>자국은 주둥이를 꽂은 자리에 생기고, 부위가 움직이면 피부에 붙은 채 따라간다 (spec/04 §4, M12).</summary>
        [Test]
        public void BiteMark_AtTheBiteSpot_FollowsThePart_OnePerSession()
        {
            var simulation = AttachedTo("forearmR");
            var human = simulation.Human;
            simulation.Player.Anchor.Resolve(out Vector3 spot, out _);
            Run(simulation, Suck, SecondsToTicks(2.5f));
            simulation.Step(Attach);

            Assert.That(human.BiteMarks.Count, Is.EqualTo(1));
            var mark = human.BiteMarks[0];
            mark.Resolve(out Vector3 position, out Vector3 normal);
            Assert.That(mark.PartId, Is.EqualTo("forearmR"));
            Assert.That(Vector3.Distance(position, spot), Is.LessThan(0.5f), "the mark sits where the mosquito bit");
            var snapshot = simulation.CaptureSnapshot().Human.BiteMarks.Single();
            Assert.That(Vector3.Distance(snapshot.Position, position), Is.LessThan(1e-3f));

            // 상체를 기울여 팔이 움직여도 자국은 팔뚝 피부 위에 있다.
            human.Pose.Posture = new PostureState { LeanDirection = Vector3.UnitZ, LeanAngle = 30f };
            human.UpdatePose();
            mark.Resolve(out Vector3 moved, out _);
            var forearm = human.Shapes["forearmR"];
            float fromSurface = Vector3.Distance(moved, ShapeGeometry.ClosestPointOnSegment(forearm.PointA, forearm.PointB, moved)) - forearm.Radius;
            Assert.That(Vector3.Distance(moved, position), Is.GreaterThan(1f), "the mark moved with the arm");
            Assert.That(Math.Abs(fromSurface), Is.LessThan(0.01f), "still on the forearm skin");

            // 같은 부위를 다시 물면 자국이 하나 더 생긴다.
            human.Pose.Posture = PostureState.Rest;
            human.UpdatePose();
            TestHumans.PlaceNearPart(simulation, "forearmR");
            simulation.Step(Attach);
            Run(simulation, Suck, SecondsToTicks(2.5f));
            simulation.Step(Attach);
            Assert.That(human.BiteMarks.Count, Is.EqualTo(2));
        }

        [Test]
        public void SessionEnd_AmountBelow5_NoBiteMark()
        {
            var simulation = AttachedTo("forearmR");
            Run(simulation, Suck, SecondsToTicks(1f));
            Assert.That(simulation.Player.SuckSession.Amount, Is.LessThan(Settings.BiteMark.MinAmount));

            simulation.Step(Attach);

            Assert.That(simulation.Events.OfType<SuckSessionEnded>().Single().BiteMark, Is.False);
            Assert.That(simulation.Human.BiteMarkCount, Is.EqualTo(0));
            Assert.That(simulation.Human.Awareness, Is.EqualTo(0f));
        }

        [TestCase(0, 1.0f, 1.0f, 0f, 1.0f)]
        [TestCase(1, 1.2f, 0.8f, 8f, 1.15f)]
        [TestCase(3, 1.6f, 0.5714286f, 24f, 1.45f)]
        [TestCase(6, 2.2f, 0.4f, 45f, 1.9f)]
        public void BiteMarks_ModifiersMatchFormula(int n, float gainMul, float decayMul, float floor, float reactionMul)
        {
            var bite = Settings.BiteMark;

            Assert.That(bite.GainMultiplier(n), Is.EqualTo(gainMul).Within(1e-5f));
            Assert.That(1f / bite.DecayDivisor(n), Is.EqualTo(decayMul).Within(1e-5f));
            Assert.That(bite.Floor(n), Is.EqualTo(floor).Within(1e-5f));
            Assert.That(bite.ReactionMultiplier(n), Is.EqualTo(reactionMul).Within(1e-5f));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(6)]
        public void BiteMarks_AppliedToAwarenessAndReactions(int n)
        {
            var bite = Settings.BiteMark;

            // 증가: 시각 1초 (Yellow 150u, 비행 소음 밖).
            var gain = TestHumans.Simulation(TestHumans.InFront(150f));
            gain.Human.BiteMarkCount = n;
            Run(gain, PlayerCommand.None, SecondsToTicks(1f));
            float baseRate = 16.5f;
            // 하한은 경계를 즉시 끌어올린다(경계는 항상 하한 이상). 그 위에 증가 배율이 붙은 시각 증가.
            Assert.That(gain.Human.Awareness, Is.EqualTo(bite.Floor(n) + (baseRate * bite.GainMultiplier(n))).Within(0.8f));

            // 감소와 하한: 자극 없이 50에서 시작.
            var decay = TestHumans.Simulation(TestHumans.FarBehind);
            decay.Human.BiteMarkCount = n;
            decay.Human.Awareness = 50f;
            Run(decay, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(decay.Human.Awareness, Is.EqualTo(50f - (Settings.Awareness.DecayRate / bite.DecayDivisor(n))).Within(0.2f));
            Run(decay, PlayerCommand.None, SecondsToTicks(30f));
            Assert.That(decay.Human.Awareness, Is.EqualTo(bite.Floor(n)).Within(1e-3f), "never decays below the floor");

            // 반응: 보정 배율.
            Assert.That(decay.HumanSystem.Reactions.Modifier(decay.Human), Is.EqualTo(bite.ReactionMultiplier(n)).Within(1e-5f));
        }

        [TestCase(0f, 1.0f, 1.0f)]
        [TestCase(50f, 0.8f, 0.875f)]
        [TestCase(100f, 0.6f, 0.75f)]
        public void Satiety_SpeedAndDashMultipliers(float gauge, float speedMul, float dashMul)
        {
            // 100%면 즉시 Stage Clear로 세계가 멈추므로, 배율은 함수로 확인하고 통합 이동은 99.99%로 본다.
            var flying = Empty();
            Assert.That(flying.Suck.SpeedMultiplier(gauge), Is.EqualTo(speedMul).Within(1e-5f));
            Assert.That(flying.Suck.DashMultiplier(gauge), Is.EqualTo(dashMul).Within(1e-5f));
            float playable = Math.Min(gauge, SuckSystem.GaugeMax - 0.01f);
            flying.Player.BloodGauge = playable;
            Run(flying, Forward, SecondsToTicks(1f));
            Assert.That(flying.Player.Velocity.Z / Settings.Flight.Speed, Is.EqualTo(speedMul).Within(1e-3f));

            var dashing = Empty();
            dashing.Player.BloodGauge = playable;
            dashing.Step(new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true });
            Run(dashing, PlayerCommand.None, SecondsToTicks(Settings.Dash.Duration) - 1);
            Assert.That(dashing.Player.Position.X / Settings.Dash.Distance, Is.EqualTo(dashMul).Within(1e-3f));
        }

        [Test]
        public void Gauge100_StageClearedExactlyOnce()
        {
            var simulation = AttachedTo("forearmR");
            simulation.Player.BloodGauge = 99.95f;

            int cleared = 0;
            StageCleared clearEvent = null;
            for (int i = 0; i < SecondsToTicks(2f); i++)
            {
                simulation.Step(Suck);
                var events = simulation.Events.OfType<StageCleared>().ToList();
                cleared += events.Count;
                clearEvent = clearEvent ?? events.FirstOrDefault();
            }

            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(simulation.Outcome, Is.EqualTo(StageOutcome.Cleared));
            Assert.That(simulation.Player.BloodGauge, Is.EqualTo(SuckSystem.GaugeMax));
            Assert.That(clearEvent.Result.BiteMarkCount, Is.EqualTo(0));
            Assert.That(clearEvent.Result.FrenzyCount, Is.EqualTo(0));
        }

        [Test]
        public void Cleared_InputIgnoredWorldFrozen()
        {
            var simulation = AttachedTo("forearmR");
            simulation.Player.BloodGauge = 99.99f;
            simulation.Step(Suck);
            Assert.That(simulation.Outcome, Is.EqualTo(StageOutcome.Cleared));
            Vector3 position = simulation.Player.Position;

            Run(simulation, new PlayerCommand { Move = new Vector2(0f, -1f), Vertical = 1f }, 30);

            Assert.That(simulation.Player.Position, Is.EqualTo(position));
        }

        [Test]
        public void RandomMotion_WhileSucking_SessionContinuesAndFollowsPart()
        {
            var simulation = AttachedTo("forearmR", TestHumans.Seated(actions: new[] { TestHumans.Scroll }));
            var forearm = simulation.Human.Shapes["forearmR"];
            Run(simulation, Suck, 10);
            simulation.HumanSystem.Motion.Start(simulation.Human, TestHumans.Scroll, simulation.Tick);
            Vector3 start = simulation.Player.Position;
            float gaugeBefore = simulation.Player.BloodGauge;

            Run(simulation, Suck, SecondsToTicks(TestHumans.Scroll.Duration * 0.5f));

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Assert.That(simulation.Player.SuckSession, Is.Not.Null, "session continues during the motion");
            Assert.That(simulation.Player.BloodGauge, Is.GreaterThan(gaugeBefore));
            Assert.That(Vector3.Distance(simulation.Player.Position, start), Is.GreaterThan(0.5f), "moved with the forearm");
            Assert.That(ShapeGeometry.Closest(forearm, simulation.Player.Position).Distance, Is.EqualTo(simulation.Player.CollisionRadius + SphereMover.Skin).Within(0.02f));
        }

        [Test]
        public void Dislodged_WhileSucking_SessionEndsWithoutDeath()
        {
            var simulation = AttachedTo("forearmR", TestHumans.Seated(actions: new[] { TestHumans.GrabTissue }));
            Run(simulation, Suck, SecondsToTicks(2.5f));
            simulation.HumanSystem.Motion.Start(simulation.Human, TestHumans.GrabTissue, simulation.Tick);

            SuckSessionEnded ended = null;
            for (int i = 0; i < SecondsToTicks(TestHumans.GrabTissue.Duration) && ended == null; i++)
            {
                simulation.Step(Suck);
                ended = simulation.Events.OfType<SuckSessionEnded>().SingleOrDefault();
                if (ended != null)
                {
                    Assert.That(simulation.Events.OfType<PlayerDislodged>().Count(), Is.EqualTo(1), "ended by the dislodge in the same tick");
                }
            }

            Assert.That(ended, Is.Not.Null);
            Assert.That(simulation.Player.SuckSession, Is.Null);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dislodged));
            Assert.That(ended.BiteMark, Is.True, "a long enough session still leaves a mark");
            Assert.That(simulation.Outcome, Is.EqualTo(StageOutcome.InProgress));
        }
    }
}
