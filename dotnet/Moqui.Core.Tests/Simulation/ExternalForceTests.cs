using System.Numerics;
using Moqui.Core.Simulation;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class ExternalForceTests
    {
        /// <summary>플레이어 쪽(+X)을 보는 선풍기가 있는 빈 세계. 시작 틱에 머리 방향이 정확히 +X다.</summary>
        private static GameSimulation WithFanBlowingPlusX(Vector3 playerSpawn)
        {
            var fans = new[] { new FanDefinition("fan", new Vector3(-100f, 0f, 0f), 90f) };
            return new GameSimulation(Settings, new SimulationSetup(new Moqui.Core.Collision.CollisionWorld(), playerSpawn, gimmicks: new GimmickSetup(fans, null, null)));
        }

        [Test]
        public void Wind_NoInput_AddedImmediatelyWithoutInertia()
        {
            var simulation = WithFanBlowingPlusX(Vector3.Zero);

            simulation.Step(PlayerCommand.None);

            Assert.That(simulation.Player.Position.X, Is.EqualTo(Settings.Fan.WindSpeed * GameSimulation.DeltaTime).Within(1e-4f));
            Assert.That(simulation.Player.Velocity, Is.EqualTo(Vector3.Zero), "wind is not part of inertial velocity");
        }

        [Test]
        public void Wind_WithInput_AddsToInputMovement()
        {
            const float seconds = 0.25f;
            var withWind = WithFanBlowingPlusX(Vector3.Zero);
            var withoutWind = Empty();

            Run(withWind, Forward, SecondsToTicks(seconds));
            Run(withoutWind, Forward, SecondsToTicks(seconds));

            // 0.25초 동안 머리가 9° 안쪽으로 돌 뿐이라 바람은 거의 +X다.
            Assert.That(withWind.Player.Position.X, Is.EqualTo(Settings.Fan.WindSpeed * seconds).Within(0.3f));
            Assert.That(withWind.Player.Position.Z, Is.EqualTo(withoutWind.Player.Position.Z).Within(1f), "input movement unchanged");
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
