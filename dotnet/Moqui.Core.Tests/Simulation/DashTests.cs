using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class DashTests
    {
        private const float Tolerance = 1e-3f;

        private static PlayerCommand DashRight => new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true };

        [Test]
        public void Dash_FromRest_Moves60uIn012Seconds()
        {
            var simulation = Empty();
            int dashTicks = SecondsToTicks(Settings.Dash.Duration);

            simulation.Step(DashRight);
            Run(simulation, PlayerCommand.None, dashTicks - 1);

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            Assert.That(simulation.Player.Position.X, Is.EqualTo(Settings.Dash.Distance).Within(1f));
            Assert.That(dashTicks * GameSimulation.DeltaTime, Is.EqualTo(Settings.Dash.Duration).Within(GameSimulation.DeltaTime));
        }

        [Test]
        public void Dash_JustFinished_HasFlightSpeedAlongDashThenDecelerates()
        {
            var simulation = Empty();
            simulation.Step(DashRight);
            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Dash.Duration) - 1);

            Vector3 residual = simulation.Player.Velocity;
            simulation.Step(PlayerCommand.None);

            Assert.That(Vector3.Distance(residual, Vector3.UnitX * Settings.Flight.Speed), Is.LessThan(Tolerance));
            Assert.That(simulation.Player.Velocity.X, Is.LessThan(residual.X));
            Assert.That(simulation.Player.Velocity.X, Is.GreaterThan(0f));
        }

        [Test]
        public void DashDirection_NoLateralOrVerticalInput_IsUp()
        {
            var forwardOnly = new PlayerCommand { Move = new Vector2(0f, 1f), DashPressed = true };

            Assert.That(DashDirectionResolver.Resolve(forwardOnly, false), Is.EqualTo(Vector3.UnitY));
            Assert.That(DashDirectionResolver.Resolve(PlayerCommand.None, false), Is.EqualTo(Vector3.UnitY));
        }

        [Test]
        public void DashDirection_LargestAxisWins()
        {
            var mostlyDown = new PlayerCommand { Move = new Vector2(0.3f, 0f), Vertical = -1f };
            var mostlyLeft = new PlayerCommand { Move = new Vector2(-1f, 0f), Vertical = 0.5f };

            Assert.That(DashDirectionResolver.Resolve(mostlyDown, false), Is.EqualTo(-Vector3.UnitY));
            Assert.That(Vector3.Distance(DashDirectionResolver.Resolve(mostlyLeft, false), -Vector3.UnitX), Is.LessThan(Tolerance));
        }

        [Test]
        public void DashDirection_TieBetweenLateralAndVertical_PrefersLateral()
        {
            var tie = new PlayerCommand { Move = new Vector2(1f, 0f), Vertical = 1f };

            Assert.That(Vector3.Distance(DashDirectionResolver.Resolve(tie, false), Vector3.UnitX), Is.LessThan(Tolerance));
        }

        [Test]
        public void DashDirection_DiagonalUnlocked_CombinesAxes()
        {
            var both = new PlayerCommand { Move = new Vector2(1f, 0f), Vertical = 1f };

            Vector3 direction = DashDirectionResolver.Resolve(both, true);

            Assert.That(Vector3.Distance(direction, Vector3.Normalize(new Vector3(1, 1, 0))), Is.LessThan(Tolerance));
        }

        [Test]
        public void Dash_StaminaBelowCost_DoesNotExecute()
        {
            var simulation = Empty();
            simulation.Player.Stamina = Settings.Dash.StaminaCost - 0.01f;

            simulation.Step(DashRight);

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            Assert.That(simulation.Events, Is.Empty);
        }

        [Test]
        public void Dash_PressedDuringCooldown_IsIgnored()
        {
            var simulation = Empty();
            simulation.Step(DashRight);
            Run(simulation, PlayerCommand.None, SecondsToTicks(Settings.Dash.Cooldown) - 2);
            float staminaBefore = simulation.Player.Stamina;

            simulation.Step(DashRight);

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
            Assert.That(simulation.Player.Stamina, Is.EqualTo(staminaBefore));

            simulation.Step(DashRight);
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dashing), "cooldown elapsed exactly at 0.5s");
        }

        [Test]
        public void Dash_Executed_EmitsSingleNoiseEventWith150uRadius()
        {
            var simulation = Empty();
            int noiseCount = 0;
            NoiseEmitted noise = null;

            simulation.Step(DashRight);
            noiseCount += simulation.Events.OfType<NoiseEmitted>().Count();
            noise = simulation.Events.OfType<NoiseEmitted>().FirstOrDefault();
            for (int i = 0; i < SecondsToTicks(Settings.Dash.Duration); i++)
            {
                simulation.Step(PlayerCommand.None);
                noiseCount += simulation.Events.OfType<NoiseEmitted>().Count();
            }

            Assert.That(noiseCount, Is.EqualTo(1));
            Assert.That(noise.Radius, Is.EqualTo(150f));
            Assert.That(noise.Awareness, Is.EqualTo(Settings.Dash.NoiseAwareness));
            Assert.That(noise.Source, Is.EqualTo(NoiseSource.Dash));
        }

        [Test]
        public void Dash_IntoWall_StopsWithoutPenetrating()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("thin", new Vector3(20f, 0f, 0f), new Vector3(0.05f, 50f, 50f), ShapeFlags.Obstacle));
            var simulation = WithWorld(world, Vector3.Zero);

            simulation.Step(DashRight);
            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            float radius = simulation.Player.CollisionRadius;
            Assert.That(simulation.Player.Position.X, Is.LessThanOrEqualTo(20f - 0.05f - radius));
            Assert.That(simulation.Player.Position.X, Is.GreaterThan(20f - 0.05f - radius - 0.05f));
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Flying));
        }
    }
}
