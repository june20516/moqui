using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation.Player
{
    /// <summary>
    /// 3D 거리감 (gulf §4, 표현 전용): 모키 아래(중력 방향)와 가장 가까운 표면에 둥근 그림자를 놓고, 가까울수록 진하고 작게 한다.
    /// 표면 가까이 날면 날갯바람에 먼지가 일어 표면을 따라 바깥으로 퍼진다(지면 효과).
    /// </summary>
    public sealed class ProximityCues
    {
        /// <summary>그림자가 보이기 시작하는 거리 (u). 이보다 멀면 숨긴다.</summary>
        public const float ShadowRange = 60f;

        /// <summary>가장 가까울 때 그림자 진하기, 지름 (u). 멀어지면 옅어지고 커진다.</summary>
        public const float ShadowMaxAlpha = 0.55f;
        public const float ShadowNearDiameter = 1.2f;
        public const float ShadowFarDiameter = 4f;

        /// <summary>표면에서 띄우는 높이 (u). 겹침 깜빡임 방지.</summary>
        public const float ShadowLift = 0.04f;

        /// <summary>지면 효과: 이 거리 안에서 날면 먼지가 인다 (u), 먼지 하나의 수명(s)과 내는 간격(s), 퍼지는 속도(u/s).</summary>
        public const float DustRange = 10f;
        public const float DustLifetime = 0.7f;
        public const float DustInterval = 0.08f;
        public const float DustSpeed = 8f;
        private const int DustPoolSize = 24;

        private static readonly Color ShadowColor = new Color(0.16f, 0.08f, 0.28f, 1f);
        private static readonly Color DustColor = new Color(0.85f, 0.82f, 0.9f, 1f);

        private readonly Transform _root;
        private readonly Renderer _gravityShadow;
        private readonly Renderer _nearestShadow;
        private readonly List<Dust> _dust = new List<Dust>();
        private float _dustTimer;
        private int _dustCursor;
        private uint _dustSeed = 1;

        private sealed class Dust
        {
            public Transform Transform;
            public Renderer Renderer;
            public Vector3 Velocity;
            public float Age = float.PositiveInfinity;
        }

        public ProximityCues(Transform parent)
        {
            _root = new GameObject("ProximityCues").transform;
            _root.SetParent(parent, false);
            _gravityShadow = CreateDisc("GravityShadow");
            _nearestShadow = CreateDisc("NearestShadow");
            for (int i = 0; i < DustPoolSize; i++)
            {
                var puff = Art.Primitives.Create(PrimitiveType.Sphere, "Dust", _root, Art.ToonMaterials.Transparent);
                var renderer = puff.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.enabled = false;
                _dust.Add(new Dust { Transform = puff.transform, Renderer = renderer });
            }
        }

        /// <summary>중력 방향 그림자가 보이는가 (테스트용).</summary>
        public bool GravityShadowVisible => _gravityShadow.enabled;

        /// <summary>가장 가까운 표면 그림자가 보이는가 (테스트용).</summary>
        public bool NearestShadowVisible => _nearestShadow.enabled;

        /// <summary>지금 보이는 먼지 수 (테스트용).</summary>
        public int ActiveDust
        {
            get
            {
                int count = 0;
                foreach (var dust in _dust)
                {
                    count += dust.Renderer.enabled ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>거리 → 그림자 진하기 (가까울수록 진함, ShadowRange에서 0).</summary>
        public static float ShadowAlpha(float distance) => ShadowMaxAlpha * Mathf.Clamp01(1f - (distance / ShadowRange));

        /// <summary>거리 → 그림자 지름 (가까울수록 작고 또렷함).</summary>
        public static float ShadowDiameter(float distance) => Mathf.Lerp(ShadowNearDiameter, ShadowFarDiameter, Mathf.Clamp01(distance / ShadowRange));

        public void Refresh(GameSimulation simulation, Vector3 position, float deltaTime)
        {
            var player = simulation.Player;
            bool flying = player.State == PlayerState.Flying || player.State == PlayerState.Dashing;
            var world = simulation.World;
            var origin = position.ToCore();

            CollisionHit floorHit = default;
            SurfacePoint surface = default;
            bool down = flying && world.Raycast(origin, -System.Numerics.Vector3.UnitY, ShadowRange, ShapeFlags.Solid, out floorHit);
            Place(_gravityShadow, down, down ? floorHit.Point.ToUnity() : Vector3.zero, down ? floorHit.Normal.ToUnity() : Vector3.up, down ? floorHit.Distance : 0f);

            bool near = flying && world.ClosestSurface(origin, ShadowRange, ShapeFlags.Solid, out surface);
            // 바로 아래 그림자와 같은 자리면 하나만 보인다.
            bool distinct = near && (!down || Vector3.Distance(surface.Point.ToUnity(), floorHit.Point.ToUnity()) > ShadowNearDiameter);
            Place(_nearestShadow, distinct, distinct ? surface.Point.ToUnity() : Vector3.zero, distinct ? surface.Normal.ToUnity() : Vector3.up, distinct ? surface.Distance : 0f);

            UpdateDust(flying && near && surface.Distance < DustRange, near ? surface.Point.ToUnity() : Vector3.zero, near ? surface.Normal.ToUnity() : Vector3.up, deltaTime);
        }

        public void Destroy()
        {
            if (_root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_root.gameObject);
            }
            else
            {
                Object.DestroyImmediate(_root.gameObject);
            }
        }

        private Renderer CreateDisc(string name)
        {
            var disc = Art.Primitives.Create(PrimitiveType.Cylinder, name, _root, Art.ToonMaterials.Transparent);
            var renderer = disc.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.enabled = false;
            return renderer;
        }

        private static void Place(Renderer disc, bool visible, Vector3 point, Vector3 normal, float distance)
        {
            disc.enabled = visible && ShadowAlpha(distance) > 0f;
            if (!disc.enabled)
            {
                return;
            }

            float diameter = ShadowDiameter(distance);
            disc.transform.SetPositionAndRotation(point + (normal * ShadowLift), Quaternion.FromToRotation(Vector3.up, normal));
            disc.transform.localScale = new Vector3(diameter, 0.005f, diameter);
            WorldView.Tint(disc, new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, ShadowAlpha(distance)));
        }

        private void UpdateDust(bool emitting, Vector3 point, Vector3 normal, float deltaTime)
        {
            if (emitting)
            {
                _dustTimer += deltaTime;
                while (_dustTimer >= DustInterval)
                {
                    _dustTimer -= DustInterval;
                    Spawn(point, normal);
                }
            }
            else
            {
                _dustTimer = 0f;
            }

            foreach (var dust in _dust)
            {
                if (!dust.Renderer.enabled)
                {
                    continue;
                }

                dust.Age += deltaTime;
                if (dust.Age >= DustLifetime)
                {
                    dust.Renderer.enabled = false;
                    continue;
                }

                float t = dust.Age / DustLifetime;
                dust.Transform.position += dust.Velocity * deltaTime;
                dust.Transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t);
                WorldView.Tint(dust.Renderer, new Color(DustColor.r, DustColor.g, DustColor.b, 0.35f * (1f - t)));
            }
        }

        private void Spawn(Vector3 point, Vector3 normal)
        {
            var dust = _dust[_dustCursor];
            _dustCursor = (_dustCursor + 1) % _dust.Count;
            Vector3 tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            float angle = Next01() * 360f;
            Vector3 outward = Quaternion.AngleAxis(angle, normal) * tangent;
            dust.Transform.position = point + (normal * 0.3f);
            dust.Velocity = (outward * DustSpeed) + (normal * (DustSpeed * 0.15f));
            dust.Age = 0f;
            dust.Renderer.enabled = true;
        }

        private float Next01()
        {
            _dustSeed = (_dustSeed * 1664525u) + 1013904223u;
            return (_dustSeed >> 8) / (float)(1u << 24);
        }
    }
}
