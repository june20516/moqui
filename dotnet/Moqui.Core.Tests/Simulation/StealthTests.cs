using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class StealthTests
    {
        [Test]
        public void ShadowZone_InsideYellowZone_NoVisionGainAndHidden()
        {
            Vector3 position = TestHumans.InFront(150f);
            var simulation = TestHumans.Simulation(position, ShadowWorld(position, 20f));

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(simulation.Player.IsHidden, Is.True);
            Assert.That(simulation.CaptureSnapshot().Player.IsHidden, Is.True, "InShadow is exported for the vignette");
            Assert.That(simulation.Human.Awareness, Is.EqualTo(0f));
            Assert.That(simulation.CaptureSnapshot().PlayerVisibleToHuman, Is.False);
        }

        [Test]
        public void ShadowZone_LeaveZone_NoLongerHidden()
        {
            var simulation = WithWorld(ShadowWorld(Vector3.Zero, 10f), Vector3.Zero);
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Player.IsHidden, Is.True);

            Run(simulation, Forward, SecondsToTicks(1f));

            Assert.That(simulation.Player.IsHidden, Is.False);
        }

        [Test]
        public void ShadowZone_NoStimulus_AwarenessDecays25PerSecond()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind, ShadowWorld(TestHumans.FarBehind, 20f));
            simulation.Human.Awareness = 60f;

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(Settings.Awareness.ShadowDecayRate, Is.EqualTo(25f));
            Assert.That(simulation.Human.Awareness, Is.EqualTo(60f - 25f).Within(0.5f));
        }

        [Test]
        public void ShadowZone_DashInside_AwarenessPlus40()
        {
            Vector3 position = TestHumans.Head + new Vector3(0f, 0f, -100f);
            var simulation = TestHumans.Simulation(position, ShadowWorld(position, 100f));
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Player.IsHidden, Is.True);

            simulation.Step(TestHumans.DashUp);

            Assert.That(simulation.Human.Awareness, Is.EqualTo(40f).Within(1e-4f));
        }

        [Test]
        public void Attached_NearEars_NoFlightNoise()
        {
            // 인간 오른쪽 귀 옆 벽: 귀까지 약 19u (비행 소음 반경 50u 안), 시야 밖(옆).
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", TestHumans.Head + new Vector3(35f, 0f, 0f), new Vector3(5f, 50f, 50f), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            Vector3 nearWall = TestHumans.Head + new Vector3(29.4f, 0f, 0f);

            var flying = TestHumans.Simulation(nearWall, world);
            Run(flying, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(flying.Human.Awareness, Is.GreaterThan(5f), "hovering there is audible");

            var world2 = new CollisionWorld();
            world2.Add(CollisionShape.Box("wall", TestHumans.Head + new Vector3(35f, 0f, 0f), new Vector3(5f, 50f, 50f), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var attached = TestHumans.Simulation(nearWall, world2);
            attached.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(attached.Player.State, Is.EqualTo(PlayerState.Attached));
            Run(attached, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(attached.Human.Awareness, Is.EqualTo(0f));
        }

        [Test]
        public void Attached_InYellowZone_VisionRateIs03Times()
        {
            var simulation = TestHumans.Simulation(TestHumans.InFront(150f));
            var vision = new VisionSensor(Settings.Vision, simulation.World);

            var flying = default(HumanPerception);
            vision.Sense(simulation.Human, simulation.Player, 0, ref flying);
            simulation.Player.State = PlayerState.Attached;
            var attached = default(HumanPerception);
            vision.Sense(simulation.Human, simulation.Player, 0, ref attached);

            Assert.That(attached.VisionRate / flying.VisionRate, Is.EqualTo(0.3f).Within(1e-4f));
        }
    }
}
