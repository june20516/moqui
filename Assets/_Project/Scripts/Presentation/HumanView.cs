using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>
    /// 툰 인간 표현. Core 상태를 읽어서 그리기만 한다:
    /// 몸 형상 위치(공격하는 팔도 Core 팔 캡슐 그대로, D-052), 머리 방향으로 도는 얼굴(눈·코), 어그로 상태 색,
    /// 공격 판정 위치 표시 (spec/02, spec/10).
    /// </summary>
    public sealed class HumanView : MonoBehaviour
    {
        private const float NoseLength = 6f;
        private const float NoseThickness = 2f;

        // 얼굴 부위 배치 (머리 반지름 배수).
        private const float EyeSpacing = 0.38f;
        private const float EyeHeight = 0.25f;
        private const float EyeDepth = 0.82f;
        private const float EyeSize = 0.28f;

        private static readonly Color SkinColor = new Color(0.86f, 0.70f, 0.58f);
        private static readonly Color EyeColor = new Color(0.15f, 0.12f, 0.22f);
        private static readonly Color SafeHeadColor = new Color(0.45f, 0.75f, 0.45f);
        private static readonly Color SuspiciousHeadColor = new Color(0.95f, 0.80f, 0.25f);
        private static readonly Color FrenzyHeadColor = new Color(0.90f, 0.20f, 0.15f);
        private static readonly Color TelegraphColor = new Color(1f, 0.55f, 0.1f);

        [SerializeField]
        private SimulationRunner _runner;

        private readonly List<KeyValuePair<CollisionShape, Transform>> _parts = new List<KeyValuePair<CollisionShape, Transform>>();
        private Human _human;
        private Renderer _headRenderer;
        private Transform _face;
        private Transform _attackMarker;
        private Renderer _attackRenderer;
        private Transform _approach;
        private Renderer _approachRenderer;
        private MaterialPropertyBlock _telegraphBlock;

        public bool IsBuilt => _human != null;

        /// <summary>얼굴 피벗. 앞(+Z)이 Core의 머리 방향이다.</summary>
        public Transform Face => _face;

        public void Build(Human human)
        {
            _human = human;
            foreach (var shape in human.Shapes.Values)
            {
                GameObject part = WorldView.CreateShapeObject(shape, transform);
                WorldView.Tint(part.GetComponent<Renderer>(), SkinColor);
                _parts.Add(new KeyValuePair<CollisionShape, Transform>(shape, part.transform));
                if (shape == human.HeadShape)
                {
                    _headRenderer = part.GetComponent<Renderer>();
                }
            }

            BuildFace(human.HeadShape.Radius);

            // 공격 예고: 카메라를 향한 판에 좁혀 오는 고리·차오름·번쩍임 (화면에서 가장 높은 위상, M12).
            _attackMarker = Art.Primitives.Create(PrimitiveType.Quad, "AttackTarget", transform, Art.ToonMaterials.Telegraph).transform;
            _attackRenderer = _attackMarker.GetComponent<Renderer>();
            _attackRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _telegraphBlock = new MaterialPropertyBlock();

            // 손이 오는 방향: 손바닥에서 목표까지 가는 줄기.
            _approach = Art.Primitives.Create(PrimitiveType.Capsule, "AttackApproach", transform, Art.ToonMaterials.Transparent).transform;
            _approachRenderer = _approach.GetComponent<Renderer>();
            _approachRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Refresh(0);
        }

        public void Refresh(int tick)
        {
            foreach (var part in _parts)
            {
                WorldView.ApplyPose(part.Key, part.Value);
            }

            Vector3 forward = _human.HeadForward.ToUnity();
            Vector3 faceUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            _face.SetPositionAndRotation(_human.HeadCenter.ToUnity(), Quaternion.LookRotation(forward, faceUp));
            WorldView.Tint(_headRenderer, HeadColor(_human.State));

            RefreshAttack(tick);
        }

        public static Color HeadColor(AwarenessState state)
        {
            switch (state)
            {
                case AwarenessState.Suspicious:
                    return SuspiciousHeadColor;
                case AwarenessState.Frenzy:
                    return FrenzyHeadColor;
                default:
                    return SafeHeadColor;
            }
        }

        /// <summary>예고 판 크기 = 판정 지름 ÷ 셰이더의 판정 반경 비율 (판 가장자리에서 판정 크기로 좁혀 온다).</summary>
        private const float TelegraphHitRadiusOfQuad = 0.34f;
        private const float ApproachThickness = 1.2f;
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int StrikeId = Shader.PropertyToID("_Strike");

        /// <summary>현재 공격 예고 진행률 0~1 (판정 중은 1). 예고가 없으면 0.</summary>
        public float TelegraphProgress { get; private set; }

        /// <summary>공격 예고 판 (예고·판정 중에만 보인다).</summary>
        public Renderer TelegraphIndicator => _attackRenderer;

        private void RefreshAttack(int tick)
        {
            var attack = _human.Attack;
            bool telegraph = attack.Phase == AttackPhase.Telegraph;
            bool strike = attack.Phase == AttackPhase.Active;
            _attackMarker.gameObject.SetActive(telegraph || strike);
            TelegraphProgress = strike ? 1f : telegraph ? Mathf.Clamp01((float)(tick - attack.StartTick) / Mathf.Max(1, attack.TelegraphEndTick - attack.StartTick)) : 0f;
            if (telegraph || strike)
            {
                Vector3 target = attack.Target.ToUnity();
                _attackMarker.position = target;
                _attackMarker.localScale = Vector3.one * (2f * attack.Radius / TelegraphHitRadiusOfQuad);
                var camera = UnityEngine.Camera.main;
                if (camera != null && (target - camera.transform.position).sqrMagnitude > 1e-4f)
                {
                    _attackMarker.rotation = Quaternion.LookRotation(target - camera.transform.position, camera.transform.up);
                }

                _attackRenderer.GetPropertyBlock(_telegraphBlock);
                _telegraphBlock.SetFloat(ProgressId, TelegraphProgress);
                _telegraphBlock.SetFloat(StrikeId, strike ? 1f : 0f);
                _attackRenderer.SetPropertyBlock(_telegraphBlock);
            }

            int arm = attack.ArmA;
            bool approach = telegraph && arm >= 0 && attack.Kind != AttackKind.Spray;
            _approach.gameObject.SetActive(approach);
            if (approach)
            {
                PlaceBetween(_approach, _human.Palm(arm).ToUnity(), attack.Target.ToUnity(), ApproachThickness);
                var color = TelegraphColor;
                color.a = 0.15f + (0.45f * TelegraphProgress);
                WorldView.Tint(_approachRenderer, color);
            }
        }

        /// <summary>단위 캡슐(높이 2, Y축)을 두 점 사이에 놓는다.</summary>
        private static void PlaceBetween(Transform capsule, Vector3 from, Vector3 to, float thickness)
        {
            Vector3 span = to - from;
            float length = Mathf.Max(span.magnitude, thickness);
            capsule.position = (from + to) * 0.5f;
            capsule.rotation = span.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(Vector3.up, span) : Quaternion.identity;
            capsule.localScale = new Vector3(thickness, length * 0.5f, thickness);
        }

        private void BuildFace(float headRadius)
        {
            _face = new GameObject("Face").transform;
            _face.SetParent(transform, false);
            foreach (float side in new[] { -1f, 1f })
            {
                Transform eye = CreateMarker(PrimitiveType.Sphere, side < 0f ? "EyeL" : "EyeR", _face);
                eye.localPosition = new Vector3(side * EyeSpacing, EyeHeight, EyeDepth) * headRadius;
                eye.localScale = Vector3.one * (EyeSize * headRadius);
                WorldView.Tint(eye.GetComponent<Renderer>(), EyeColor);
            }

            Transform nose = CreateMarker(PrimitiveType.Cube, "HeadDirection", _face);
            nose.localPosition = new Vector3(0f, 0f, headRadius + (NoseLength * 0.5f));
            nose.localScale = new Vector3(NoseThickness, NoseThickness, NoseLength);
            WorldView.Tint(nose.GetComponent<Renderer>(), SkinColor);
        }

        private void LateUpdate()
        {
            if (_human == null && _runner != null && _runner.IsRunning && _runner.Driver.Simulation.Human != null)
            {
                Build(_runner.Driver.Simulation.Human);
            }

            if (_human != null)
            {
                Refresh(_runner != null && _runner.IsRunning ? _runner.Driver.Simulation.Tick : 0);
            }
        }

        private static Transform CreateMarker(PrimitiveType primitive, string markerName, Transform parent)
        {
            return Art.Primitives.Create(primitive, markerName, parent).transform;
        }
    }
}
