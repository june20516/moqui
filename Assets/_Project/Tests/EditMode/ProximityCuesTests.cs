using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation.Player;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>3D 거리감 (gulf §4): 중력 방향·가장 가까운 표면 그림자, 지면 효과 먼지.</summary>
    public class ProximityCuesTests
    {
        private ProximityCues _cues;

        [TearDown]
        public void TearDown()
        {
            _cues?.Destroy();
        }

        private static GameSimulation Stage01()
        {
            var tuning = TuningLoader.Load(new UnityDataSource());
            var level = new LevelLoader(new UnityDataSource()).Load("stage01");
            return new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
        }

        [Test]
        public void Shadow_DarkerAndSmallerWhenCloser()
        {
            Assert.That(ProximityCues.ShadowAlpha(5f), Is.GreaterThan(ProximityCues.ShadowAlpha(40f)));
            Assert.That(ProximityCues.ShadowDiameter(5f), Is.LessThan(ProximityCues.ShadowDiameter(40f)));
            Assert.That(ProximityCues.ShadowAlpha(ProximityCues.ShadowRange), Is.EqualTo(0f));
        }

        [Test]
        public void FlyingLowOverTheFloor_ShowsShadowAndKicksUpDust()
        {
            var simulation = Stage01();
            var low = new Vector3(0f, 5f, -250f);
            simulation.Player.Position = low.ToCore();
            _cues = new ProximityCues(null);

            for (int i = 0; i < 10; i++)
            {
                _cues.Refresh(simulation, low, 0.05f);
            }

            Assert.That(_cues.GravityShadowVisible, Is.True);
            Assert.That(_cues.ActiveDust, Is.GreaterThan(0), "close to the floor, the wings stir up dust");

            var high = new Vector3(0f, 200f, -250f);
            for (int i = 0; i < 30; i++)
            {
                _cues.Refresh(simulation, high, 0.05f);
            }

            Assert.That(_cues.ActiveDust, Is.Zero, "far from surfaces, old dust fades and none is added");
        }
    }
}
