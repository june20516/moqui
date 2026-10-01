using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    public class RetryTests
    {
        [Test]
        public void Retry_AfterPlaying_EveryStateMatchesFreshStart()
        {
            var setup = new SimulationSetup(() => new CollisionWorld(), TestHumans.InFront(100f), TestHumans.Seated(), TestHumans.DefaultSeed);
            var played = new GameSimulation(Settings, setup);
            string fresh = SnapshotDescriber.Describe(played.CaptureSnapshot());

            // 상태를 최대한 어지럽힌다: 경계·광분·공격, 위치·스태미나, 가려움·자국.
            TestHumans.Provoke(played, 100f);
            Run(played, new PlayerCommand { Move = new Vector2(1f, 0f), DashPressed = true }, SecondsToTicks(3f));
            foreach (var site in played.Human.SkinSites)
            {
                site.Itch = 80f;
                site.HasBiteMark = true;
            }

            played.Human.BiteMarkCount = 3;
            Assert.That(SnapshotDescriber.Describe(played.CaptureSnapshot()), Is.Not.EqualTo(fresh), "the played state differs");

            var retried = played.Retry();

            Assert.That(SnapshotDescriber.Describe(retried.CaptureSnapshot()), Is.EqualTo(fresh));
        }

        [Test]
        public void Retry_SameCommands_ReplaysIdentically()
        {
            var setup = new SimulationSetup(() => new CollisionWorld(), TestHumans.InFront(100f), TestHumans.Seated(), TestHumans.DefaultSeed);
            var first = new GameSimulation(Settings, setup);
            TestHumans.Provoke(first, 100f);
            Run(first, PlayerCommand.None, SecondsToTicks(2f));

            var retry = first.Retry();
            TestHumans.Provoke(retry, 100f);
            Run(retry, PlayerCommand.None, SecondsToTicks(2f));

            Assert.That(SnapshotDescriber.Describe(retry.CaptureSnapshot()), Is.EqualTo(SnapshotDescriber.Describe(first.CaptureSnapshot())));
        }

        [Test]
        public void Setup_FromWorldInstance_CannotRetry()
        {
            var setup = new SimulationSetup(new CollisionWorld(), Vector3.Zero, TestHumans.Seated());
            var simulation = new GameSimulation(Settings, setup);

            Assert.Throws<System.InvalidOperationException>(() => simulation.Retry());
        }

        [Test]
        public void SkinSites_SeatedHuman_TypesFromDefinition()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);

            var types = simulation.Human.SkinSites.Select(site => site.Type).OrderBy(type => type).ToList();

            Assert.That(types, Is.EqualTo(new[] { SkinSiteType.Forearm, SkinSiteType.Forearm, SkinSiteType.Calf, SkinSiteType.Calf, SkinSiteType.Cheek }));
            Assert.That(Settings.Sites.Sensitivity(SkinSiteType.Cheek), Is.EqualTo(2.2f));
            Assert.That(Settings.Sites.BloodAmount(SkinSiteType.FootTop), Is.EqualTo(0.7f));
        }
    }
}
