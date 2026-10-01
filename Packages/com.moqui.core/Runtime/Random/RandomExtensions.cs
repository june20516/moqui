using System;
using System.Numerics;

namespace Moqui.Core.Random
{
    public static class RandomExtensions
    {
        /// <summary>[min, max] 범위의 실수.</summary>
        public static float Range(this IRandom random, float min, float max)
        {
            return min + ((max - min) * (float)random.NextDouble());
        }

        /// <summary>확률 probability로 참.</summary>
        public static bool Chance(this IRandom random, double probability)
        {
            return random.NextDouble() < probability;
        }

        /// <summary>반지름 radius인 구 안의 균일한 무작위 점 (거부 표본추출).</summary>
        public static Vector3 InsideSphere(this IRandom random, float radius)
        {
            while (true)
            {
                var point = new Vector3(
                    (float)((random.NextDouble() * 2.0) - 1.0),
                    (float)((random.NextDouble() * 2.0) - 1.0),
                    (float)((random.NextDouble() * 2.0) - 1.0));
                if (point.LengthSquared() <= 1f)
                {
                    return point * radius;
                }
            }
        }

        public static float NextFloat(this IRandom random)
        {
            return (float)random.NextDouble();
        }

        public static int Pick(this IRandom random, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return random.NextInt(0, count);
        }
    }
}
