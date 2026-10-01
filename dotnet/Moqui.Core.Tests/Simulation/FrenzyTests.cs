using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class FrenzyTests
    {
        /// <summary>시야 안(Yellow), 사거리(어깨 120u) 밖.</summary>
        private static Vector3 SeenOutOfReach => TestHumans.InFront(200f);

        [Test]
        public void Frenzy_NeverSeen_LastsMinimum8SecondsThenCalmsTo60()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind);
            var frenzy = Settings.Frenzy;
            TestHumans.Provoke(simulation, 100f);
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));

            Run(simulation, PlayerCommand.None, SecondsToTicks(frenzy.MinDuration) - 2);
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy), "still frenzied before 8s");

            Run(simulation, PlayerCommand.None, 2);
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Suspicious));
            Assert.That(simulation.Human.Awareness, Is.EqualTo(frenzy.ExitValue));
        }

        [Test]
        public void Frenzy_SeenUntil10Seconds_CalmsAfter6UnseenSeconds()
        {
            var simulation = TestHumans.Simulation(SeenOutOfReach);
            TestHumans.Provoke(simulation, 100f);
            Run(simulation, PlayerCommand.None, SecondsToTicks(10f));
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));

            simulation.Player.Position = TestHumans.FarBehind;
            int ticks = 0;
            while (simulation.Human.State == AwarenessState.Frenzy && ticks < SecondsToTicks(20f))
            {
                simulation.Step(PlayerCommand.None);
                ticks++;
            }

            Assert.That(ticks * GameSimulation.DeltaTime, Is.EqualTo(Settings.Frenzy.CalmTime).Within(GameSimulation.DeltaTime * 2));
        }

        [Test]
        public void Frenzy_SeenAgainWhileHidden_ResetsUnseenTimer()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind);
            TestHumans.Provoke(simulation, 100f);
            simulation.Step(PlayerCommand.None);

            Run(simulation, PlayerCommand.None, SecondsToTicks(5f));
            simulation.Player.Position = SeenOutOfReach;
            Run(simulation, PlayerCommand.None, SecondsToTicks(0.5f));
            Assert.That(simulation.Human.UnseenTicks, Is.EqualTo(0), "seeing resets the unseen timer");

            simulation.Player.Position = TestHumans.FarBehind;
            Run(simulation, PlayerCommand.None, SecondsToTicks(5f));
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy), "only 5s unseen since last sighting");

            Run(simulation, PlayerCommand.None, SecondsToTicks(1.2f));
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Suspicious));
        }

        [Test]
        public void Frenzy_PlayerNotVisible_BlindSwatsAroundLastSeenPosition()
        {
            Vector3 lastSeen = TestHumans.InFront(60f);
            var simulation = TestHumans.Simulation(lastSeen);
            TestHumans.Provoke(simulation, 100f);
            simulation.Step(PlayerCommand.None);
            simulation.Player.Position = TestHumans.FarBehind;

            var swats = new List<AttackTelegraphStarted>();
            for (int i = 0; i < SecondsToTicks(5f); i++)
            {
                simulation.Step(PlayerCommand.None);
                swats.AddRange(simulation.Events.OfType<AttackTelegraphStarted>().Where(e => e.Kind == AttackKind.BlindSwat));
            }

            Assert.That(swats.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(swats.All(e => Vector3.Distance(e.Target, lastSeen) <= Settings.Frenzy.BlindSwatRadius), Is.True);
            Assert.That(simulation.Player.State, Is.Not.EqualTo(PlayerState.Dead));
        }

        [Test]
        public void Frenzy_EntryCounted()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind);

            TestHumans.Provoke(simulation, 100f);
            Run(simulation, PlayerCommand.None, SecondsToTicks(9f));
            TestHumans.Provoke(simulation, 100f);
            simulation.Step(PlayerCommand.None);

            Assert.That(simulation.Human.FrenzyCount, Is.EqualTo(2));
        }

        [Test]
        public void SameSeedAndCommands_HumanBehaviourReplaysExactly()
        {
            var first = Record(TestHumans.DefaultSeed);
            var second = Record(TestHumans.DefaultSeed);
            var otherSeed = Record(TestHumans.DefaultSeed + 1);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(otherSeed, Is.Not.EqualTo(first), "blind swat targets depend on the seed");
        }

        private static List<string> Record(ulong seed)
        {
            var simulation = TestHumans.Simulation(TestHumans.InFront(100f), seed: seed);
            var log = new List<string>();
            TestHumans.Provoke(simulation, 100f);
            for (int i = 0; i < SecondsToTicks(6f); i++)
            {
                if (i == 1)
                {
                    simulation.Player.Position = TestHumans.FarBehind;
                }

                simulation.Step(new PlayerCommand { LookYaw = i % 360 });
                var human = simulation.Human;
                log.Add($"{simulation.Tick}:{human.Awareness:R}:{human.HeadYaw:R}:{human.HeadPitch:R}:{human.State}");
                log.AddRange(simulation.Events.OfType<AttackTelegraphStarted>().Select(e => $"atk:{e.Kind}:{e.Target.X:R},{e.Target.Y:R},{e.Target.Z:R}"));
            }

            return log;
        }
    }
}
