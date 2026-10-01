namespace Moqui.Core.Random
{
    public interface IRandom
    {
        ulong NextULong();

        /// <summary>[0, 1) 범위의 실수.</summary>
        double NextDouble();

        /// <summary>[minInclusive, maxExclusive) 범위의 정수.</summary>
        int NextInt(int minInclusive, int maxExclusive);
    }
}
