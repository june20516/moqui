using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Random;
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
        public void DashDirection_FollowsMovementInput_IncludingForwardAndVertical()
        {
            var forward = new PlayerCommand { Move = new Vector2(0f, 1f) };
            var forwardUp = new PlayerCommand { Move = new Vector2(0f, 1f), Vertical = 1f };
            var leftDown = new PlayerCommand { Move = new Vector2(-1f, 0f), Vertical = -1f };
            var turned = new PlayerCommand { Move = new Vector2(0f, 1f), LookYaw = 90f };
            var random = SeedStreams.Create(1, SeedStreams.Dash);

            Assert.That(Vector3.Distance(DashDirectionResolver.Resolve(forward, random), Vector3.UnitZ), Is.LessThan(Tolerance), "forward is no longer ignored");
            Assert.That(Vector3.Distance(DashDirectionResolver.Resolve(forwardUp, random), Vector3.Normalize(new Vector3(0, 1, 1))), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(DashDirectionResolver.Resolve(leftDown, random), Vector3.Normalize(new Vector3(-1, -1, 0))), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(DashDirectionResolver.Resolve(turned, random), Vector3.UnitX), Is.LessThan(Tolerance), "camera yaw");
        }

        [Test]
        public void DashDirection_NoInput_RandomUnitVector_ReproducibleWithSeed_NotAlwaysUp()
        {
            var first = SeedStreams.Create(7, SeedStreams.Dash);
            var second = SeedStreams.Create(7, SeedStreams.Dash);
            int upward = 0;
            for (int i = 0; i < 200; i++)
            {
                Vector3 a = DashDirectionResolver.Resolve(PlayerCommand.None, first);
                Vector3 b = DashDirectionResolver.Resolve(PlayerCommand.None, second);
                Assert.That(a, Is.EqualTo(b), "same seed, same direction");
                Assert.That(a.Length(), Is.EqualTo(1f).Within(Tolerance));
                upward += a.Y > 0.9f ? 1 : 0;
            }

            Assert.That(upward, Is.LessThan(40), "uniform on the sphere, not biased up");
        }

        [Test]
        public void Dash_ForwardInput_MovesForward()
        {
            var simulation = Empty();
            simulation.Step(new PlayerCommand { Move = new Vector2(0f, 1f), DashPressed = true });

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dashing));
            Assert.That(Vector3.Distance(simulation.Player.DashDirection, Vector3.UnitZ), Is.LessThan(Tolerance));
        }

        [Test]
        public void Dash_WhileAttached_LeavesAlongSurfaceNormal()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("ceiling", new Vector3(0, 110, 0), new Vector3(200, 2, 200), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 108.5f, 0));
            simulation.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));

            simulation.Step(new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true });

            Assert.That(Vector3.Distance(simulation.Player.DashDirection, -Vector3.UnitY), Is.LessThan(Tolerance), $"away from the ceiling, ignoring move input; state {simulation.Player.State}, pos {simulation.Player.Position}");
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Dashing));
            Assert.That(simulation.Events.OfType<PlayerDetached>().Count(), Is.EqualTo(1));
            Assert.That(simulation.Events.OfType<NoiseEmitted>().Count(e => e.Source == NoiseSource.Dash), Is.EqualTo(1));
        }

        [Test]
        public void Dash_WhileAttached_WithoutStamina_StaysAttached()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0, 0, 10), new Vector3(50, 50, 1), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            var simulation = WithWorld(world, new Vector3(0, 0, 8f));
            simulation.Step(new PlayerCommand { AttachPressed = true });
            simulation.Player.Stamina = 0f;

            simulation.Step(new PlayerCommand { DashPressed = true });

            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
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
