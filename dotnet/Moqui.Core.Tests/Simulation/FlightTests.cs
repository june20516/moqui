using System;
using System.Numerics;
using Moqui.Core.Simulation;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class FlightTests
    {
        private const float VelocityEpsilon = 1e-4f;

        [Test]
        public void Release_AtTopSpeed_StopsIn015SecondsAfterSliding45u()
        {
            var simulation = Empty();
            Run(simulation, Forward, SecondsToTicks(1f));
            float startZ = simulation.Player.Position.Z;

            int ticks = 0;
            while (simulation.Player.Velocity.Length() > VelocityEpsilon && ticks < GameSimulation.TickRate)
            {
                simulation.Step(PlayerCommand.None);
                ticks++;
            }

            Assert.That(ticks * GameSimulation.DeltaTime, Is.EqualTo(0.15f).Within(GameSimulation.DeltaTime));
            Assert.That(simulation.Player.Position.Z - startZ, Is.EqualTo(4.5f).Within(4.5f * 0.02f));
        }

        [Test]
        public void Forward_OneSecondFromRest_Travels564u()
        {
            var simulation = Empty();

            Run(simulation, Forward, SecondsToTicks(1f));

            Assert.That(simulation.Player.Position.Z, Is.EqualTo(56.4f).Within(56.4f * 0.01f));
            Assert.That(simulation.Player.Position.X, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void ReverseInput_AtTopSpeed_ReversesSmoothly()
        {
            var simulation = Empty();
            var settings = Settings.Flight;
            Run(simulation, Forward, SecondsToTicks(1f));
            var backward = new PlayerCommand { Move = new Vector2(0f, -1f) };
            float maxStep = settings.Speed / settings.AccelTime * GameSimulation.DeltaTime;

            float previous = simulation.Player.Velocity.Z;
            int ticks = 0;
            while (simulation.Player.Velocity.Z > -settings.Speed + VelocityEpsilon)
            {
                simulation.Step(backward);
                float current = simulation.Player.Velocity.Z;
                Assert.That(previous - current, Is.LessThanOrEqualTo(maxStep + VelocityEpsilon), $"tick {ticks}: velocity jumped");
                previous = current;
                ticks++;
            }

            // +60 → −60을 가속도 speed/accelTime으로 바꾸면 2 × 0.12 = 0.24초.
            Assert.That(ticks * GameSimulation.DeltaTime, Is.EqualTo(2f * settings.AccelTime).Within(GameSimulation.DeltaTime));
        }

        [Test]
        public void DiagonalInput_TopSpeed_EqualsSingleDirectionSpeed()
        {
            var straight = Empty();
            var diagonal = Empty();

            Run(straight, Forward, SecondsToTicks(1f));
            Run(diagonal, new PlayerCommand { Move = new Vector2(1f, 1f) }, SecondsToTicks(1f));

            Assert.That(diagonal.Player.Velocity.Length(), Is.EqualTo(straight.Player.Velocity.Length()).Within(1e-3f));
            Assert.That(diagonal.Player.Velocity.Length(), Is.EqualTo(Settings.Flight.Speed).Within(1e-3f));
        }

        [Test]
        public void Forward_CameraYaw90_MovesAlongPositiveX()
        {
            var simulation = Empty();

            Run(simulation, new PlayerCommand { Move = new Vector2(0f, 1f), LookYaw = 90f }, SecondsToTicks(1f));

            Assert.That(simulation.Player.Position.X, Is.EqualTo(56.4f).Within(56.4f * 0.01f));
            Assert.That(simulation.Player.Position.Z, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Ascend_TopSpeed_UsesVerticalSpeed()
        {
            var simulation = Empty();

            Run(simulation, new PlayerCommand { Vertical = 1f }, SecondsToTicks(1f));

            Assert.That(simulation.Player.Velocity.Y, Is.EqualTo(Settings.Flight.VerticalSpeed).Within(1e-3f));
        }

        [Test]
        public void Pitch_DoesNotAffectMoveDirection()
        {
            var simulation = Empty();

            Run(simulation, new PlayerCommand { Move = new Vector2(0f, 1f), LookPitch = 60f }, SecondsToTicks(1f));

            Assert.That(simulation.Player.Position.Y, Is.EqualTo(0f));
        }

        [Test]
        public void Precision_TopSpeed_AppliesPrecisionMultiplier()
        {
            var simulation = Empty();
            var flight = Settings.Flight;

            Run(simulation, new PlayerCommand { Move = new Vector2(0f, 1f), PrecisionHeld = true }, SecondsToTicks(1f));

            Assert.That(simulation.Player.Velocity.Z, Is.EqualTo(flight.Speed * flight.PrecisionSpeedMul).Within(1e-3f));
        }

        [Test]
        public void SameCommands_TwoRuns_ProduceIdenticalPositions()
        {
            var a = Empty();
            var b = Empty();
            var commands = new[]
            {
                new PlayerCommand { Move = new Vector2(0.3f, 1f), LookYaw = 17f },
                new PlayerCommand { Move = new Vector2(-1f, 0f), Vertical = 1f, LookYaw = 200f },
                PlayerCommand.None,
            };

            for (int i = 0; i < 180; i++)
            {
                a.Step(commands[i % commands.Length]);
                b.Step(commands[i % commands.Length]);
            }

            Assert.That(a.Player.Position, Is.EqualTo(b.Player.Position));
        }
    }
}
