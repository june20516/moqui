using System;
using System.Numerics;

namespace Moqui.Core.Collision
{
    /// <summary>
    /// 기본 도형의 최근접점과 광선/구 스윕 계산.
    /// 구 스윕은 "형상을 구 반지름만큼 부풀린 도형"에 대한 광선 검사로 바꿔 푼다.
    /// 부풀린 박스(둥근 박스)는 면 방향으로만 늘린 박스 3개와 모서리 캡슐 12개의 합집합과 같으므로, 각 요소의 최솟값을 쓴다.
    /// </summary>
    public static class ShapeGeometry
    {
        private const float Epsilon = 1e-6f;

        private static readonly Vector3 FallbackNormal = Vector3.UnitY;

        public static SurfacePoint Closest(CollisionShape shape, Vector3 point)
        {
            switch (shape.Type)
            {
                case ShapeType.Sphere:
                    return ClosestOnRoundedPoint(shape, shape.Center, shape.Radius, point);
                case ShapeType.Capsule:
                    return ClosestOnRoundedPoint(shape, ClosestPointOnSegment(shape.PointA, shape.PointB, point), shape.Radius, point);
                case ShapeType.Box:
                    return ClosestOnBox(shape, point);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }
        }

        /// <summary>
        /// 반지름 radius인 구를 origin에서 direction(단위 벡터)으로 maxDistance까지 이동시켜 형상과 처음 닿는 지점을 구한다.
        /// radius가 0이면 광선 검사이다.
        /// </summary>
        public static bool Cast(CollisionShape shape, Vector3 origin, Vector3 direction, float maxDistance, float radius, out CollisionHit hit)
        {
            var start = Closest(shape, origin);
            if (start.Distance <= radius)
            {
                hit = new CollisionHit(shape, 0f, start.Point, start.Normal, true);
                return true;
            }

            float distance;
            bool found;
            switch (shape.Type)
            {
                case ShapeType.Sphere:
                    found = RaySphere(origin, direction, shape.Center, shape.Radius + radius, maxDistance, out distance);
                    break;
                case ShapeType.Capsule:
                    found = RayCapsule(origin, direction, shape.PointA, shape.PointB, shape.Radius + radius, maxDistance, out distance);
                    break;
                case ShapeType.Box:
                    found = RayRoundedBox(shape, origin, direction, maxDistance, radius, out distance);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }

            if (!found)
            {
                hit = default;
                return false;
            }

            Vector3 center = origin + (direction * distance);
            var surface = Closest(shape, center);
            hit = new CollisionHit(shape, distance, surface.Point, surface.Normal, false);
            return true;
        }

        public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
        {
            Vector3 ab = b - a;
            float lengthSquared = ab.LengthSquared();
            if (lengthSquared < Epsilon)
            {
                return a;
            }

            float t = Math.Clamp(Vector3.Dot(point - a, ab) / lengthSquared, 0f, 1f);
            return a + (ab * t);
        }

        public static bool RaySphere(Vector3 origin, Vector3 direction, Vector3 center, float radius, float maxDistance, out float distance)
        {
            distance = 0f;
            Vector3 m = origin - center;
            float b = Vector3.Dot(m, direction);
            float c = m.LengthSquared() - (radius * radius);
            if (c > 0f && b > 0f)
            {
                return false;
            }

            float discriminant = (b * b) - c;
            if (discriminant < 0f)
            {
                return false;
            }

            distance = Math.Max(0f, -b - MathF.Sqrt(discriminant));
            return distance <= maxDistance;
        }

