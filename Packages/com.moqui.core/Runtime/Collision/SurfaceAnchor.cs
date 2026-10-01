using System;
using System.Numerics;

namespace Moqui.Core.Collision
{
    /// <summary>
    /// 형상 표면의 한 점을 형상 기준으로 기억한다. 형상이 움직이면 그 점도 따라간다 (spec/03 부착, spec/02 §6).
    /// Box는 로컬 좌표, Sphere는 중심 기준 방향, Capsule은 축 위치 비율과 법선을 쓴다.
    /// 캡슐 법선은 축이 돈 만큼 같이 돌린다(평행 이동). 고정된 기준 벡터로 프레임을 다시 만들면 축이 기준과 가까워질 때 점이 튄다.
    /// </summary>
    public sealed class SurfaceAnchor
    {
        private const float Epsilon = 1e-6f;

        private Vector3 _local;
        private Vector3 _localNormal;
        private float _segmentT;
        private Vector3 _capsuleNormal;
        private Vector3 _lastAxis;

        private SurfaceAnchor(CollisionShape shape)
        {
            Shape = shape;
        }

        public CollisionShape Shape { get; }

        public static SurfaceAnchor Create(CollisionShape shape, Vector3 surfacePoint, Vector3 normal)
        {
            var anchor = new SurfaceAnchor(shape);
            switch (shape.Type)
            {
                case ShapeType.Box:
                    anchor._local = shape.ToLocal(surfacePoint);
                    anchor._localNormal = shape.ToLocalDirection(normal);
                    break;
                case ShapeType.Sphere:
                    anchor._localNormal = Vector3.Normalize(normal);
                    break;
                case ShapeType.Capsule:
                    Vector3 axis = shape.PointB - shape.PointA;
                    float lengthSquared = axis.LengthSquared();
                    Vector3 core = ShapeGeometry.ClosestPointOnSegment(shape.PointA, shape.PointB, surfacePoint);
                    anchor._segmentT = lengthSquared > Epsilon ? Vector3.Dot(core - shape.PointA, axis) / lengthSquared : 0f;
                    anchor._capsuleNormal = Vector3.Normalize(normal);
                    anchor._lastAxis = lengthSquared > Epsilon ? Vector3.Normalize(axis) : Vector3.UnitY;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }

            return anchor;
        }

        /// <summary>형상의 현재 자세에서 앵커의 표면 점과 바깥 법선.</summary>
        public void Resolve(out Vector3 point, out Vector3 normal)
        {
            switch (Shape.Type)
            {
                case ShapeType.Box:
                    point = Shape.ToWorld(_local);
                    normal = Vector3.Normalize(Shape.ToWorldDirection(_localNormal));
                    return;
                case ShapeType.Sphere:
                    normal = _localNormal;
                    point = Shape.Center + (normal * Shape.Radius);
                    return;
                default:
                    Vector3 axis = Shape.PointB - Shape.PointA;
                    if (axis.LengthSquared() > Epsilon)
                    {
                        Vector3 axisUnit = Vector3.Normalize(axis);
                        _capsuleNormal = Vector3.Transform(_capsuleNormal, FromTo(_lastAxis, axisUnit));
                        _lastAxis = axisUnit;
                    }

                    Vector3 perpendicular = _capsuleNormal - (_lastAxis * Vector3.Dot(_capsuleNormal, _lastAxis));
                    if (perpendicular.LengthSquared() > Epsilon)
                    {
                        _capsuleNormal = Vector3.Normalize(perpendicular);
                    }

                    normal = _capsuleNormal;
                    point = Shape.PointA + (axis * _segmentT) + (normal * Shape.Radius);
                    return;
            }
        }

        /// <summary>단위 벡터 from을 to로 돌리는 최소 회전.</summary>
        public static Quaternion FromTo(Vector3 from, Vector3 to)
        {
            float dot = Math.Clamp(Vector3.Dot(from, to), -1f, 1f);
            Vector3 cross = Vector3.Cross(from, to);
            if (cross.LengthSquared() < Epsilon)
            {
                return Quaternion.Identity;
            }

            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(cross), MathF.Acos(dot));
        }
    }
}
