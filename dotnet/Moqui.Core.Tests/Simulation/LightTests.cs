using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>조명 스위치 (spec/06, M14).</summary>
    public class LightTests
    {
        private static readonly Vector3 AreaCenter = new Vector3(0f, 100f, 150f);
        private static readonly Vector3 AreaHalf = new Vector3(100f, 100f, 100f);

        private static GameSimulation WithLight(LightDefinition light, Vector3 spawn, CollisionWorld world = null)
        {
            var gimmicks = new GimmickSetup(null, null, null, new[] { light });
            var setup = new SimulationSetup(world ?? new CollisionWorld(), spawn, TestHumans.Seated(), TestHumans.DefaultSeed, gimmicks: gimmicks);
            var simulation = new GameSimulation(Settings, setup);
            TestHumans.DisableReactions(simulation);
            return simulation;
        }

        [Test]
        public void Scheduled_LitOnlyWhileOnAndInsideArea()
        {
            var light = new LightDefinition("lamp", AreaCenter, AreaCenter, AreaHalf, new FanSchedule(2f, 2f, 0f), false);
            var simulation = WithLight(light, AreaCenter);

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(simulation.Lights.IsOn(0), Is.True);
            Assert.That(simulation.Player.InLight, Is.True);
            Assert.That(simulation.Lights.IsLit(AreaCenter + new Vector3(150f, 0f, 0f)), Is.False, "outside the area");

            Run(simulation, PlayerCommand.None, SecondsToTicks(2f));
            Assert.That(simulation.Lights.IsOn(0), Is.False);
            Assert.That(simulation.Player.InLight, Is.False);
            Assert.That(simulation.Lights.IsAboutToTurnOn(0, simulation.Tick, 0.5f), Is.False, "two seconds off: not yet");
            Run(simulation, PlayerCommand.None, SecondsToTicks(0.6f));
            Assert.That(simulation.Lights.IsAboutToTurnOn(0, simulation.Tick, 0.5f), Is.True, "within half a second of the schedule");
        }

        [Test]
        public void OnWhenAlert_SwitchesOnAfterDelay_OffAfterCalm()
        {
            var light = new LightDefinition("ceiling", AreaCenter, AreaCenter, AreaHalf, null, true);
            var simulation = WithLight(light, TestHumans.FarBehind);
            var lights = Settings.Light;

            TestHumans.Provoke(simulation, 50f);
            for (int i = 0; i < SecondsToTicks(lights.SwitchDelay) - 2; i++)
            {
                TestHumans.Provoke(simulation, 50f);
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(simulation.Lights.IsOn(0), Is.False, "the human needs time to reach the switch");
            Assert.That(simulation.Lights.IsAboutToTurnOn(0, simulation.Tick, 1f), Is.True, "about to turn on: the presentation can warn (gulf §8)");
            for (int i = 0; i < 4; i++)
            {
                TestHumans.Provoke(simulation, 50f);
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(simulation.Lights.IsOn(0), Is.True);

            simulation.Human.Awareness = 0f;
            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Safe));
            Assert.That(simulation.Lights.IsOn(0), Is.True, "stays on for a while after calming down");
            Run(simulation, PlayerCommand.None, SecondsToTicks(lights.OffDelay));
            Assert.That(simulation.Lights.IsOn(0), Is.False);
        }

        [Test]
        public void Lit_ShadowZoneNoLongerHides()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("shadow", AreaCenter, new Vector3(20f, 20f, 20f), ShapeFlags.ShadowZone));
            var light = new LightDefinition("lamp", AreaCenter, AreaCenter, AreaHalf, new FanSchedule(1f, 1f, 0f), false);
            var simulation = WithLight(light, AreaCenter, world);

            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Player.IsHidden, Is.False, "the light removes the shadow");
            Run(simulation, PlayerCommand.None, SecondsToTicks(1.2f));
            Assert.That(simulation.Player.IsHidden, Is.True, "dark again");
        }

        [Test]
        public void Lit_VisionGainMultiplied()
        {
            Vector3 inFront = TestHumans.InFront(150f);
            var lit = new LightDefinition("lamp", inFront, inFront, AreaHalf, new FanSchedule(100f, 1f, 0f), false);
            var dark = new LightDefinition("lamp", inFront, inFront, AreaHalf, new FanSchedule(1f, 100f, 50f), false);
            var litSimulation = WithLight(lit, inFront);
            var darkSimulation = WithLight(dark, inFront);

            Run(litSimulation, PlayerCommand.None, 10);
            Run(darkSimulation, PlayerCommand.None, 10);

            float litRate = litSimulation.HumanSystem.LastPerception.VisionRate;
            float darkRate = darkSimulation.HumanSystem.LastPerception.VisionRate;
            Assert.That(darkRate, Is.GreaterThan(0f));
            Assert.That(litRate, Is.EqualTo(darkRate * Settings.Light.VisionMul).Within(1e-3f));
        }
    }
}
