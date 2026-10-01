using System.Collections.Generic;
using System.Numerics;

namespace Moqui.Core.Collision
{
    /// <summary>
    /// 구를 충돌 월드 안에서 이동시킨다. 막히면 그 자리에서 멈추고 남은 이동을 표면을 따라 미끄러뜨린다 (spec/01 이동).
    /// </summary>
    public sealed class SphereMover
    {
        /// <summary>표면과 남겨 두는 간격. 부동소수 오차로 다음 틱에 겹쳐 시작하는 것을 막는다.</summary>
        public const float Skin = 0.01f;

        private const int MaxSlideIterations = 4;
        private const int MaxDepenetrationIterations = 4;
        private const float MinMoveLength = 1e-5f;

        private readonly CollisionWorld _world;
        private readonly List<CollisionShape> _overlaps = new List<CollisionShape>();

        public SphereMover(CollisionWorld world)
        {
            _world = world;
        }

        /// <summary>
        /// position에서 displacement만큼 이동한 결과 위치를 돌려준다. velocity는 닿은 표면을 파고드는 성분이 제거된다.
        /// </summary>
        public MoveResult Move(Vector3 position, float radius, Vector3 displacement, Vector3 velocity, ShapeFlags mask)
        {
            position = Depenetrate(position, radius, mask);
            bool collided = false;
            Vector3 lastNormal = Vector3.Zero;
            Vector3 remaining = displacement;

            for (int i = 0; i < MaxSlideIterations; i++)
            {
                float length = remaining.Length();
                if (length < MinMoveLength)
                {
                    break;
                }

                Vector3 direction = remaining / length;
                if (!_world.SphereSweep(position, radius, direction, length + Skin, mask, out var hit))
                {
                    position += remaining;
                    break;
                }

                collided = true;
                lastNormal = hit.Normal;
                float travel = hit.StartedInside ? 0f : System.Math.Max(0f, System.Math.Min(length, hit.Distance - Skin));
                position += direction * travel;
                remaining = direction * (length - travel);
                remaining = RemoveInto(remaining, hit.Normal);
                velocity = RemoveInto(velocity, hit.Normal);
            }

            return new MoveResult(position, velocity, collided, lastNormal);
        }

        /// <summary>겹친 장애물에서 법선 방향으로 밀어낸다.</summary>
        public Vector3 Depenetrate(Vector3 position, float radius, ShapeFlags mask)
        {
            for (int i = 0; i < MaxDepenetrationIterations; i++)
            {
                if (_world.Overlap(position, radius, mask, _overlaps) == 0)
                {
                    break;
                }

                foreach (var shape in _overlaps)
                {
                    var surface = ShapeGeometry.Closest(shape, position);
                    float push = radius - surface.Distance + Skin;
                    if (push > 0f)
                    {
                        position += surface.Normal * push;
                    }
                }
            }

            return position;
        }

        private static Vector3 RemoveInto(Vector3 vector, Vector3 normal)
        {
            float into = Vector3.Dot(vector, normal);
            return into < 0f ? vector - (normal * into) : vector;
        }
    }

    public readonly struct MoveResult
    {
        public MoveResult(Vector3 position, Vector3 velocity, bool collided, Vector3 lastNormal)
        {
            Position = position;
            Velocity = velocity;
            Collided = collided;
            LastNormal = lastNormal;
        }

        public Vector3 Position { get; }

        public Vector3 Velocity { get; }

        public bool Collided { get; }

        public Vector3 LastNormal { get; }
    }
}
