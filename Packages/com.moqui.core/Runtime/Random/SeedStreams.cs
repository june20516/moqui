namespace Moqui.Core.Random
{
    /// <summary>
    /// 레벨 시드에서 용도별 난수 스트림을 파생한다 (tech/architecture.md §4.1).
    /// 한 시스템의 난수 소비가 바뀌어도 다른 시스템의 난수열은 바뀌지 않는다.
    /// 스트림 이름은 FNV-1a 64비트로 해시한다 (string.GetHashCode는 실행마다 달라진다, D-029).
    /// </summary>
    public static class SeedStreams
    {
        public const string HumanActions = "humanActions";
        public const string Reactions = "reactions";
        public const string BlindSwat = "blindSwat";
        public const string Debuff = "debuff";
        public const string Spray = "spray";
        public const string Doze = "doze";
        public const string Glance = "glance";

        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static IRandom Create(ulong levelSeed, string streamName)
        {
            return new SplitMix64Random(Derive(levelSeed, streamName));
        }

        public static ulong Derive(ulong levelSeed, string streamName)
        {
            // 한 번 섞어서 이웃한 레벨 시드끼리도 스트림 시드가 멀리 떨어지게 한다.
            return new SplitMix64Random(levelSeed ^ Fnv1a64(streamName)).NextULong();
        }

        public static ulong Fnv1a64(string text)
        {
            ulong hash = FnvOffset;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= FnvPrime;
            }

            return hash;
        }
    }
}
