using System.Numerics;
using Moqui.Core.Simulation;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class ExternalForceTests
    {
        [Test]
        public void Wind_NoInput_AddedImmediatelyWithoutInertia()
        {
            var simulation = Empty();
            var wind = new Vector3(40f, 0f, 0f);
            simulation.Player.ExternalVelocity = wind;

            simulation.Step(PlayerCommand.None);

            Assert.That(simulation.Player.Position.X, Is.EqualTo(wind.X * GameSimulation.DeltaTime).Within(1e-5f));
            Assert.That(simulation.Player.Velocity, Is.EqualTo(Vector3.Zero), "wind is not part of inertial velocity");
        }

        [Test]
        public void Wind_WithInput_AddsToInputMovement()
        {
            var withWind = Empty();
            var withoutWind = Empty();
            withWind.Player.ExternalVelocity = new Vector3(40f, 0f, 0f);

            Run(withWind, Forward, SecondsToTicks(1f));
            Run(withoutWind, Forward, SecondsToTicks(1f));

            Assert.That(withWind.Player.Position.X, Is.EqualTo(40f).Within(1e-2f));
            Assert.That(withWind.Player.Position.Z, Is.EqualTo(withoutWind.Player.Position.Z).Within(1e-4f));
        }

        [Test]
        public void FallingBody_OneSecond_Falls4905u()
        {
            var simulation = Empty();
            var body = new FallingBody("drop", new Vector3(0f, 1000f, 0f), 1.5f);
            simulation.AddFallingBody(body);

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            float fallen = 1000f - body.Position.Y;
            Assert.That(fallen, Is.EqualTo(490.5f).Within(490.5f * 0.01f));
        }
    }
}
