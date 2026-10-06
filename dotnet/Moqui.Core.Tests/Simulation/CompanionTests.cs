using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>두 사람 (spec/02 §10, M14).</summary>
    public class CompanionTests
    {
        private static readonly Vector3 FriendPosition = new Vector3(300f, 0f, 0f);

        private static GameSimulation TwoHumans(Vector3 spawn)
        {
            var seated = TestHumans.Seated();
            var friend = new HumanDefinition("friend", FriendPosition, 0f, seated.Parts, seated.HeadPartId, seated.ShoulderLocals, seated.IdleLookYaws, seated.Actions, seated.Traits);
            var setup = new SimulationSetup(new CollisionWorld(), spawn, seated, TestHumans.DefaultSeed, companions: new[] { friend });
            var simulation = new GameSimulation(Settings, setup);
            foreach (var human in simulation.Humans)
            {
                simulation.HumanSystemOf(human).Reactions.ExtraMultiplier = 0f;
                simulation.HumanSystemOf(human).Reactions.LandingSkillMultiplier = 0f;
                simulation.HumanSystemOf(human).SuckEvents.Enabled = false;
            }

            return simulation;
        }

        [Test]
        public void EachHuman_SensesOnItsOwn()
        {
            Vector3 inFrontOfFriend = FriendPosition + new Vector3(0f, TestHumans.HeadHeight, 150f);
            var simulation = TwoHumans(inFrontOfFriend);
            var friend = simulation.Humans[1];

            Run(simulation, PlayerCommand.None, SecondsToTicks(1f));

            Assert.That(simulation.Humans.Count, Is.EqualTo(2));
            Assert.That(friend.Awareness, Is.GreaterThan(5f), "the friend sees the mosquito in front of it");
            Assert.That(simulation.Human.Awareness, Is.EqualTo(0f), "the other human is looking elsewhere");
        }

        [Test]
        public void Sucking_TheFriendsArm_FeedsAndMarksTheFriend()
        {
            var simulation = TwoHumans(new Vector3(0f, 300f, -400f));
            var friend = simulation.Humans[1];
            var forearm = friend.Shapes["forearmR"];
            Vector3 outward = Vector3.UnitX;
            var surface = ShapeGeometry.Closest(forearm, forearm.Center + (outward * 50f));
            simulation.Player.Position = surface.Point + (surface.Normal * 1f);
            simulation.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(simulation.Player.State, Is.EqualTo(PlayerState.Attached));

            Run(simulation, new PlayerCommand { SuckHeld = true }, SecondsToTicks(3f));
            Assert.That(simulation.Player.BloodGauge, Is.GreaterThan(5f));
            friend.TryGetSite(forearm, out var site);
            Assert.That(site.Itch, Is.GreaterThan(0f));

            simulation.Step(new PlayerCommand { AttachPressed = true });
            Assert.That(friend.BiteMarkCount, Is.EqualTo(1));
            Assert.That(simulation.Human.BiteMarkCount, Is.EqualTo(0));
        }

        [Test]
        public void Frenzy_SpreadsAlarmToTheOtherHuman()
        {
            var simulation = TwoHumans(new Vector3(0f, 300f, -400f));
            var friend = simulation.Humans[1];
            simulation.Human.HasStimulus = true;
            simulation.Human.LastStimulusPosition = new Vector3(50f, 100f, 50f);
            TestHumans.Provoke(simulation, 100f);

            simulation.Step(PlayerCommand.None);

            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));
            Assert.That(friend.Awareness, Is.GreaterThanOrEqualTo(Settings.HumanMotion.AlarmShare - 1e-3f));
            Assert.That(friend.LastStimulusPosition, Is.EqualTo(new Vector3(50f, 100f, 50f)));
            simulation.Step(PlayerCommand.None);
            Assert.That(friend.State, Is.EqualTo(AwarenessState.Suspicious), "alarmed, looking at what the other one saw");
        }
    }
}
