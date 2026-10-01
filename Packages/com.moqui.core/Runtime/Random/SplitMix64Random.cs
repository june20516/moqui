using System;

namespace Moqui.Core.Random
{
    /// <summary>
    /// 플랫폼과 런타임에 관계없이 같은 시드에서 같은 수열을 내는 난수기.
    /// System.Random은 런타임마다 구현이 달라 결정성을 보장하지 못하므로 쓰지 않는다.
    /// </summary>
    public sealed class SplitMix64Random : IRandom
    {
        private const ulong Gamma = 0x9E3779B97F4A7C15UL;
        private const ulong Mix1 = 0xBF58476D1CE4E5B9UL;
        private const ulong Mix2 = 0x94D049BB133111EBUL;
        private const int DoubleMantissaBits = 53;
        private const double DoubleUnit = 1.0 / (1UL << DoubleMantissaBits);

        private ulong _state;

        public SplitMix64Random(ulong seed)
        {
            _state = seed;
        }

        public ulong NextULong()
        {
            _state += Gamma;
            ulong z = _state;
            z = (z ^ (z >> 30)) * Mix1;
            z = (z ^ (z >> 27)) * Mix2;
            return z ^ (z >> 31);
        }

        public double NextDouble()
        {
            return (NextULong() >> (64 - DoubleMantissaBits)) * DoubleUnit;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            ulong range = (ulong)((long)maxExclusive - minInclusive);
            return (int)((long)minInclusive + (long)(NextULong() % range));
        }
    }
}
