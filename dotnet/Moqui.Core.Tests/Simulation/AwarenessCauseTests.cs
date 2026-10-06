using System.Linq;
using System.Numerics;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>왜 들켰나 (gulf §10, D-067): 광분 순간 최근 경계를 가장 많이 올린 원인을 낸다.</summary>
    public class AwarenessCauseTests
    {
        [Test]
        public void StayingInSight_FrenzyCauseIsSight()
        {
            var simulation = TestHumans.Simulation(TestHumans.InFront(120f));

            for (int i = 0; i < SecondsToTicks(30f) && simulation.Human.State != AwarenessState.Frenzy; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));
            Assert.That(simulation.Human.LastFrenzyCause, Is.EqualTo(AwarenessCause.Sight));
        }

        [Test]
        public void DashingBehindTheHead_FrenzyCauseIsHearing()
        {
            var simulation = TestHumans.Simulation(TestHumans.Head + new Vector3(0f, 0f, -60f));
            FrenzyTriggered triggered = null;
            for (int i = 0; i < SecondsToTicks(60f) && triggered == null; i++)
            {
                // 머리 뒤에서 제자리 대시를 반복한다(시야 밖, 소음만).
                simulation.Player.Position = TestHumans.Head + new Vector3(0f, 0f, -60f);
                simulation.Player.Stamina = simulation.Settings.Stamina.Max;
                simulation.Step(TestHumans.DashUp);
                triggered = simulation.Events.OfType<FrenzyTriggered>().FirstOrDefault();
            }

            Assert.That(triggered, Is.Not.Null);
            Assert.That(triggered.Cause, Is.EqualTo(AwarenessCause.Hearing));
        }

        [Test]
        public void Memory_ForgetsOldCauses()
        {
            var memory = new AwarenessCauseMemory();
            memory.Add(AwarenessCause.Sight, 60f);
            memory.Decay(12f, 4f);
            memory.Add(AwarenessCause.Hearing, 5f);

            Assert.That(memory.Dominant, Is.EqualTo(AwarenessCause.Hearing), "60 · e^-3 ≈ 3 < 5");
            Assert.That(new AwarenessCauseMemory().Dominant, Is.EqualTo(AwarenessCause.None));
        }
    }
}
