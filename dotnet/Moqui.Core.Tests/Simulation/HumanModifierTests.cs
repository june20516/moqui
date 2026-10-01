using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class HumanModifierTests
    {
        private static readonly Vector3 Head = new Vector3(0f, 115f, 140f);

        /// <summary>거실 인간은 −Z(TV)를 본다. 정면 distance 지점.</summary>
        private static Vector3 InFront(float distance) => Head + new Vector3(0f, 0f, -distance);

        private static Vector3 FarAway => new Vector3(-240f, 120f, -190f);

        private static LevelDefinition Level(string id) => new LevelLoader(FileSystemDataSource.ForRepoData()).Load(id);

        private static GameSimulation Stage(string id, Vector3 playerPosition, HumanTraits traits = null)
        {
            var level = Level(id);
            var human = level.Human;
            if (traits != null)
            {
                human = new HumanDefinition(human.Id, human.Position, human.FacingYaw, human.Parts, human.HeadPartId, human.ShoulderLocals, human.IdleLookYaws, human.Actions, traits);
            }

            var setup = new SimulationSetup(level.CreateWorld, playerPosition, human, level.Seed, level.DripSources);
            return new GameSimulation(Settings, setup);
        }

        [Test]
        public void Doze_SleepingInRedZone_NoVisionAndNoClap()
        {
            var simulation = Stage("stage01", InFront(30f));
            Assert.That(simulation.Human.Doze, Is.EqualTo(DozeState.Sleeping));

            int telegraphs = 0;
            for (int i = 0; i < SecondsToTicks(1f); i++)
            {
                simulation.Step(PlayerCommand.None);
                telegraphs += simulation.Events.OfType<AttackTelegraphStarted>().Count();
                Assert.That(simulation.Human.PlayerVisible, Is.False, $"tick {i}");
            }

            Assert.That(simulation.Human.Doze, Is.EqualTo(DozeState.Sleeping));
            Assert.That(telegraphs, Is.EqualTo(0));
            Assert.That(simulation.Human.State, Is.Not.EqualTo(AwarenessState.Frenzy));

            var awake = Stage("stage02", InFront(30f));
            awake.Step(PlayerCommand.None);
            Assert.That(awake.Events.OfType<AttackTelegraphStarted>().Single().Kind, Is.EqualTo(AttackKind.Clap), "the same spot is deadly when awake");
        }

        [Test]
        public void Doze_CycleFollowsTuningRanges_WakeTelegraph05SecondsBefore()
        {
            var simulation = Stage("stage01", FarAway);
            var doze = Settings.Doze;
            var changes = new List<(int Tick, DozeState To)>();
            var telegraphs = new List<int>();

            for (int i = 0; i < SecondsToTicks(150f); i++)
            {
                simulation.Step(PlayerCommand.None);
                changes.AddRange(simulation.Events.OfType<DozeStateChanged>().Select(e => (e.Tick, e.To)));
                telegraphs.AddRange(simulation.Events.OfType<DozeWakeTelegraph>().Select(e => e.Tick));
            }

            Assert.That(changes.Count, Is.GreaterThanOrEqualTo(10));
            int previousTick = 0;
            var previousState = DozeState.Sleeping;
            foreach (var (tick, to) in changes)
            {
                float seconds = (tick - previousTick) * GameSimulation.DeltaTime;
                var range = previousState == DozeState.Sleeping ? doze.SleepDuration : doze.WakeDuration;
                Assert.That(seconds, Is.InRange(range.Min - 0.02f, range.Max + 0.02f), $"{previousState} lasted {seconds:F2}s");
                if (to == DozeState.Blinking)
                {
                    Assert.That(telegraphs, Does.Contain(tick - SecondsToTicks(doze.WakeTelegraph)), $"wake at {tick} has a telegraph 0.5s before");
                }

                previousTick = tick;
                previousState = to;
            }
        }

        [Test]
        public void Doze_FrenzyMinimumHalved_CalmsSoonerThanAwakeHuman()
        {
            var dozing = Stage("stage01", FarAway);
            var normal = Stage("stage01", FarAway, HumanTraits.None);
            foreach (var simulation in new[] { dozing, normal })
            {
                simulation.Human.Awareness = 100f;
                simulation.Human.LastStimulusTick = simulation.Tick;
                simulation.Step(PlayerCommand.None);
                Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));
            }

            Assert.That(Settings.Frenzy.MinDuration * Settings.Doze.FrenzyDurationMul, Is.EqualTo(4f));
            Run(dozing, PlayerCommand.None, SecondsToTicks(Settings.Frenzy.CalmTime));
            Run(normal, PlayerCommand.None, SecondsToTicks(Settings.Frenzy.CalmTime));

            Assert.That(dozing.Human.State, Is.EqualTo(AwarenessState.Suspicious), "minimum 4s already passed, calms at 6s unseen");
            Assert.That(normal.Human.State, Is.EqualTo(AwarenessState.Frenzy), "minimum 8s still holds");
        }

        [Test]
        public void Doze_Sleeping_HearingAndReactionsHalved()
        {
            // 오른쪽 귀 옆 20u 위: 비행 소음 반경 안, 시야 밖.
            Vector3 nearEar = Head + new Vector3(-30f, 0f, 0f);
            var dozing = Stage("stage01", nearEar);
            var normal = Stage("stage01", nearEar, HumanTraits.None);

            Run(dozing, PlayerCommand.None, 30);
            Run(normal, PlayerCommand.None, 30);

            Assert.That(dozing.Human.Doze, Is.EqualTo(DozeState.Sleeping));
            Assert.That(dozing.Human.Awareness / normal.Human.Awareness, Is.EqualTo(Settings.Doze.HearingMul).Within(0.01f));
            Assert.That(dozing.HumanSystem.Reactions.Modifier(dozing.Human), Is.EqualTo(Settings.Doze.ReactionMul).Within(1e-5f));
            Assert.That(normal.HumanSystem.Reactions.Modifier(normal.Human), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void Doze_AwarenessAtSuspicion_FullyWakes_ThenResleepsAfter5CalmSeconds()
        {
            var simulation = Stage("stage01", FarAway);
            simulation.Human.Awareness = 45f;
            simulation.Human.LastStimulusTick = simulation.Tick;
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Human.Doze, Is.EqualTo(DozeState.Awake));

            int calmTick = -1;
            for (int i = 0; i < SecondsToTicks(30f) && simulation.Human.Doze == DozeState.Awake; i++)
            {
                simulation.Step(PlayerCommand.None);
                if (calmTick < 0 && simulation.Human.State == AwarenessState.Safe)
                {
                    calmTick = simulation.Tick;
                }
            }

            Assert.That(simulation.Human.Doze, Is.EqualTo(DozeState.Sleeping));
            Assert.That((simulation.Tick - calmTick) * GameSimulation.DeltaTime, Is.EqualTo(Settings.Doze.ResleepDelay).Within(0.1f));
        }

        [Test]
        public void Stage2_Safe_GlancesSideways60DegreesFor2SecondsEvery6To10Seconds()
        {
            var simulation = Stage("stage02", FarAway);
            var glance = Level("stage02").Human.Traits.Glance;
            var starts = new List<int>();
            float maxAbsYaw = 0f;

            for (int i = 0; i < SecondsToTicks(60f); i++)
            {
                int before = simulation.Human.GlanceEndTick;
                simulation.Step(PlayerCommand.None);
                if (before == Player.NeverTick && simulation.Human.GlanceEndTick != Player.NeverTick)
                {
                    starts.Add(simulation.Tick - 1);
                    Assert.That(System.Math.Abs(simulation.Human.GlanceYaw), Is.EqualTo(60f));
                    Assert.That((simulation.Human.GlanceEndTick - (simulation.Tick - 1)) * GameSimulation.DeltaTime, Is.EqualTo(glance.Duration).Within(0.02f));
                }

                maxAbsYaw = System.Math.Max(maxAbsYaw, System.Math.Abs(simulation.Human.HeadYaw));
            }

            Assert.That(starts.Count, Is.GreaterThanOrEqualTo(5));
            foreach (int gap in starts.Zip(starts.Skip(1), (a, b) => b - a))
            {
                Assert.That(gap * GameSimulation.DeltaTime, Is.InRange(6f - 0.02f, 10f + 0.02f));
            }

            Assert.That(maxAbsYaw, Is.EqualTo(60f).Within(0.5f), "the head actually reaches the glance angle");
        }

        [Test]
        public void Breathing_PeriodAndExhaleFollowTuning_DrunkStrength16x()
        {
            var simulation = Stage("stage02", FarAway);
            var breath = Settings.Breath;
            int exhaleTicks = 0;
            var exhaleStarts = new List<int>();
            bool wasExhaling = false;

            for (int i = 0; i < SecondsToTicks(breath.Period * 3f); i++)
            {
                simulation.Step(PlayerCommand.None);
                bool exhaling = simulation.Human.IsExhaling;
                if (exhaling)
                {
                    exhaleTicks++;
                    Assert.That(simulation.Human.ExhaleStrength, Is.EqualTo(breath.Co2Strength));
                    Assert.That(Vector3.Distance(simulation.Human.ExhalePosition, simulation.Human.HeadCenter), Is.EqualTo(simulation.Human.HeadShape.Radius).Within(1e-3f));
                }
                else
                {
                    Assert.That(simulation.Human.ExhaleStrength, Is.EqualTo(0f));
                }

                if (exhaling && !wasExhaling)
                {
                    exhaleStarts.Add(simulation.Tick - 1);
                }

                wasExhaling = exhaling;
            }

            Assert.That(exhaleTicks * GameSimulation.DeltaTime, Is.EqualTo(breath.ExhaleDuration * 3f).Within(0.05f));
            Assert.That((exhaleStarts[1] - exhaleStarts[0]) * GameSimulation.DeltaTime, Is.EqualTo(breath.Period).Within(0.02f));

            var drunk = Stage("stage02", FarAway, new HumanTraits(new[] { HumanModifier.Drunk }, false, null));
            Assert.That(drunk.HumanSystem.Breath.Strength(drunk.Human) / breath.Co2Strength, Is.EqualTo(1.6f).Within(1e-5f));
        }

        [Test]
        public void Snapshot_ContainsSensesSourceData()
        {
            var simulation = Stage("stage01", FarAway);
            Run(simulation, PlayerCommand.None, 10);

            var snapshot = simulation.CaptureSnapshot();

            Assert.That(snapshot.Human.BreathPhase, Is.InRange(0f, 1f));
            Assert.That(snapshot.Human.IsExhaling, Is.True, "first 1.5s of the cycle");
            Assert.That(snapshot.Human.ExhaleStrength, Is.GreaterThan(0f));
            Assert.That(snapshot.Human.ExhalePosition, Is.EqualTo(simulation.Human.ExhalePosition));
            Assert.That(snapshot.Human.SkinSites.Select(s => s.Type).OrderBy(t => t), Is.EqualTo(new[] { SkinSiteType.Forearm, SkinSiteType.Forearm, SkinSiteType.Calf, SkinSiteType.Calf, SkinSiteType.Neck }.OrderBy(t => t)));
            Assert.That(snapshot.Human.SkinSites.All(s => s.Radius > 0f && s.Position != Vector3.Zero), Is.True);
            Assert.That(snapshot.ShadowZones.Select(z => z.Id), Is.EquivalentTo(new[] { "shadow_coffee_table", "shadow_bookshelf", "shadow_curtain" }));
            Assert.That(snapshot.WindZones, Is.Empty, "no fans in the living room");
            Assert.That(snapshot.Player.IsHidden, Is.False);
            Assert.That(snapshot.PlayerVisibleToHuman, Is.False);
            Assert.That(snapshot.Human.Doze, Is.EqualTo(DozeState.Sleeping));
        }
    }
}