        public static bool RayCapsule(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, float radius, float maxDistance, out float distance)
        {
            distance = float.MaxValue;
            bool found = false;

            if (RaySphere(origin, direction, a, radius, maxDistance, out float t) && t < distance)
            {
                distance = t;
                found = true;
            }

            if (RaySphere(origin, direction, b, radius, maxDistance, out t) && t < distance)
            {
                distance = t;
                found = true;
            }

            Vector3 axis = b - a;
            float axisLength = axis.Length();
            if (axisLength > Epsilon)
            {
                Vector3 axisUnit = axis / axisLength;
                Vector3 w = origin - a;
                Vector3 dPerp = direction - (axisUnit * Vector3.Dot(direction, axisUnit));
                Vector3 wPerp = w - (axisUnit * Vector3.Dot(w, axisUnit));
                float qa = dPerp.LengthSquared();
                if (qa > Epsilon)
                {
                    float qb = 2f * Vector3.Dot(wPerp, dPerp);
                    float qc = wPerp.LengthSquared() - (radius * radius);
                    float discriminant = (qb * qb) - (4f * qa * qc);
                    if (discriminant >= 0f)
                    {
                        float tc = Math.Max(0f, (-qb - MathF.Sqrt(discriminant)) / (2f * qa));
                        float along = Vector3.Dot(origin + (direction * tc) - a, axisUnit);
                        if (tc <= maxDistance && along >= 0f && along <= axisLength && tc < distance)
                        {
                            distance = tc;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>원점 중심 축 정렬 박스에 대한 슬랩 검사. 원점이 안에 있으면 0.</summary>
        public static bool RayAabb(Vector3 origin, Vector3 direction, Vector3 halfExtents, float maxDistance, out float distance)
        {
            float tMin = 0f;
            float tMax = maxDistance;
            distance = 0f;
            for (int axis = 0; axis < 3; axis++)
            {
                float o = Component(origin, axis);
                float d = Component(direction, axis);
                float e = Component(halfExtents, axis);
                if (MathF.Abs(d) < Epsilon)
                {
                    if (o < -e || o > e)
                    {
                        return false;
                    }

                    continue;
                }

                float inverse = 1f / d;
                float t1 = (-e - o) * inverse;
                float t2 = (e - o) * inverse;
                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);
                if (tMin > tMax)
                {
                    return false;
                }
            }

            distance = tMin;
            return true;
        }

        private static bool RayRoundedBox(CollisionShape box, Vector3 origin, Vector3 direction, float maxDistance, float radius, out float distance)
        {
            Vector3 localOrigin = box.ToLocal(origin);
            Vector3 localDirection = box.ToLocalDirection(direction);
            Vector3 e = box.HalfExtents;

            if (!RayAabb(localOrigin, localDirection, e + new Vector3(radius), maxDistance, out distance))
            {
                return false;
            }

            if (radius <= 0f)
            {
                return true;
            }

            distance = float.MaxValue;
            bool found = false;
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 slab = e + (Unit(axis) * radius);
                if (RayAabb(localOrigin, localDirection, slab, maxDistance, out float t) && t < distance)
                {
                    distance = t;
                    found = true;
                }
            }

            for (int axis = 0; axis < 3; axis++)
            {
                int j = (axis + 1) % 3;
                int k = (axis + 2) % 3;
                for (int sj = -1; sj <= 1; sj += 2)
                {
                    for (int sk = -1; sk <= 1; sk += 2)
                    {
                        Vector3 offset = (Unit(j) * (sj * Component(e, j))) + (Unit(k) * (sk * Component(e, k)));
                        Vector3 along = Unit(axis) * Component(e, axis);
                        if (RayCapsule(localOrigin, localDirection, offset - along, offset + along, radius, maxDistance, out float t) && t < distance)
                        {
                            distance = t;
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        private static SurfacePoint ClosestOnRoundedPoint(CollisionShape shape, Vector3 core, float radius, Vector3 point)
        {
            Vector3 offset = point - core;
            float length = offset.Length();
            Vector3 normal = length > Epsilon ? offset / length : FallbackNormal;
            return new SurfacePoint(shape, core + (normal * radius), normal, length - radius);
        }

        private static SurfacePoint ClosestOnBox(CollisionShape box, Vector3 point)
        {
            Vector3 local = box.ToLocal(point);
            Vector3 e = box.HalfExtents;
            Vector3 clamped = Vector3.Clamp(local, -e, e);
            Vector3 outside = local - clamped;
            float outsideLength = outside.Length();
            if (outsideLength > Epsilon)
            {
                return new SurfacePoint(box, box.ToWorld(clamped), box.ToWorldDirection(outside / outsideLength), outsideLength);
            }

            // 안쪽(또는 표면 위): 가장 가까운 면으로 밀어낸다.
            int bestAxis = 0;
            float bestGap = float.MaxValue;
            for (int axis = 0; axis < 3; axis++)
            {
                float gap = Component(e, axis) - MathF.Abs(Component(local, axis));
                if (gap < bestGap)
                {
                    bestGap = gap;
                    bestAxis = axis;
                }
            }

            float sign = Component(local, bestAxis) >= 0f ? 1f : -1f;
            Vector3 localNormal = Unit(bestAxis) * sign;
            Vector3 surface = local + (localNormal * bestGap);
            return new SurfacePoint(box, box.ToWorld(surface), box.ToWorldDirection(localNormal), -bestGap);
        }

        private static float Component(Vector3 v, int axis)
        {
            return axis == 0 ? v.X : axis == 1 ? v.Y : v.Z;
        }

        private static Vector3 Unit(int axis)
        {
            return axis == 0 ? Vector3.UnitX : axis == 1 ? Vector3.UnitY : Vector3.UnitZ;
        }
    }
}
