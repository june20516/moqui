using System;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Random;
using NUnit.Framework;

namespace Moqui.Core.Tests.Collision
{
    /// <summary>
    /// 해석적 스윕 결과를 "부호 있는 거리로 잘게 전진하는" 무차별 방식과 비교한다.
    /// 접선에 가까운 경우(최소 간격이 표본 간격 수준)는 판정이 모호하므로 건너뛴다.
    /// </summary>
    public class ShapeCastCrossCheckTests
    {
        private const int CaseCount = 3000;
        private const float MarchStep = 0.002f;
        private const float MaxDistance = 12f;
        private const float AmbiguousGap = 0.01f;

        [TestCase(ShapeType.Box)]
        [TestCase(ShapeType.Sphere)]
        [TestCase(ShapeType.Capsule)]
        public void Cast_RandomRays_AgreesWithMarching(ShapeType type)
        {
            var random = new SplitMix64Random(20261001UL + (ulong)type);
            int compared = 0;
            for (int i = 0; i < CaseCount; i++)
            {
                var shape = RandomShape(random, type);
                Vector3 origin = RandomVector(random, 6f);

                // 대부분은 형상 쪽으로, 일부는 아무 방향(멀어지는 광선 포함)으로 쏜다.
                Vector3 direction = random.NextDouble() < 0.7
                    ? Vector3.Normalize((shape.Center + RandomVector(random, 2f)) - origin)
                    : Vector3.Normalize(RandomVector(random, 1f) + new Vector3(1e-3f));
                float radius = (float)(random.NextDouble() * 1.0);

                bool analytic = ShapeGeometry.Cast(shape, origin, direction, MaxDistance, radius, out var hit);
                March(shape, origin, direction, radius, out bool marched, out float marchedDistance, out float minGap);

                if (MathF.Abs(minGap) < AmbiguousGap)
                {
                    continue;
                }

                compared++;
                Assert.That(analytic, Is.EqualTo(marched), $"case {i}: hit mismatch (minGap={minGap})");
                if (analytic && !hit.StartedInside)
                {
                    Assert.That(hit.Distance, Is.EqualTo(marchedDistance).Within(MarchStep * 2f), $"case {i}: distance mismatch");
                    var surface = ShapeGeometry.Closest(shape, origin + (direction * hit.Distance));
                    Assert.That(surface.Distance, Is.EqualTo(radius).Within(1e-3f), $"case {i}: hit center not at radius");
                }
            }

            Assert.That(compared, Is.GreaterThan(CaseCount / 2));
        }

        private static void March(CollisionShape shape, Vector3 origin, Vector3 direction, float radius, out bool hit, out float distance, out float minGap)
        {
            hit = false;
            distance = 0f;
            minGap = float.MaxValue;
            // 접선 여부를 판단하려고 첫 접촉 이후에도 광선 끝까지 최소 간격을 잰다.
            for (float t = 0f; t <= MaxDistance; t += MarchStep)
            {
                float gap = ShapeGeometry.Closest(shape, origin + (direction * t)).Distance - radius;
                minGap = Math.Min(minGap, gap);
                if (gap <= 0f && !hit)
                {
                    hit = true;
                    distance = t;
                }
            }
        }

        private static CollisionShape RandomShape(IRandom random, ShapeType type)
        {
            Vector3 center = RandomVector(random, 1f);
            switch (type)
            {
                case ShapeType.Box:
                    var axis = Vector3.Normalize(RandomVector(random, 1f) + new Vector3(0.01f));
                    var rotation = Quaternion.CreateFromAxisAngle(axis, (float)(random.NextDouble() * Math.PI * 2));
                    var extents = new Vector3(Positive(random), Positive(random), Positive(random));
                    return CollisionShape.Box("box", center, extents, rotation, ShapeFlags.Obstacle);
                case ShapeType.Sphere:
                    return CollisionShape.Sphere("sphere", center, Positive(random), ShapeFlags.Obstacle);
                default:
                    Vector3 half = RandomVector(random, 1.5f);
                    return CollisionShape.Capsule("capsule", center - half, center + half, Positive(random), ShapeFlags.Obstacle);
            }
        }

        private static float Positive(IRandom random)
        {
            return 0.1f + (float)(random.NextDouble() * 1.5);
        }

        private static Vector3 RandomVector(IRandom random, float scale)
        {
            return new Vector3(
                (float)((random.NextDouble() * 2) - 1) * scale,
                (float)((random.NextDouble() * 2) - 1) * scale,
                (float)((random.NextDouble() * 2) - 1) * scale);
        }
    }
}
