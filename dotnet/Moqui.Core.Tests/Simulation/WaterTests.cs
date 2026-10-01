using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class WaterTests
    {
        private static readonly Vector3 Source = new Vector3(0f, 300f, 0f);

        /// <summary>바닥(윗면 y=0) 위 300u 발생원, 발생원 아래 높이 150에 플레이어.</summary>
        private static GameSimulation UnderDrip(Vector3? playerPosition = null)
        {
            var setup = new SimulationSetup(FloorWorld, playerPosition ?? new Vector3(0f, 150f, 0f), dripSources: new[] { Source });
            return new GameSimulation(Settings, setup);
        }

        private static CollisionWorld FloorWorld()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("floor", new Vector3(0, -5, 0), new Vector3(200, 5, 200), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            return world;
        }

        private static GameSimulation Trapped()
        {
            var simulation = UnderDrip();
            for (int i = 0; i < SecondsToTicks(1f) && simulation.Player.State != PlayerState.Trapped; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Trapped));
            return simulation;
        }

        private static GameSimulation Escaped()
        {
            var simulation = Trapped();
            simulation.Step(new PlayerCommand { DashPressed = true });
            simulation.Step(new PlayerCommand { DashPressed = true });
            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Dash.Duration) + 1);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            return simulation;
        }

        [Test]
        public void Drops_SpawnEvery25Seconds()
        {
            var simulation = UnderDrip(new Vector3(100f, 50f, 0f));

            Run(simulation, PlayerCommand.None, SecondsToTicks(10f));

            Assert.That(Settings.Water.DropInterval, Is.EqualTo(2.5f));
            Assert.That(simulation.Water.SpawnedCount, Is.EqualTo(4), "t = 0, 2.5, 5, 7.5");
            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Water.SpawnedCount, Is.EqualTo(5), "t = 10");
        }

        [Test]
        public void Drops_FallAndVanishOnFloor()
        {
            var simulation = UnderDrip(new Vector3(100f, 50f, 0f));

            simulation.Step(PlayerCommand.None);
            Assert.That(simulation.Water.Drops.Count, Is.EqualTo(1));
            Run(simulation, PlayerCommand.None, SecondsToTicks(1.5f));

            Assert.That(simulation.Water.Drops.Count, Is.EqualTo(0), "300u fall takes about 0.78s");
        }

        [Test]
        public void DropHitsPlayer_Trapped_MoveInputIgnored()
        {
            var simulation = Trapped();
            Assert.That(simulation.Events.OfType<PlayerTrapped>().Count(), Is.EqualTo(1));
            Vector3 start = simulation.Player.Position;

            Run(simulation, new PlayerCommand { Move = new Vector2(1f, 1f), Vertical = 1f }, 20);

            Assert.That(simulation.Player.Position.X, Is.EqualTo(start.X).Within(1e-4f));
            Assert.That(simulation.Player.Position.Z, Is.EqualTo(start.Z).Within(1e-4f));
            Assert.That(simulation.Player.Position.Y, Is.LessThan(start.Y), "falls with the drop");
            Assert.That(simulation.Player.Position, Is.EqualTo(simulation.Water.Drops.Single().Position), "pinned to the drop center");
        }

        [Test]
        public void DropPassesPlayer_AnyHeight_AlwaysTraps()
        {
            // 물방울은 아래로 갈수록 틱당 수 u씩 떨어진다. 어느 높이에 있든 건너뛰지 않아야 한다.
            for (float height = 20f; height <= 280f; height += 3.7f)
            {
                var simulation = UnderDrip(new Vector3(0.5f, height, 0f));
                for (int i = 0; i < SecondsToTicks(2f) && simulation.Player.State != PlayerState.Trapped; i++)
                {
                    simulation.Step(PlayerCommand.None);
                }

                Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Trapped), $"height {height}");
            }
        }

        [Test]
        public void Trapped_FallsAtConstant100()
        {
            var simulation = Trapped();
            float startY = simulation.Player.Position.Y;
            const int ticks = 30;

            Run(simulation, PlayerCommand.None, ticks);

            float speed = (startY - simulation.Player.Position.Y) / (ticks * GameSimulation.DeltaTime);
            Assert.That(speed, Is.EqualTo(100f).Within(2f));
            Assert.That(simulation.CaptureSnapshot().TrappedHeightRemaining, Is.GreaterThan(0f));
        }

        [Test]
        public void DashTwice_Escapes_StaminaUnchanged()
        {
            var simulation = Trapped();
            float stamina = simulation.Player.Stamina;
            float y = simulation.Player.Position.Y;

            simulation.Step(new PlayerCommand { DashPressed = true });
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Trapped), "one press is not enough");
            simulation.Step(new PlayerCommand { DashPressed = true });

            Assert.That(simulation.Events.OfType<PlayerEscapedDrop>().Count(), Is.EqualTo(1));
            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Dash.Duration) + 1);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            Assert.That(simulation.Player.Stamina, Is.EqualTo(stamina), "escape costs no stamina");
            Assert.That(simulation.Player.Position.Y, Is.GreaterThan(y + Settings.Dash.Distance - 10f), "moves up one dash");
            Assert.That(simulation.Events.OfType<NoiseEmitted>(), Is.Empty);
        }

        [Test]
        public void TrappedUntilFloor_DiesWithWaterImpact()
        {
            var simulation = Trapped();
            PlayerDied death = null;

            for (int i = 0; i < SecondsToTicks(3f) && death == null; i++)
            {
                simulation.Step(PlayerCommand.None);
                death = simulation.Events.OfType<PlayerDied>().SingleOrDefault();
            }

            Assert.That(death, Is.Not.Null);
            Assert.That(death.Cause, Is.EqualTo(DeathCause.WaterImpact));
            Assert.That(simulation.Outcome, Is.EqualTo(StageOutcome.Died));
        }

        [Test]
        public void AfterEscape_WetWingsFor10Seconds_SpeedRegenDashCost()
        {
            var simulation = Escaped();
            var water = Settings.Water;
            Assert.That(simulation.Player.WetRemaining, Is.EqualTo(water.WetDuration).Within(0.2f));

            Run(simulation, new PlayerCommand { Move = new Vector2(0f, 1f), LookYaw = 0f }, SecondsToTicks(1f));
            Assert.That(simulation.Player.Velocity.Z / Settings.Flight.Speed, Is.EqualTo(water.WetSpeedMul).Within(1e-3f));

            simulation.Player.Stamina = 50f;
            float before = simulation.Player.Stamina;
            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));
            Assert.That(simulation.Player.Stamina - before, Is.EqualTo(Settings.Stamina.RegenRate * water.WetRegenMul).Within(0.5f), "regen x0.5");

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));
            float staminaBeforeDash = simulation.Player.Stamina;
            simulation.Step(new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true });
            Assert.That(staminaBeforeDash - simulation.Player.Stamina, Is.EqualTo(35f).Within(0.2f), "dash costs 25 + 10");

            Run(simulation, PlayerCommand.None, SecondsToTicks(water.WetDuration));
            Assert.That(simulation.Player.IsWet, Is.False, "wears off after 10s");
        }

        [Test]
        public void WetAgain_DurationRefreshedNotStacked()
        {
            var simulation = Escaped();
            Run(simulation, PlayerCommand.None, SecondsToTicks(4f));
            Assert.That(simulation.Player.WetRemaining, Is.LessThan(7f));

            simulation.Player.Position = new Vector3(0f, 150f, 0f);
            for (int i = 0; i < SecondsToTicks(3f) && simulation.Player.State != PlayerState.Trapped; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            simulation.Step(new PlayerCommand { DashPressed = true });
            simulation.Step(new PlayerCommand { DashPressed = true });

            Assert.That(simulation.Player.WetRemaining, Is.EqualTo(Settings.Water.WetDuration).Within(0.05f));
        }

        [Test]
        public void DripSourceHeight_CheckedByDownwardRay()
        {
            var world = FloorWorld();
            world.Add(CollisionShape.Box("shelf", new Vector3(50, 99, 0), new Vector3(20, 1, 20), ShapeFlags.Obstacle));
            var water = Settings.Water;

            Assert.That(LevelChecks.HeightAboveLanding(world, new Vector3(0, 300, 0)), Is.EqualTo(300f).Within(1e-3f));
            Assert.That(LevelChecks.DripSourceHighEnough(world, new Vector3(0, 300, 0), water), Is.True);
            Assert.That(LevelChecks.DripSourceHighEnough(world, new Vector3(50, 200, 0), water), Is.False, "only 100u above the shelf");
            Assert.That(water.MinSourceHeight, Is.EqualTo(150f));
        }

        [Test]
        public void Humidity_StrongWeakOutside_15_5_Minus10()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("steam", new Vector3(0, 0, 0), new Vector3(20, 20, 20), ShapeFlags.HumidStrong));
            world.Add(CollisionShape.Box("damp", new Vector3(100, 0, 0), new Vector3(20, 20, 20), ShapeFlags.HumidWeak));
            var strong = WithWorld(world, Vector3.Zero);
            Run(strong, PlayerCommand.None, SecondsToTicks(2f));
            Assert.That(strong.Player.Humidity, Is.EqualTo(30f).Within(0.3f));
            Assert.That(strong.Player.InSteam, Is.True);

            var world2 = new CollisionWorld();
            world2.Add(CollisionShape.Box("damp", new Vector3(100, 0, 0), new Vector3(20, 20, 20), ShapeFlags.HumidWeak));
            var weak = WithWorld(world2, new Vector3(100, 0, 0));
            Run(weak, PlayerCommand.None, SecondsToTicks(2f));
            Assert.That(weak.Player.Humidity, Is.EqualTo(10f).Within(0.2f));
            Assert.That(weak.Player.InSteam, Is.False);

            var outside = Empty();
            outside.Player.Humidity = 50f;
            Run(outside, PlayerCommand.None, SecondsToTicks(2f));
            Assert.That(outside.Player.Humidity, Is.EqualTo(30f).Within(0.3f));
        }

        [Test]
        public void Humidity_Reaches100_WetWingsAndRefreshedWhileInside()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("steam", Vector3.Zero, new Vector3(20, 20, 20), ShapeFlags.HumidStrong));
            var simulation = WithWorld(world, Vector3.Zero);
            simulation.Player.Humidity = 99f;

            Run(simulation, PlayerCommand.None, 10);
            Assert.That(simulation.Player.IsWet, Is.True);

            Run(simulation, PlayerCommand.None, SecondsToTicks(15f));
            Assert.That(simulation.Player.Humidity, Is.EqualTo(100f));
            Assert.That(simulation.Player.WetRemaining, Is.EqualTo(Settings.Water.WetDuration).Within(0.02f), "refreshed every tick inside the zone");
        }

        [Test]
        public void Humidity_RisesWhileAttached()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("steam", Vector3.Zero, new Vector3(40, 40, 40), ShapeFlags.HumidStrong));
            world.Add(CollisionShape.Box("tile", new Vector3(0, 0, 10), new Vector3(20, 20, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 0, 8f));
            simulation.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(simulation.Player.Humidity, Is.EqualTo(Settings.Humid.GainStrong).Within(0.5f));
        }

        [Test]
        public void Steam_VisionRateIs06Times()
        {
            var world = new CollisionWorld();
            var simulation = Support.TestHumans.Simulation(Support.TestHumans.InFront(150f), world);
            var vision = new VisionSensor(Settings.Vision, simulation.World, Settings.Humid.SteamVisionMul);

            var clear = default(HumanPerception);
            vision.Sense(simulation.Human, simulation.Player, 0, ref clear);
            simulation.Player.InSteam = true;
            var steamy = default(HumanPerception);
            vision.Sense(simulation.Human, simulation.Player, 0, ref steamy);

            Assert.That(steamy.VisionRate / clear.VisionRate, Is.EqualTo(0.6f).Within(1e-4f));
        }

        [Test]
        public void Steam_IntegratedInHumanSystem()
        {
            Vector3 position = Support.TestHumans.InFront(150f);
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("steam", position, new Vector3(20, 20, 20), ShapeFlags.HumidStrong));
            var steamy = Support.TestHumans.Simulation(position, world);
            var clear = Support.TestHumans.Simulation(position);

            Run(steamy, PlayerCommand.None, SecondsToTicks(1f));
            Run(clear, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(steamy.Human.Awareness / clear.Human.Awareness, Is.EqualTo(0.6f).Within(0.02f));
        }

        [Test]
        public void Hidden_WetAndHumidityRecoverTwiceAsFast()
        {
            var hidden = WithWorld(ShadowWorld(Vector3.Zero, 20f), Vector3.Zero);
            var open = Empty();
            foreach (var simulation in new[] { hidden, open })
            {
                simulation.Player.WetRemaining = 10f;
                simulation.Player.Humidity = 80f;
            }

            Run(hidden, PlayerCommand.None, SecondsToTicks(2f));
            Run(open, PlayerCommand.None, SecondsToTicks(2f));

            Assert.That(10f - hidden.Player.WetRemaining, Is.EqualTo(2f * (10f - open.Player.WetRemaining)).Within(0.05f));
            Assert.That(80f - hidden.Player.Humidity, Is.EqualTo(2f * (80f - open.Player.Humidity)).Within(0.3f));
            Assert.That(hidden.Player.WetRemaining, Is.EqualTo(6f).Within(0.05f));
            Assert.That(hidden.Player.Humidity, Is.EqualTo(40f).Within(0.3f));
        }
    }
}
