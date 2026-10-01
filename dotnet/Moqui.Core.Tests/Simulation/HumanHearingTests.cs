using System.Linq;
using System.Numerics;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Simulation
{
    public class HumanHearingTests
    {
        private const float RateTolerance = 0.01f;

        private static HearingSensor Sensor => new HearingSensor(TestSimulations.Settings.Noise, TestSimulations.Settings.Hearing);

        [Test]
        public void FlightNoise_EarDistanceZero_Is16AndRadiusEdge_Is4()
        {
            float radius = TestSimulations.Settings.Noise.FlightRadius;

            Assert.That(Sensor.FlightNoiseRate(0f, radius), Is.EqualTo(16f).Within(RateTolerance));
            Assert.That(Sensor.FlightNoiseRate(radius, radius), Is.EqualTo(4f).Within(RateTolerance));
        }

        [Test]
        public void FlightNoise_InsideRadiusOutsideEarZone_UsesNearestEarDistance()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            var human = simulation.Human;
            Vector3 position = human.RightEar + new Vector3(20f, 0f, 0f);
            simulation.Player.Position = position;
            var perception = default(HumanPerception);

            Sensor.Sense(human, simulation.Player, simulation.Events, ref perception);

            float expected = Sensor.FlightNoiseRate(20f, TestSimulations.Settings.Noise.FlightRadius);
            Assert.That(perception.HearingRate, Is.EqualTo(expected).Within(RateTolerance));
            Assert.That(perception.InEarZone, Is.False);
        }

        [Test]
        public void FlightNoise_HeadTurned_SamePositionGivesDifferentRate()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            var human = simulation.Human;
            simulation.Player.Position = TestHumans.Head + new Vector3(30f, 0f, 0f);

            var facingForward = default(HumanPerception);
            Sensor.Sense(human, simulation.Player, simulation.Events, ref facingForward);
            human.HeadYaw = 90f;
            var facingPlayer = default(HumanPerception);
            Sensor.Sense(human, simulation.Player, simulation.Events, ref facingPlayer);

            // 정면을 볼 때는 오른쪽 귀가 플레이어 쪽(가까움), 오른쪽으로 90° 돌리면 두 귀가 플레이어와 같은 거리(멀어짐).
            Assert.That(facingForward.HearingRate, Is.GreaterThan(facingPlayer.HearingRate + 1f));
        }

        [Test]
        public void EarZone_Inside_Adds30PerSecond()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            var human = simulation.Human;
            const float distance = 8f;
            simulation.Player.Position = human.RightEar + new Vector3(distance, 0f, 0f);
            var perception = default(HumanPerception);

            Sensor.Sense(human, simulation.Player, simulation.Events, ref perception);

            float expected = Sensor.FlightNoiseRate(distance, TestSimulations.Settings.Noise.FlightRadius) + TestSimulations.Settings.Hearing.EarZoneRate;
            Assert.That(perception.InEarZone, Is.True);
            Assert.That(perception.HearingRate, Is.EqualTo(expected).Within(RateTolerance));
            Assert.That(TestSimulations.Settings.Hearing.EarZoneRate, Is.EqualTo(30f));
        }

        [Test]
        public void FlightNoise_Precision_HalvesRadius()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            var human = simulation.Human;
            simulation.Player.Position = human.RightEar + new Vector3(35f, 0f, 0f);
            simulation.Player.PrecisionHeld = true;
            var perception = default(HumanPerception);

            Sensor.Sense(human, simulation.Player, simulation.Events, ref perception);

            Assert.That(perception.HearingRate, Is.EqualTo(0f), "35u is outside the 25u precision radius");
        }

        [Test]
        public void DashNoise_EarInsideRadius_RaisesAwarenessBy40Immediately()
        {
            // 귀에서 100u 뒤: 비행 소음(50u) 밖, 대시 소음(150u) 안, 시야 밖.
            var simulation = TestHumans.Simulation(TestHumans.Head + new Vector3(0f, 0f, -100f));

            simulation.Step(new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true });

            Assert.That(simulation.Events.OfType<NoiseEmitted>().Count(), Is.EqualTo(1));
            Assert.That(simulation.Human.Awareness, Is.EqualTo(TestSimulations.Settings.Dash.NoiseAwareness).Within(1e-4f));
            Assert.That(simulation.Human.Awareness, Is.EqualTo(40f).Within(1e-4f));
        }

        [Test]
        public void DashNoise_EarOutsideRadius_NoChange()
        {
            var simulation = TestHumans.Simulation(TestHumans.Head + new Vector3(0f, 0f, -200f));

            simulation.Step(new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true });

            Assert.That(simulation.Human.Awareness, Is.EqualTo(0f));
        }
    }
}
