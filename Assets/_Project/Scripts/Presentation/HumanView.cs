using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>
    /// 툰 인간 표현. Core 상태를 읽어서 그리기만 한다:
    /// 몸 형상 위치, 머리 방향으로 도는 얼굴(눈·코), 어그로 상태 색,
    /// 공격 3단계 팔(예고 → 타격 → 회복)과 판정 위치 표시 (spec/02, spec/10).
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

        // 공격 팔 굵기 (머리 반지름 배수).
        private const float ArmThickness = 0.5f;
        private const float FistSize = 0.9f;

        private static readonly Color SkinColor = new Color(0.86f, 0.70f, 0.58f);
        private static readonly Color EyeColor = new Color(0.15f, 0.12f, 0.22f);
        private static readonly Color SafeHeadColor = new Color(0.45f, 0.75f, 0.45f);
        private static readonly Color SuspiciousHeadColor = new Color(0.95f, 0.80f, 0.25f);
        private static readonly Color FrenzyHeadColor = new Color(0.90f, 0.20f, 0.15f);
        private static readonly Color TelegraphColor = new Color(1f, 0.55f, 0.1f);
        private static readonly Color ActiveColor = new Color(1f, 0.1f, 0.1f);

        [SerializeField]
        private SimulationRunner _runner;

        private readonly List<KeyValuePair<CollisionShape, Transform>> _parts = new List<KeyValuePair<CollisionShape, Transform>>();
        private Human _human;
        private Renderer _headRenderer;
        private Transform _face;
        private Transform _attackMarker;
        private Renderer _attackRenderer;
        private Transform _attackArm;
        private Transform _attackFist;
        private AttackPhase _observedPhase = AttackPhase.Idle;
        private int _phaseStartTick;

        public bool IsBuilt => _human != null;

        /// <summary>얼굴 피벗. 앞(+Z)이 Core의 머리 방향이다.</summary>
        public Transform Face => _face;

        /// <summary>공격 팔의 주먹 (공격 중에만 보인다).</summary>
        public Transform AttackFist => _attackFist;

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

            _attackArm = CreateMarker(PrimitiveType.Capsule, "AttackArm", transform);
            WorldView.Tint(_attackArm.GetComponent<Renderer>(), SkinColor);
            _attackFist = CreateMarker(PrimitiveType.Sphere, "AttackFist", transform);
            _attackFist.localScale = Vector3.one * (FistSize * human.HeadShape.Radius);
            WorldView.Tint(_attackFist.GetComponent<Renderer>(), SkinColor);

            _attackMarker = CreateMarker(PrimitiveType.Sphere, "AttackTarget", transform);
            _attackRenderer = _attackMarker.GetComponent<Renderer>();
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

        private void RefreshAttack(int tick)
        {
            var attack = _human.Attack;
            if (attack.Phase != _observedPhase)
            {
                _observedPhase = attack.Phase;
                _phaseStartTick = tick;
            }

            bool showTarget = attack.Phase == AttackPhase.Telegraph || attack.Phase == AttackPhase.Active;
            _attackMarker.gameObject.SetActive(showTarget);
            if (showTarget)
            {
                _attackMarker.position = attack.Target.ToUnity();
                _attackMarker.localScale = Vector3.one * (attack.Radius * 2f);
                WorldView.Tint(_attackRenderer, attack.Phase == AttackPhase.Active ? ActiveColor : TelegraphColor);
            }

            bool swinging = attack.IsBusy;
            _attackArm.gameObject.SetActive(swinging);
            _attackFist.gameObject.SetActive(swinging);
            if (!swinging)
            {
                return;
            }

            Vector3 target = attack.Target.ToUnity();
            Vector3 shoulder = _human.NearestShoulder(attack.Target).ToUnity();
            float headRadius = _human.HeadShape.Radius;
            Vector3 windUp = HumanArmPose.WindUp(shoulder, target, Vector3.up, headRadius);
            float progress = HumanArmPose.Progress(tick, _phaseStartTick, HumanArmPose.PhaseEndTick(attack));
            Vector3 hand = HumanArmPose.Hand(attack.Phase, progress, shoulder, windUp, target);

            _attackFist.position = hand;
            PlaceBetween(_attackArm, shoulder, hand, ArmThickness * headRadius);
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
