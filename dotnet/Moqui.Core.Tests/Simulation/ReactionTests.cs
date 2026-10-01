using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Random;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class ReactionTests
    {
        private const int Trials = 10000;
        private const double ProbabilityTolerance = 0.02;

        private static PlayerCommand Attach => new PlayerCommand { AttachPressed = true };

        private static ReactionSystem NewReactions(ulong seed)
        {
            var settings = Settings;
            var attacks = new HumanAttackSystem(settings.Attack, settings.Frenzy, new SplitMix64Random(seed));
            return new ReactionSystem(settings, attacks, new SplitMix64Random(seed));
        }

        [TestCase(SkinSiteType.Forearm)]
        [TestCase(SkinSiteType.Calf)]
        [TestCase(SkinSiteType.FootTop)]
        [TestCase(SkinSiteType.Neck)]
        [TestCase(SkinSiteType.Cheek)]
        public void LandingReaction_10000Trials_MatchesLandChanceTimesSensitivity(SkinSiteType type)
        {
            var reactions = NewReactions(seed: 7UL + (ulong)type);
            float sensitivity = Settings.Sites.Sensitivity(type);

            int hits = 0;
            for (int i = 0; i < Trials; i++)
            {
                if (reactions.RollLanding(sensitivity, 1f))
                {
                    hits++;
                }
            }

            double expected = Settings.Reaction.LandChance * sensitivity;
            Assert.That((double)hits / Trials, Is.EqualTo(expected).Within(ProbabilityTolerance));
        }

        [Test]
        public void LandingReaction_SameSeed_SameOutcomes()
        {
            var a = NewReactions(99UL);
            var b = NewReactions(99UL);

            var first = Enumerable.Range(0, 1000).Select(_ => a.RollLanding(1f, 1f)).ToList();
            var second = Enumerable.Range(0, 1000).Select(_ => b.RollLanding(1f, 1f)).ToList();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void AttachedHazard_GrowsWithItchSquared()
        {
            var reactions = NewReactions(1UL);
            var reaction = Settings.Reaction;

            float atZero = reactions.AttachedHazard(1f, 0f, 1f);
            float atHalf = reactions.AttachedHazard(1f, 50f, 1f);
            float atFull = reactions.AttachedHazard(1f, 100f, 1f);

            Assert.That(atZero, Is.EqualTo(reaction.BaseRate).Within(1e-6f));
            Assert.That(atFull, Is.EqualTo(reaction.BaseRate + reaction.ItchRate).Within(1e-6f));
            Assert.That((atHalf - atZero) / (atFull - atZero), Is.EqualTo(0.25f).Within(1e-5f), "itch² proportional");
            Assert.That(reactions.AttachedHazard(2.2f, 50f, 1f), Is.EqualTo(atHalf * 2.2f).Within(1e-5f), "proportional to sensitivity");
        }

        [Test]
        public void AttachedHazard_PerTickRolls_MatchExponentialProbability()
        {
            var random = new SplitMix64Random(2024UL);
            double hazard = NewReactions(1UL).AttachedHazard(1f, 100f, 1f);
            double perTick = ReactionSystem.TickProbability(hazard, GameSimulation.DeltaTime);
            const int ticks = 200000;

            int hits = 0;
            for (int i = 0; i < ticks; i++)
            {
                if (random.Chance(perTick))
                {
                    hits++;
                }
            }

            Assert.That(perTick, Is.EqualTo(1.0 - Math.Exp(-hazard / GameSimulation.TickRate)).Within(1e-8));
            Assert.That(hits, Is.EqualTo(ticks * perTick).Within(ticks * perTick * 0.1));
        }

        [Test]
        public void Attached_Itch100_ReactsImmediately()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            TestHumans.PlaceNearPart(simulation, "forearmR");
            simulation.Step(Attach);
            TestHumans.Site(simulation, "forearmR").Itch = 100f;
            simulation.Human.Attack.Phase = AttackPhase.Idle;

            simulation.Step(PlayerCommand.None);

            var telegraph = simulation.Events.OfType<AttackTelegraphStarted>().Single();
            Assert.That(telegraph.Kind, Is.EqualTo(AttackKind.ReactSlap));
            Assert.That(telegraph.Target, Is.EqualTo(simulation.Player.Position));
        }

        [Test]
        public void EarZone_Airborne_ProducesEarReactionsAtEarRate()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            var reactions = simulation.HumanSystem.Reactions;
            var human = simulation.Human;
            simulation.Player.Position = human.RightEar + new Vector3(8f, 0f, 0f);
            var perception = new HumanPerception { InEarZone = true };
            var events = new List<SimulationEvent>();
            const int seconds = 120;

            int reactionsCount = 0;
            for (int tick = 0; tick < seconds * GameSimulation.TickRate; tick++)
            {
                events.Clear();
                reactions.Step(human, simulation.Player, perception, tick, GameSimulation.DeltaTime, events);
                var started = events.OfType<AttackTelegraphStarted>().ToList();
                reactionsCount += started.Count;
                Assert.That(started.All(e => e.Kind == AttackKind.ReactSlap && e.Target == simulation.Player.Position), Is.True);
                human.Attack.Phase = AttackPhase.Idle;
            }

            double expected = Settings.Reaction.EarRate * seconds;
            Assert.That(reactionsCount, Is.EqualTo(expected).Within(expected * 0.5));
        }

        [Test]
        public void ReactSlap_PlayerLeavesDuringTelegraph_Survives()
        {
            var simulation = ReactionInProgress();

            simulation.Step(new PlayerCommand { Move = new Vector2(1f, 0f) });
            simulation.Step(new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true });
            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(simulation.Player.State, Is.Not.EqualTo(PlayerState.Dead));
        }

        [Test]
        public void ReactSlap_PlayerStays_DiesWithAttackCause()
        {
            var simulation = ReactionInProgress();
            int telegraphTicks = SecondsToTicks(Settings.Attack.SelfSlapTelegraph);
            PlayerDied death = null;

            for (int i = 0; i < SecondsToTicks(1f) && death == null; i++)
            {
                simulation.Step(PlayerCommand.None);
                death = simulation.Events.OfType<PlayerDied>().SingleOrDefault();
            }

            Assert.That(death, Is.Not.Null);
            Assert.That(death.Cause, Is.EqualTo(DeathCause.Attack));
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dead));
            Assert.That(death.Tick - simulation.Human.Attack.TelegraphEndTick, Is.EqualTo(0), "dies when the hit activates after the telegraph");
            Assert.That(telegraphTicks, Is.GreaterThan(0));
        }

        [Test]
        public void Clap_PlayerLeavesRedZoneDuringTelegraph_Survives()
        {
            var simulation = TestHumans.Simulation(TestHumans.InFront(30f));
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Human.Attack.Kind, Is.EqualTo(AttackKind.Clap));

            simulation.Step(new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true });
            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Attack.ClapTelegraph + 0.2f));

            Assert.That(simulation.Player.State, Is.Not.EqualTo(PlayerState.Dead));
        }

        [Test]
        public void Clap_PlayerStays_Dies()
        {
            var simulation = TestHumans.Simulation(TestHumans.InFront(30f));

            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Attack.ClapTelegraph + 0.2f));

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dead));
        }

        [Test]
        public void AttackInProgress_NoNewAttackOrReactionUntilRecoveryEnds()
        {
            // 박수 진행 중에 피부에 붙어 가려움 100 → 반응이 버려진다.
            var simulation = TestHumans.Simulation(TestHumans.InFront(30f));
            simulation.Step(PlayerCommand.None);
            var clap = simulation.Human.Attack;
            int recoveryEnd = clap.RecoveryEndTick;
            TestHumans.PlaceNearPart(simulation, "calfR");
            simulation.Step(Attach);
            TestHumans.Site(simulation, "calfR").Itch = 100f;

            int startedDuringBusy = 0;
            while (simulation.Tick < recoveryEnd)
            {
                simulation.Step(PlayerCommand.None);
                startedDuringBusy += simulation.Events.OfType<AttackTelegraphStarted>().Count();
            }

            Assert.That(startedDuringBusy, Is.EqualTo(0));
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Events.OfType<AttackTelegraphStarted>().Count(), Is.EqualTo(1), "the next attack or reaction starts once recovery ends");
        }

        /// <summary>오른쪽 팔뚝에 붙은 플레이어에게 반응 때리기 예고가 막 시작된 상태.</summary>
        private static GameSimulation ReactionInProgress()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            TestHumans.PlaceNearPart(simulation, "forearmR");
            simulation.Step(Attach);
            simulation.Human.Attack.Phase = AttackPhase.Idle;
            TestHumans.Site(simulation, "forearmR").Itch = 100f;
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Human.Attack.Kind, Is.EqualTo(AttackKind.ReactSlap));
            TestHumans.Site(simulation, "forearmR").Itch = 0f;
            return simulation;
        }
    }
}
