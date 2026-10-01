using System.Numerics;
using Moqui.Core.Simulation;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class StaminaTests
    {
        private static PlayerCommand DashUp => new PlayerCommand { DashPressed = true };

        [Test]
        public void Stamina_AfterDash_RegeneratesFrom1SecondAt20PerSecond()
        {
            var simulation = Empty();
            var stamina = Settings.Stamina;
            float perTick = stamina.RegenRate * GameSimulation.DeltaTime;

            simulation.Step(DashUp);
            float afterSpend = simulation.Player.Stamina;
            Run(simulation, PlayerCommand.None, SecondsToTicks(stamina.RegenDelay) - 1);

            Assert.That(simulation.Player.Stamina, Is.EqualTo(afterSpend).Within(perTick), "no regen before delay");

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(simulation.Player.Stamina, Is.EqualTo(afterSpend + stamina.RegenRate).Within(perTick * 1.5f));
        }

        [Test]
        public void Stamina_Regen_ClampsAtMax()
        {
            var simulation = Empty();
            simulation.Step(DashUp);

            Run(simulation, PlayerCommand.None, SecondsToTicks(10f));

            Assert.That(simulation.Player.Stamina, Is.EqualTo(Settings.Stamina.Max));
        }

        [Test]
        public void Stamina_ReachesZero_Exhausted2SecondsWithHalfSpeedAndNoDash()
        {
            var simulation = Empty();
            simulation.Player.Stamina = Settings.Dash.StaminaCost;
            simulation.Step(DashUp);
            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Dash.Cooldown));

            Assert.That(simulation.Player.Stamina, Is.EqualTo(0f));
            Assert.That(simulation.Player.IsExhausted, Is.True);

            // 탈진 중: 스태미나가 회복되어도 대시 불가, 최고 속도는 50%.
            simulation.Player.Stamina = Settings.Stamina.Max;
            simulation.Step(DashUp);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying), "dash while exhausted");

            Run(simulation, Forward, SecondsToTicks(0.5f));
            Assert.That(simulation.Player.Velocity.Z, Is.EqualTo(Settings.Flight.Speed * Settings.Stamina.ExhaustedSpeedMul).Within(1e-3f));

            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Stamina.ExhaustedDuration));
            Assert.That(simulation.Player.IsExhausted, Is.False);
            simulation.Step(DashUp);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dashing), "dash after exhaustion ends");
        }

        [Test]
        public void Exhaustion_WhileHidden_RecoversTwiceAsFast()
        {
            var simulation = WithWorld(ShadowWorld(Vector3.Zero), Vector3.Zero);
            simulation.Player.Stamina = Settings.Dash.StaminaCost;

            simulation.Step(DashUp);
            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Stamina.ExhaustedDuration / Settings.Hiding.DebuffRecoveryMul));

            Assert.That(simulation.Player.IsExhausted, Is.False);
        }
    }
}
