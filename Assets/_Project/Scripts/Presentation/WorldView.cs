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

        private static readonly Color[] Palette =
        {
            new Color(0.78f, 0.74f, 0.68f),
            new Color(0.55f, 0.42f, 0.32f),
            new Color(0.62f, 0.66f, 0.70f),
            new Color(0.70f, 0.60f, 0.45f),
            new Color(0.45f, 0.52f, 0.47f),
        };

        public static Transform Build(CollisionWorld world, Transform parent, ShapeFlags visibleMask = ShapeFlags.Obstacle)
        {
            var root = new GameObject("WorldView").transform;
            root.SetParent(parent, false);
            foreach (var shape in world.Shapes)
            {
                if (shape.Matches(visibleMask))
                {
                    CreateShapeObject(shape, root);
                }
            }

            return root;
        }

        public static GameObject CreateShapeObject(CollisionShape shape, Transform parent)
        {
            GameObject visual;
            switch (shape.Type)
            {
                case ShapeType.Box:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.transform.SetPositionAndRotation(shape.Center.ToUnity(), shape.Rotation.ToUnity());
                    visual.transform.localScale = (shape.HalfExtents * 2f).ToUnity();
                    break;
                case ShapeType.Sphere:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    visual.transform.position = shape.Center.ToUnity();
                    visual.transform.localScale = Vector3.one * (shape.Radius * 2f / UnitySphereDiameter);
                    break;
                case ShapeType.Capsule:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Vector3 a = shape.PointA.ToUnity();
                    Vector3 b = shape.PointB.ToUnity();
                    Vector3 axis = b - a;
                    float diameter = shape.Radius * 2f;
                    visual.transform.position = (a + b) * 0.5f;
                    visual.transform.rotation = axis.sqrMagnitude > 0f ? Quaternion.FromToRotation(Vector3.up, axis) : Quaternion.identity;
                    visual.transform.localScale = new Vector3(diameter, (axis.magnitude + diameter) / UnityCapsuleHeight, diameter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape));
            }

            visual.name = shape.Id;
            visual.transform.SetParent(parent, true);

            // 판정은 Core가 하므로 Unity 콜라이더는 쓰지 않는다.
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            Tint(visual.GetComponent<Renderer>(), ColorFor(shape.Id));
            return visual;
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
