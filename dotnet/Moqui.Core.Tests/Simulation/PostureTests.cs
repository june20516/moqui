using System;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>인간 자세 (spec/07 Stage 3 누운 자세, Stage 4 고개 숙임, D-047).</summary>
    public class PostureTests
    {
        private static HumanDefinition WithPosture(float facingPitch, float restPitch)
        {
            var seated = TestHumans.Seated();
            return new HumanDefinition(seated.Id, seated.Position, seated.FacingYaw, seated.Parts, seated.HeadPartId, seated.ShoulderLocals, seated.IdleLookYaws, null, null, facingPitch, restPitch);
        }

        [Test]
        public void Lying_FacingPitch90_LooksUp_SeesPlayerAboveNotInFront()
        {
            var human = new Human(WithPosture(90f, 0f), new CollisionWorld());
            Assert.That(Vector3.Distance(human.HeadForward, Vector3.UnitY), Is.LessThan(1e-4f), "face up");

            var above = TestHumans.Simulation(human.HeadCenter + new Vector3(0f, 150f, 0f), human: WithPosture(90f, 0f));
            above.Step(PlayerCommand.None);
            Assert.That(above.Human.PlayerVisible, Is.True, "ceiling side is in view");

            var level = TestHumans.Simulation(human.HeadCenter + new Vector3(0f, 0f, 150f), human: WithPosture(90f, 0f));
            level.Step(PlayerCommand.None);
            Assert.That(level.Human.PlayerVisible, Is.False, "horizontal front is outside the cone when lying");
        }

        [Test]
        public void HeadDown_RestPitchMinus35_GazeStaysDownWhileCalm()
        {
            var simulation = TestHumans.Simulation(TestHumans.FarBehind, human: WithPosture(0f, -35f));
            Run(simulation, PlayerCommand.None, SecondsToTicks(3f));

            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Safe));
            Assert.That(simulation.Human.HeadPitch, Is.EqualTo(-35f).Within(0.01f));
            Assert.That(simulation.Human.HeadForward.Y, Is.EqualTo(MathF.Sin(-35f * MathF.PI / 180f)).Within(1e-3f));
        }
    }
}
