using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Data.Levels
{
    /// <summary>
    /// 방·레벨 데이터의 판정 형상 하나 (tech/architecture.md §5 room JSON).
    /// box: center, size(전체 크기), rotation(선택, 도 단위 [x, y, z]. 보통 [0, yaw, 0]만 쓴다).
    /// sphere: center, radius. capsule: a, b, radius.
    /// </summary>
    public sealed class ShapeDefinition
    {
        private const float DegreesToRadians = MathF.PI / 180f;

        private static readonly Dictionary<string, ShapeFlags> FlagNames = new Dictionary<string, ShapeFlags>(StringComparer.Ordinal)
        {
            ["obstacle"] = ShapeFlags.Obstacle,
            ["attachable"] = ShapeFlags.Attachable,
            ["skinSite"] = ShapeFlags.SkinSite,
            ["shadowZone"] = ShapeFlags.ShadowZone,
            ["hazard"] = ShapeFlags.Hazard,
            ["wind"] = ShapeFlags.Wind,
            ["glass"] = ShapeFlags.Glass,
            ["humidWeak"] = ShapeFlags.HumidWeak,
            ["humidStrong"] = ShapeFlags.HumidStrong,
            ["netGap"] = ShapeFlags.NetGap,
            ["net"] = ShapeFlags.Net,
        };

        private ShapeDefinition(string id, ShapeType type, ShapeFlags flags)
        {
            Id = id;
            Type = type;
            Flags = flags;
        }

        public string Id { get; }

        public ShapeType Type { get; }

        public ShapeFlags Flags { get; }

        public Vector3 Center { get; private set; }

        /// <summary>박스 전체 크기 (W×H×D).</summary>
        public Vector3 Size { get; private set; }

        /// <summary>박스 회전 (도, x/y/z축).</summary>
        public Vector3 RotationDegrees { get; private set; }

        public float Radius { get; private set; }

        public Vector3 PointA { get; private set; }

        public Vector3 PointB { get; private set; }

        public static ShapeDefinition Box(string id, Vector3 center, Vector3 size, ShapeFlags flags, Vector3 rotationDegrees = default)
        {
            return new ShapeDefinition(id, ShapeType.Box, flags) { Center = center, Size = size, RotationDegrees = rotationDegrees };
        }

        public static ShapeDefinition Parse(JsonAccess json)
        {
            string id = json.Get("id").String();
            var type = json.Get("type").Enum<ShapeType>();
            var flags = ParseFlags(json.Get("flags"));
            var shape = new ShapeDefinition(id, type, flags);
            switch (type)
            {
                case ShapeType.Box:
                    shape.Center = json.Get("center").Vector3();
                    shape.Size = json.Get("size").Vector3();
                    shape.RotationDegrees = json.Has("rotation") ? json.Get("rotation").Vector3() : Vector3.Zero;
                    if (shape.Size.X <= 0 || shape.Size.Y <= 0 || shape.Size.Z <= 0)
                    {
                        throw json.Get("size").Error("must be positive");
                    }

                    break;
                case ShapeType.Sphere:
                    shape.Center = json.Get("center").Vector3();
                    shape.Radius = json.Get("radius").Float();
                    break;
                case ShapeType.Capsule:
                    shape.PointA = json.Get("a").Vector3();
                    shape.PointB = json.Get("b").Vector3();
                    shape.Center = (shape.PointA + shape.PointB) * 0.5f;
                    shape.Radius = json.Get("radius").Float();
                    break;
            }

            return shape;
        }

        public static ShapeFlags ParseFlags(JsonAccess json)
        {
            var flags = ShapeFlags.None;
            foreach (var item in json.Items())
            {
                string name = item.String();
                if (!FlagNames.TryGetValue(name, out var flag))
                {
                    throw item.Error($"unknown flag '{name}'");
                }

                flags |= flag;
            }

            return flags;
        }

        public CollisionShape ToCollisionShape()
        {
            switch (Type)
            {
                case ShapeType.Box:
                    var rotation = Quaternion.CreateFromYawPitchRoll(
                        RotationDegrees.Y * DegreesToRadians,
                        RotationDegrees.X * DegreesToRadians,
                        RotationDegrees.Z * DegreesToRadians);
                    return CollisionShape.Box(Id, Center, Size * 0.5f, rotation, Flags);
                case ShapeType.Sphere:
                    return CollisionShape.Sphere(Id, Center, Radius, Flags);
                default:
                    return CollisionShape.Capsule(Id, PointA, PointB, Radius, Flags);
            }
        }
    }
}
