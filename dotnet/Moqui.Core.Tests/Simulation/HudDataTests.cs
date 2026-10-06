using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>HUD 경계 아이콘의 원천 데이터 (spec/08): 가려짐, 광분 남은 최소 시간, 진정 진행.</summary>
    public class HudDataTests
    {
        private const float TimeTolerance = 0.05f;

        [Test]
        public void PlayerOccluded_InYellowZoneBehindObstacle_True_VisibleOrOutsideCone_False()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("screen", TestHumans.InFront(80f), new Vector3(50f, 50f, 1f), ShapeFlags.Obstacle));
            var blocked = TestHumans.Simulation(TestHumans.InFront(150f), world);
            blocked.Step(PlayerCommand.None);
            Assert.That(blocked.Human.PlayerOccluded, Is.True);
            Assert.That(blocked.CaptureSnapshot().Human.PlayerOccluded, Is.True);

            var open = TestHumans.Simulation(TestHumans.InFront(150f));
            open.Step(PlayerCommand.None);
            Assert.That(open.Human.PlayerOccluded, Is.False, "visible is not occluded");

            var behind = TestHumans.Simulation(TestHumans.FarBehind);
            behind.Step(PlayerCommand.None);
            Assert.That(behind.Human.PlayerOccluded, Is.False, "outside the cone is not occluded");
        }

        [Test]
        public void Frenzy_MinRemainingCountsDown_CalmProgressRisesWhileUnseen()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind);
            var frenzy = Settings.Frenzy;
            Assert.That(simulation.Human.FrenzyMinRemaining, Is.EqualTo(0f));
            Assert.That(simulation.Human.CalmProgress, Is.EqualTo(0f));

            TestHumans.Provoke(simulation, 100f);
            simulation.Step(PlayerCommand.None);
            Run(simulation, PlayerCommand.None, SecondsToTicks(2f));

            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));
            Assert.That(simulation.Human.FrenzyMinRemaining, Is.EqualTo(frenzy.MinDuration - 2f).Within(TimeTolerance));
            Assert.That(simulation.Human.CalmProgress, Is.EqualTo(2f / frenzy.CalmTime).Within(TimeTolerance));
            var snapshot = simulation.CaptureSnapshot().Human;
            Assert.That(snapshot.FrenzyMinRemaining, Is.EqualTo(simulation.Human.FrenzyMinRemaining));
            Assert.That(snapshot.CalmProgress, Is.EqualTo(simulation.Human.CalmProgress));

            Run(simulation, PlayerCommand.None, SecondsToTicks(frenzy.MinDuration));
            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Suspicious));
            Assert.That(simulation.Human.FrenzyMinRemaining, Is.EqualTo(0f), "cleared after calming");
            Assert.That(simulation.Human.CalmProgress, Is.EqualTo(0f));
        }

        [Test]
        public void Frenzy_SeenAgain_CalmProgressResets()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind);
            TestHumans.Provoke(simulation, 100f);
            simulation.Step(PlayerCommand.None);
            Run(simulation, PlayerCommand.None, SecondsToTicks(2f));
            Assert.That(simulation.Human.CalmProgress, Is.GreaterThan(0f));

            simulation.Player.Position = TestHumans.InFront(200f);
            simulation.Step(PlayerCommand.None);

            Assert.That(simulation.Human.PlayerVisible, Is.True);
            Assert.That(simulation.Human.CalmProgress, Is.EqualTo(0f));
        }

        [Test]
        public void CanAttach_OnlyFlyingWithinSnapRangeOfAttachableSurface()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(0f, 100f, 50f), new Vector3(100f, 100f, 1f), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            float range = Settings.Attach.SnapRange;
            var simulation = WithWorld(world, new Vector3(0f, 100f, 50f - 1f - (range * 0.5f)));
            Assert.That(simulation.CanAttach, Is.True);

            simulation.Player.Position = new Vector3(0f, 100f, 50f - 1f - (range * 2f));
            Assert.That(simulation.CanAttach, Is.False, "out of range");

            simulation.Player.Position = new Vector3(0f, 100f, 50f - 1f - (range * 0.5f));
            simulation.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));
            Assert.That(simulation.CanAttach, Is.False, "already attached");
        }

        [Test]
        public void DashCost_IncludesWetWingsExtraCost()
        {
            var simulation = Empty();
            Assert.That(simulation.DashCost, Is.EqualTo(Settings.Dash.StaminaCost));

            simulation.Player.WetRemaining = 1f;
            simulation.Step(PlayerCommand.None);

            Assert.That(simulation.DashCost, Is.EqualTo(Settings.Dash.StaminaCost + Settings.Water.WetDashCostAdd).Within(1e-4f));
        }
    }
}
