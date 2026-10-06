using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using Moqui.Core.Tutorial;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>튜토리얼 안내 진행 (spec/08 §튜토리얼, spec/07): 현재 안내의 행동을 하면 다음으로 넘어간다.</summary>
    public class TutorialTests
    {
        private static TutorialSettings TutorialSettings => new TutorialSettings(Tuning);

        private static GameSimulation Stage(string levelId, out LevelDefinition level)
        {
            level = new LevelLoader(FileSystemDataSource.ForRepoData()).Load(levelId);
            return new GameSimulation(Settings, level.CreateSetup());
        }

        private static void Run(GameSimulation simulation, TutorialTracker tracker, PlayerCommand command, float seconds)
        {
            for (int i = 0; i < SecondsToTicks(seconds); i++)
            {
                simulation.Step(command);
                tracker.Observe(simulation);
            }
        }

        private static void Tick(GameSimulation simulation, TutorialTracker tracker, PlayerCommand command)
        {
            simulation.Step(command);
            tracker.Observe(simulation);
        }

        [Test]
        public void LevelTutorialSteps_AreAllKnown()
        {
            foreach (string id in new[] { "stage01", "stage02" })
            {
                var level = new LevelLoader(FileSystemDataSource.ForRepoData()).Load(id);
                Assert.That(level.Tutorial, Is.Not.Empty, id);
                Assert.That(level.Tutorial.All(TutorialTracker.IsKnownStep), Is.True, id);
            }

            Assert.Throws<System.ArgumentException>(() => new TutorialTracker(new[] { "nope" }, TutorialSettings));
        }

        [Test]
        public void Stage1_MoveLookPrecisionDash_AdvanceInOrderOnlyByTheirActions()
        {
            var simulation = Stage("stage01", out var level);
            var tracker = new TutorialTracker(level.Tutorial, TutorialSettings);
            var hold = TutorialSettings.HoldSeconds;
            Assert.That(tracker.CurrentStep, Is.EqualTo("move"));

            Run(simulation, tracker, new PlayerCommand { DashPressed = true }, 0.1f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("move"), "dash does not complete move");

            Run(simulation, tracker, new PlayerCommand { Move = new Vector2(0f, 1f) }, hold * 0.5f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("move"), "needs holdSeconds of movement");
            Run(simulation, tracker, new PlayerCommand { Move = new Vector2(0f, 1f) }, hold * 0.6f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("look"));

            float yaw = 0f;
            for (int i = 0; i < SecondsToTicks(2f) && tracker.CurrentStep == "look"; i++)
            {
                yaw += 2f;
                simulation.Step(new PlayerCommand { LookYaw = yaw });
                tracker.Observe(simulation);
            }

            Assert.That(tracker.CurrentStep, Is.EqualTo("precision"));
            Assert.That(yaw, Is.GreaterThanOrEqualTo(TutorialSettings.LookDegrees));

            Run(simulation, tracker, new PlayerCommand { PrecisionHeld = true, Move = new Vector2(1f, 0f), LookYaw = yaw }, hold * 1.1f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("dash"));

            Run(simulation, tracker, new PlayerCommand { DashPressed = true, LookYaw = yaw }, 0.1f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("attach"));
            Assert.That(tracker.CompletedCount, Is.EqualTo(4));
        }

        [Test]
        public void Stage1_AttachHideCo2SuckDetach_CompleteByCoreEvents()
        {
            var simulation = Stage("stage01", out var level);
            var tracker = new TutorialTracker(level.Tutorial, TutorialSettings, startIndex: level.Tutorial.ToList().IndexOf("attach"));
            var player = simulation.Player;

            // 착지: 붙을 수 있는 가구 윗면 가까이에서 착지 입력.
            var table = simulation.World.Shapes.First(shape => shape.Id == "coffee_table_top");
            var top = ShapeGeometry.Closest(table, table.Center + (Vector3.UnitY * 1000f));
            player.Position = top.Point + (top.Normal * 1f);
            Tick(simulation, tracker, new PlayerCommand { AttachPressed = true });
            Assert.That(tracker.CurrentStep, Is.EqualTo("hide"));

            Tick(simulation, tracker, new PlayerCommand { AttachPressed = true });
            Assert.That(player.State, Is.EqualTo(PlayerState.Flying));
            var shadow = simulation.World.Shapes.First(shape => shape.Id == "shadow_curtain");
            player.Position = shadow.Center;
            Run(simulation, tracker, PlayerCommand.None, 0.05f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("co2"));

            var site = simulation.Human.SkinSites.First(s => s.PartId == "forearmR");
            player.Position = site.Shape.Center + (Vector3.UnitY * (TutorialSettings.Co2ReachRange * 0.5f));
            Run(simulation, tracker, PlayerCommand.None, 0.05f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("suck"));

            TestHumans.DisableReactions(simulation);
            TestHumans.PlaceNearPart(simulation, "forearmR");
            Tick(simulation, tracker, new PlayerCommand { AttachPressed = true });
            Run(simulation, tracker, new PlayerCommand { SuckHeld = true }, 0.5f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("detach"));

            Tick(simulation, tracker, new PlayerCommand { AttachPressed = true });

            // 마지막은 비행 방식 안내(정보 단계): 읽을 시간이 지나면 끝난다 (gulf §5).
            Assert.That(tracker.CurrentStep, Is.EqualTo("flightMode"));
            Run(simulation, tracker, PlayerCommand.None, TutorialSettings.InfoTimeout + 0.1f);
            Assert.That(tracker.IsComplete, Is.True);
            Assert.That(tracker.CurrentStep, Is.Null);
        }

        [Test]
        public void Stage2_InfoSteps_CompleteByActionOrTimeout()
        {
            var simulation = Stage("stage02", out var level);
            var tracker = new TutorialTracker(level.Tutorial, TutorialSettings);
            Assert.That(tracker.CurrentStep, Is.EqualTo("visionZones"));

            simulation.Player.Position = simulation.Human.HeadCenter + (simulation.Human.HeadForward * 150f);
            Run(simulation, tracker, PlayerCommand.None, 0.2f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("frenzyHide"), "entering the yellow zone completes visionZones");

            simulation.Player.Position = simulation.Human.HeadCenter - (simulation.Human.HeadForward * 400f);
            Run(simulation, tracker, PlayerCommand.None, TutorialSettings.InfoTimeout + 0.1f);
            Assert.That(tracker.CurrentStep, Is.EqualTo("reactionDodge"), "info step times out");
        }
    }
}
