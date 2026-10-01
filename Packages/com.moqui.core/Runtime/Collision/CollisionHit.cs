using System.Numerics;

namespace Moqui.Core.Collision
{
    /// <summary>
    /// Raycast/SphereSweep 결과. Distance는 원점(구 중심)이 이동한 거리, Point는 표면 접촉점,
    /// Normal은 표면에서 바깥을 향하는 단위 법선이다. 시작부터 겹쳐 있으면 Distance = 0, StartedInside = true.
    /// </summary>
    public readonly struct CollisionHit
    {
        public CollisionHit(CollisionShape shape, float distance, Vector3 point, Vector3 normal, bool startedInside)
        {
            Shape = shape;
            Distance = distance;
            Point = point;
            Normal = normal;
            StartedInside = startedInside;
        }

        public CollisionShape Shape { get; }

        public float Distance { get; }

        public Vector3 Point { get; }

        public Vector3 Normal { get; }

        public bool StartedInside { get; }
    }

    /// <summary>
    /// ClosestSurface 결과. Distance는 질의점에서 표면까지의 부호 있는 거리 (안쪽이면 음수).
    /// </summary>
    public readonly struct SurfacePoint
    {
        public SurfacePoint(CollisionShape shape, Vector3 point, Vector3 normal, float distance)
        {
            Shape = shape;
            Point = point;
            Normal = normal;
            Distance = distance;
        }

        public CollisionShape Shape { get; }

        public Vector3 Point { get; }

        public Vector3 Normal { get; }

        public float Distance { get; }
    }
}
