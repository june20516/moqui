using System;
using System.Collections.Generic;
using System.Numerics;

namespace Moqui.Core.Collision
{
    /// <summary>
    /// 기본 도형 충돌 월드 (tech/architecture.md §4.5). 형상 수가 수백 개 수준이라 전수 검사한다.
    /// </summary>
    public sealed class CollisionWorld
    {
        private const float MinDirectionLengthSquared = 1e-12f;

        private readonly List<CollisionShape> _shapes = new List<CollisionShape>();
        private readonly Dictionary<string, CollisionShape> _byId = new Dictionary<string, CollisionShape>();

        public IReadOnlyList<CollisionShape> Shapes => _shapes;

        public void Add(CollisionShape shape)
        {
            if (_byId.ContainsKey(shape.Id))
            {
                throw new ArgumentException($"Duplicate shape id '{shape.Id}'.", nameof(shape));
            }

            _shapes.Add(shape);
            _byId.Add(shape.Id, shape);
        }

        public bool Remove(string id)
        {
            if (!_byId.TryGetValue(id, out var shape))
            {
                return false;
            }

            _byId.Remove(id);
            _shapes.Remove(shape);
            return true;
        }

        public bool TryGet(string id, out CollisionShape shape)
        {
            return _byId.TryGetValue(id, out shape);
        }

        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, ShapeFlags mask, out CollisionHit hit, ShapeFlags exclude = ShapeFlags.None)
        {
            return SphereSweep(origin, 0f, direction, maxDistance, mask, out hit, exclude);
        }

        /// <summary>구를 direction 방향으로 distance만큼 이동시킬 때 가장 먼저 닿는 형상. exclude 플래그를 가진 형상은 무시한다.</summary>
        public bool SphereSweep(Vector3 center, float radius, Vector3 direction, float distance, ShapeFlags mask, out CollisionHit hit, ShapeFlags exclude = ShapeFlags.None)
        {
            hit = default;
            if (direction.LengthSquared() < MinDirectionLengthSquared)
            {
                return false;
            }

            Vector3 unit = Vector3.Normalize(direction);
            bool found = false;
            float best = float.MaxValue;
            foreach (var shape in _shapes)
            {
                if (!shape.Matches(mask) || shape.Matches(exclude))
                {
                    continue;
                }

                if (ShapeGeometry.Cast(shape, center, unit, distance, radius, out var candidate) && candidate.Distance < best)
                {
                    best = candidate.Distance;
                    hit = candidate;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>구와 겹치는 형상을 results에 채운다 (기존 내용은 지운다).</summary>
        public int Overlap(Vector3 center, float radius, ShapeFlags mask, List<CollisionShape> results)
        {
            results.Clear();
            foreach (var shape in _shapes)
            {
                if (shape.Matches(mask) && ShapeGeometry.Closest(shape, center).Distance <= radius)
                {
                    results.Add(shape);
                }
            }

            return results.Count;
        }

        public bool AnyOverlap(Vector3 center, float radius, ShapeFlags mask)
        {
            foreach (var shape in _shapes)
            {
                if (shape.Matches(mask) && ShapeGeometry.Closest(shape, center).Distance <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>point에서 maxDistance 안에 있는 가장 가까운 표면 (부착 거리와 법선).</summary>
        public bool ClosestSurface(Vector3 point, float maxDistance, ShapeFlags mask, out SurfacePoint surface)
        {
            surface = default;
            bool found = false;
            float best = float.MaxValue;
            foreach (var shape in _shapes)
            {
                if (!shape.Matches(mask))
                {
                    continue;
                }

                var candidate = ShapeGeometry.Closest(shape, point);
                if (candidate.Distance <= maxDistance && candidate.Distance < best)
                {
                    best = candidate.Distance;
                    surface = candidate;
                    found = true;
                }
            }

            return found;
        }
    }
}
