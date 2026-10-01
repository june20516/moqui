using Moqui.Core.Random;
using NUnit.Framework;

namespace Moqui.Core.Tests.Random
{
    public class SplitMix64RandomTests
    {
        private const ulong Seed = 12345UL;
        private const int SampleCount = 1000;

        [Test]
        public void NextULong_SameSeed_ProducesSameSequence()
        {
            var a = new SplitMix64Random(Seed);
            var b = new SplitMix64Random(Seed);

            for (int i = 0; i < SampleCount; i++)
            {
                Assert.That(b.NextULong(), Is.EqualTo(a.NextULong()));
            }
        }

        [Test]
        public void NextULong_KnownSeed_MatchesReferenceValue()
        {
            // 참고 구현(Vigna, splitmix64.c)의 seed=0 첫 출력. 다른 언어 이식 시 같은 값을 내야 한다.
            var random = new SplitMix64Random(0UL);

            Assert.That(random.NextULong(), Is.EqualTo(0xE220A8397B1DCDAFUL));
        }

        [Test]
        public void NextDouble_ManySamples_StaysInUnitRange()
        {
            var random = new SplitMix64Random(Seed);

            for (int i = 0; i < SampleCount; i++)
            {
                Assert.That(random.NextDouble(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }

        [Test]
        public void NextInt_ManySamples_StaysInRange()
        {
            var random = new SplitMix64Random(Seed);

            for (int i = 0; i < SampleCount; i++)
            {
                Assert.That(random.NextInt(-3, 4), Is.InRange(-3, 3));
            }
        }
    }
}
