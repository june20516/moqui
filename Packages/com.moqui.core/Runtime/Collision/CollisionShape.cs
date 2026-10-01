using System;
using System.Numerics;

namespace Moqui.Core.Collision
{
    public enum ShapeType
    {
        Box,
        Sphere,
        Capsule,
    }

    /// <summary>
    /// 판정용 기본 도형. Box는 회전 포함 OBB(중심, 반크기, 회전), Sphere는 중심과 반지름,
    /// Capsule은 선분 양 끝(PointA, PointB)과 반지름으로 정의한다. 위치를 바꿀 수 있는 형상(인간 몸 캡슐)은 Set* 메서드로 갱신한다.
    /// </summary>
    public sealed class CollisionShape
    {
        private CollisionShape(string id, ShapeType type, ShapeFlags flags)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type;
            Flags = flags;
            Rotation = Quaternion.Identity;
            InverseRotation = Quaternion.Identity;
        }

        public string Id { get; }

        public ShapeType Type { get; }

        public ShapeFlags Flags { get; set; }

        /// <summary>Box·Sphere의 중심. Capsule은 두 끝점의 중점.</summary>
        public Vector3 Center { get; private set; }

        public Vector3 HalfExtents { get; private set; }

        public Quaternion Rotation { get; private set; }

        public Quaternion InverseRotation { get; private set; }

        public float Radius { get; private set; }

        public Vector3 PointA { get; private set; }

        public Vector3 PointB { get; private set; }

        public static CollisionShape Box(string id, Vector3 center, Vector3 halfExtents, Quaternion rotation, ShapeFlags flags)
        {
            if (halfExtents.X < 0 || halfExtents.Y < 0 || halfExtents.Z < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(halfExtents));
            }

            var shape = new CollisionShape(id, ShapeType.Box, flags) { HalfExtents = halfExtents };
            shape.SetPose(center, rotation);
            return shape;
        }

        public static CollisionShape Box(string id, Vector3 center, Vector3 halfExtents, ShapeFlags flags)
        {
            return Box(id, center, halfExtents, Quaternion.Identity, flags);
        }

        public static CollisionShape Sphere(string id, Vector3 center, float radius, ShapeFlags flags)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            return new CollisionShape(id, ShapeType.Sphere, flags) { Center = center, Radius = radius };
        }

        public static CollisionShape Capsule(string id, Vector3 pointA, Vector3 pointB, float radius, ShapeFlags flags)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            var shape = new CollisionShape(id, ShapeType.Capsule, flags) { Radius = radius };
            shape.SetSegment(pointA, pointB);
            return shape;
        }

        public bool Matches(ShapeFlags mask)
        {
            return (Flags & mask) != 0;
        }

        public void SetPose(Vector3 center, Quaternion rotation)
        {
            if (Type != ShapeType.Box)
            {
                Center = center;
                return;
            }

            Center = center;
            Rotation = Quaternion.Normalize(rotation);
            InverseRotation = Quaternion.Conjugate(Rotation);
        }

        public void SetSegment(Vector3 pointA, Vector3 pointB)
        {
            if (Type != ShapeType.Capsule)
            {
                throw new InvalidOperationException($"Shape '{Id}' is not a capsule.");
            }

            PointA = pointA;
            PointB = pointB;
            Center = (pointA + pointB) * 0.5f;
        }

        public Vector3 ToLocal(Vector3 worldPoint)
        {
            return Vector3.Transform(worldPoint - Center, InverseRotation);
        }

        public Vector3 ToLocalDirection(Vector3 worldDirection)
        {
            return Vector3.Transform(worldDirection, InverseRotation);
        }

        public Vector3 ToWorld(Vector3 localPoint)
        {
            return Vector3.Transform(localPoint, Rotation) + Center;
        }

        public Vector3 ToWorldDirection(Vector3 localDirection)
        {
            return Vector3.Transform(localDirection, Rotation);
        }
    }
}
