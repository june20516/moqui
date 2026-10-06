using System.Numerics;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class HumanAwarenessTests
    {
        [Test]
        public void StateTransitions_Hysteresis_Enter40Exit20()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind);
            var human = simulation.Human;

            human.Awareness = 39f;
            simulation.Step(PlayerCommand.None);
            Assert.That(human.State, Is.EqualTo(AwarenessState.Safe), "39 stays Safe");

            TestHumans.Provoke(simulation, 45f);
            simulation.Step(PlayerCommand.None);
            Assert.That(human.State, Is.EqualTo(AwarenessState.Suspicious), "≥40 enters Suspicious");

            human.Awareness = 25f;
            simulation.Step(PlayerCommand.None);
            Assert.That(human.State, Is.EqualTo(AwarenessState.Suspicious), "25 stays Suspicious (hysteresis)");

            human.Awareness = 19.5f;
            simulation.Step(PlayerCommand.None);
            Assert.That(human.State, Is.EqualTo(AwarenessState.Safe), "<20 returns to Safe");

            TestHumans.Provoke(simulation, 100f);
            simulation.Step(PlayerCommand.None);
            Assert.That(human.State, Is.EqualTo(AwarenessState.Frenzy), "100 enters Frenzy");
        }

        [Test]
        public void NoStimulus_DecaysAfter2SecondsAt10PerSecond()
        {
            // 대시 소음 1회(+40) 뒤 자극 없음.
            var simulation = TestHumans.Simulation(TestHumans.Head + new Vector3(0f, 0f, -100f));
            simulation.Step(TestHumans.DashUp);
            float afterNoise = simulation.Human.Awareness;
            var awareness = Settings.Awareness;
            float perTick = awareness.DecayRate * GameSimulation.DeltaTime;

            Run(simulation, PlayerCommand.None, SecondsToTicks(awareness.DecayDelay) - 1);
            Assert.That(simulation.Human.Awareness, Is.EqualTo(afterNoise), "no decay before 2s");

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(simulation.Human.Awareness, Is.EqualTo(afterNoise - awareness.DecayRate).Within(perTick * 1.5f));
        }

        [Test]
        public void NoStimulus_PlayerHidden_UsesShadowDecayRate()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind, ShadowWorld(TestHumans.FarBehind, 20f));
            simulation.Human.Awareness = 50f;

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(simulation.Human.Awareness, Is.EqualTo(50f - Settings.Awareness.ShadowDecayRate).Within(0.5f));
        }

        [Test]
        public void Head_StimulusBehind_NeverExceedsYawLimit()
        {
            var simulation = TestHumans.Simulation(TestHumans.Head + new Vector3(0f, 0f, -100f));
            TestHumans.Provoke(simulation, 45f);
            float limit = Settings.Head.YawLimit;

            simulation.Step(TestHumans.DashUp);
            for (int i = 0; i < SecondsToTicks(3f); i++)
            {
                simulation.Step(PlayerCommand.None);
                Assert.That(System.Math.Abs(simulation.Human.HeadYaw), Is.LessThanOrEqualTo(limit));
            }

            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Suspicious));
            Assert.That(System.Math.Abs(simulation.Human.HeadYaw), Is.EqualTo(limit).Within(1e-3f), "head reached the limit while looking back");
        }

        /// <summary>머리는 가속해 출발하고(최고 속도까지 head.turnAccelTime), 남은 각에 맞춰 감속해 넘치지 않고 멈춘다 (M14).</summary>
        [Test]
        public void Head_IdlePattern_EasesInAndOut_NoOvershoot()
        {
            var definition = TestHumans.Seated(idleLookYaws: new[] { 60f, -60f });
            var simulation = TestHumans.Simulation(TestHumans.FarBehind, human: definition);
            float speed = Settings.Head.IdleTurnSpeed;
            float accelTime = Settings.Head.TurnAccelTime;

            Run(simulation, PlayerCommand.None, SecondsToTicks(0.1f));
            Assert.That(simulation.Human.HeadYaw, Is.LessThan(speed * 0.1f * 0.6f), "starts slowly");

            Run(simulation, PlayerCommand.None, SecondsToTicks(0.4f));
            Assert.That(simulation.Human.HeadYaw, Is.EqualTo(speed * (0.5f - (accelTime * 0.5f))).Within(1.5f), "then turns at idle speed");

            float maxYaw = 0f;
            float lastVelocity = 0f;
            for (int i = 0; i < SecondsToTicks(1f); i++)
            {
                simulation.Step(PlayerCommand.None);
                maxYaw = System.Math.Max(maxYaw, simulation.Human.HeadYaw);
                if (simulation.Human.HeadYaw >= 59.9f)
                {
                    break;
                }

                lastVelocity = simulation.Human.HeadYawVelocity;
            }

            Assert.That(maxYaw, Is.LessThanOrEqualTo(60f + 1e-3f), "no overshoot");
            Assert.That(lastVelocity, Is.LessThan(speed * 0.5f), "slows down before stopping");
        }
    }
}
