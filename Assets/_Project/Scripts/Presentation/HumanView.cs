using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>
    /// 캡슐 인간의 화이트박스 표현 (M2). Core 상태를 읽어서 그리기만 한다:
    /// 몸 캡슐 위치, 머리 방향 표시, 어그로 상태 색, 공격 예고·판정 위치 표시.
    /// </summary>
    public sealed class HumanView : MonoBehaviour
    {
        private const float NoseLength = 6f;
        private const float NoseThickness = 2f;

        private static readonly Color SkinColor = new Color(0.86f, 0.70f, 0.58f);
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
        private Transform _nose;
        private Transform _attackMarker;
        private Renderer _attackRenderer;

        public bool IsBuilt => _human != null;

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

            _nose = CreateMarker(PrimitiveType.Cube, "HeadDirection");
            _nose.localScale = new Vector3(NoseThickness, NoseThickness, NoseLength);
            _attackMarker = CreateMarker(PrimitiveType.Sphere, "AttackTarget");
            _attackRenderer = _attackMarker.GetComponent<Renderer>();
            Refresh();
        }

        public void Refresh()
        {
            foreach (var part in _parts)
            {
                WorldView.ApplyPose(part.Key, part.Value);
            }

            Vector3 forward = _human.HeadForward.ToUnity();
            Vector3 headCenter = _human.HeadCenter.ToUnity();
            _nose.SetPositionAndRotation(headCenter + (forward * (_human.HeadShape.Radius + (NoseLength * 0.5f))), Quaternion.LookRotation(forward));
            WorldView.Tint(_headRenderer, HeadColor(_human.State));

            var attack = _human.Attack;
            bool showAttack = attack.Phase == AttackPhase.Telegraph || attack.Phase == AttackPhase.Active;
            _attackMarker.gameObject.SetActive(showAttack);
            if (showAttack)
            {
                _attackMarker.position = attack.Target.ToUnity();
                _attackMarker.localScale = Vector3.one * (attack.Radius * 2f);
                WorldView.Tint(_attackRenderer, attack.Phase == AttackPhase.Active ? ActiveColor : TelegraphColor);
            }
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

        private void LateUpdate()
        {
            if (_human == null && _runner != null && _runner.IsRunning && _runner.Driver.Simulation.Human != null)
            {
                Build(_runner.Driver.Simulation.Human);
            }

            if (_human != null)
            {
                Refresh();
            }
        }

        private Transform CreateMarker(PrimitiveType primitive, string markerName)
        {
            var marker = GameObject.CreatePrimitive(primitive);
            marker.name = markerName;
            marker.transform.SetParent(transform, false);
            DestroyImmediate(marker.GetComponent<Collider>());
            return marker.transform;
        }
    }
}
