using System;
using Moqui.Core.Collision;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>
    /// Core 충돌 형상을 화이트박스 프리미티브로 그린다. 판정은 Core가 하고, 여기서는 겉모습만 만든다 (tech/architecture.md §4.6).
    /// </summary>
    public static class WorldView
    {
        private const float UnitySphereDiameter = 1f;
        private const float UnityCapsuleHeight = 2f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>밤 실내 팔레트 (spec/10): 라벤더 벽, 따뜻한 나무, 청회색, 베이지, 청록 회색. 그림자는 툰 셰이더가 남보라로 물들인다.</summary>
        private static readonly Color[] Palette =
        {
            new Color(0.66f, 0.62f, 0.76f),
            new Color(0.58f, 0.42f, 0.33f),
            new Color(0.50f, 0.55f, 0.70f),
            new Color(0.76f, 0.62f, 0.50f),
            new Color(0.45f, 0.55f, 0.58f),
        };

        public static Transform Build(CollisionWorld world, Transform parent, ShapeFlags visibleMask = ShapeFlags.Solid)
        {
            var root = new GameObject("WorldView").transform;
            root.SetParent(parent, false);
            foreach (var shape in world.Shapes)
            {
                // 인간 몸 캡슐은 움직이므로 HumanView가 따로 그린다.
                if (shape.Matches(visibleMask) && !shape.Matches(ShapeFlags.Body))
                {
                    CreateShapeObject(shape, root);
                }
            }

            return root;
        }

        public static GameObject CreateShapeObject(CollisionShape shape, Transform parent)
        {
            PrimitiveType primitive;
            switch (shape.Type)
            {
                case ShapeType.Box:
                    primitive = PrimitiveType.Cube;
                    break;
                case ShapeType.Sphere:
                    primitive = PrimitiveType.Sphere;
                    break;
                case ShapeType.Capsule:
                    primitive = PrimitiveType.Capsule;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }

            // 판정은 Core가 하므로 Unity 콜라이더는 쓰지 않는다. 화풍은 공통 툰 셰이더 (spec/10).
            GameObject visual = Art.Primitives.Create(primitive, shape.Id, parent);
            ApplyPose(shape, visual.transform);
            Tint(visual.GetComponent<Renderer>(), ColorFor(shape.Id));
            return visual;
        }

        /// <summary>형상의 현재 위치·회전·크기를 프리미티브에 반영한다 (월드 좌표).</summary>
        public static void ApplyPose(CollisionShape shape, Transform visual)
        {
            switch (shape.Type)
            {
                case ShapeType.Box:
                    visual.SetPositionAndRotation(shape.Center.ToUnity(), shape.Rotation.ToUnity());
                    visual.localScale = (shape.HalfExtents * 2f).ToUnity();
                    break;
                case ShapeType.Sphere:
                    visual.SetPositionAndRotation(shape.Center.ToUnity(), Quaternion.identity);
                    visual.localScale = Vector3.one * (shape.Radius * 2f / UnitySphereDiameter);
                    break;
                case ShapeType.Capsule:
                    Vector3 a = shape.PointA.ToUnity();
                    Vector3 b = shape.PointB.ToUnity();
                    Vector3 axis = b - a;
                    float diameter = shape.Radius * 2f;
                    Quaternion rotation = axis.sqrMagnitude > 0f ? Quaternion.FromToRotation(Vector3.up, axis) : Quaternion.identity;
                    visual.SetPositionAndRotation((a + b) * 0.5f, rotation);
                    visual.localScale = new Vector3(diameter, (axis.magnitude + diameter) / UnityCapsuleHeight, diameter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }
        }

        public static void Tint(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(block);
        }

        private static Color ColorFor(string id)
        {
            int hash = 0;
            foreach (char c in id)
            {
                hash = unchecked((hash * 31) + c);
            }

            return Palette[Mathf.Abs(hash % Palette.Length)];
        }
    }
}
