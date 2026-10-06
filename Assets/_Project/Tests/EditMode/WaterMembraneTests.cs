using Moqui.Unity.Presentation;
using NUnit.Framework;

namespace Moqui.Unity.Tests
{
    /// <summary>물방울 탈출 막 (gulf §9): 진행에 따라 늘어나고, 박자가 맞으면 더 크게 출렁인다.</summary>
    public class WaterMembraneTests
    {
        [Test]
        public void Membrane_GrowsWithProgress()
        {
            Assert.That(WaterView.Membrane(0f, 0f), Is.EqualTo(1f));
            Assert.That(WaterView.Membrane(1f, 0f), Is.EqualTo(1f + WaterView.MembraneGrowAtFull));
            Assert.That(WaterView.Membrane(0.5f, 0.1f), Is.GreaterThan(WaterView.Membrane(0.5f, 0f)));
        }

        [Test]
        public void Rhythm_StretchesMore()
        {
            Assert.That(WaterView.StretchFor(0.25f), Is.EqualTo(WaterView.RhythmStretch));
            Assert.That(WaterView.StretchFor(1.5f), Is.EqualTo(WaterView.PressStretch), "too slow");
            Assert.That(WaterView.StretchFor(0.05f), Is.EqualTo(WaterView.PressStretch), "mashing");
        }
    }
}
