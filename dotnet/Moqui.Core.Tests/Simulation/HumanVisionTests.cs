using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Simulation
{
    public class HumanVisionTests
    {
        private const float RateTolerance = 0.01f;

        [TestCase(40f)]
        [TestCase(150f)]
        [TestCase(299f)]
        public void YellowZone_LineOfSight_RateIsLinearFrom25To8(float distance)
        {
            var simulation = TestHumans.Simulation(TestHumans.InFront(distance));
            var vision = new VisionSensor(TestSimulations.Settings.Vision, simulation.World);
            var perception = default(HumanPerception);

            vision.Sense(simulation.Human, simulation.Player, 0, ref perception);

            var settings = TestSimulations.Settings.Vision;
            float expected = settings.YellowRateNear + ((settings.YellowRateFar - settings.YellowRateNear) * (distance / settings.YellowRange));
            Assert.That(perception.PlayerSeen, Is.True);
            Assert.That(perception.VisionRate, Is.EqualTo(expected).Within(RateTolerance));
        }

        [Test]
        public void YellowZone_RateEndpoints_Are25And8()
        {
            var settings = TestSimulations.Settings.Vision;

            Assert.That(settings.YellowRateNear, Is.EqualTo(25f));
            Assert.That(settings.YellowRateFar, Is.EqualTo(8f));
        }

        [Test]
        public void YellowZone_IntegratedOverOneSecond_AwarenessRisesByRate()
        {
            const float distance = 150f;
            var simulation = TestHumans.Simulation(TestHumans.InFront(distance));

            TestSimulations.Run(simulation, PlayerCommand.None, TestSimulations.SecondsToTicks(1f));

            var settings = TestSimulations.Settings.Vision;
            float expected = settings.YellowRateNear + ((settings.YellowRateFar - settings.YellowRateNear) * (distance / settings.YellowRange));
            Assert.That(simulation.Human.Awareness, Is.EqualTo(expected).Within(0.5f));
        }

        [Test]
        public void Obstacle_BlocksLineOfSight_NoVisionGainAndNotVisible()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("screen", TestHumans.InFront(80f), new Vector3(50f, 50f, 1f), ShapeFlags.Obstacle));
            var blocked = TestHumans.Simulation(TestHumans.InFront(150f), world);

            TestSimulations.Run(blocked, PlayerCommand.None, TestSimulations.SecondsToTicks(1f));

            Assert.That(blocked.Human.Awareness, Is.EqualTo(0f));
            Assert.That(blocked.CaptureSnapshot().PlayerVisibleToHuman, Is.False);

            var open = TestHumans.Simulation(TestHumans.InFront(150f));
            open.Step(PlayerCommand.None);
            Assert.That(open.CaptureSnapshot().PlayerVisibleToHuman, Is.True);
        }

        [Test]
        public void OutsideCone_BehindHuman_NotVisible()
        {
            var simulation = TestHumans.Simulation(TestHumans.Head + new Vector3(0f, 0f, -100f));

            simulation.Step(PlayerCommand.None);

            Assert.That(simulation.CaptureSnapshot().PlayerVisibleToHuman, Is.False);
            Assert.That(simulation.Human.Awareness, Is.EqualTo(0f));
        }

        [Test]
        public void RedZone_EnteredWithLineOfSight_ClapAndFrenzySameTick()
        {
            var simulation = TestHumans.Simulation(TestHumans.InFront(30f));

            simulation.Step(PlayerCommand.None);

            var telegraph = simulation.Events.OfType<AttackTelegraphStarted>().Single();
            Assert.That(telegraph.Kind, Is.EqualTo(AttackKind.Clap));
            Assert.That(telegraph.Tick, Is.EqualTo(0));
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));
            Assert.That(simulation.Events.OfType<AwarenessStateChanged>().Single().To, Is.EqualTo(AwarenessState.Frenzy));
            Assert.That(Vector3.Distance(telegraph.Target, TestHumans.InFront(TestSimulations.Settings.Attack.ClapOffset)), Is.LessThan(0.01f));
        }

        [Test]
        public void Hidden_InYellowZone_NotVisibleAndNoGain()
        {
            var simulation = TestHumans.Simulation(TestHumans.InFront(150f), TestSimulations.ShadowWorld(TestHumans.InFront(150f), 20f));

            TestSimulations.Run(simulation, PlayerCommand.None, 30);

            Assert.That(simulation.CaptureSnapshot().PlayerVisibleToHuman, Is.False);
            Assert.That(simulation.Human.Awareness, Is.EqualTo(0f));
        }
    }
}
