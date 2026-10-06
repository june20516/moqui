using System.Collections.Generic;
using Moqui.Core.Simulation;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>낙하 중인 물방울을 그린다 (spec/05). Core 스냅샷의 물방울 위치를 따라 구를 놓기만 한다.</summary>
    public sealed class WaterView : MonoBehaviour
    {
        private static readonly Color DropColor = new Color(0.55f, 0.75f, 0.95f, 0.6f);

        [SerializeField]
        private SimulationRunner _runner;

        private readonly List<Transform> _pool = new List<Transform>();
        private Transform _membrane;
        private int _lastPresses;
        private float _stretch;
        private float _lastPressTime = float.NegativeInfinity;

        /// <summary>갇힌 물방울 막 (gulf §9): 탈출 진행에 따라 늘어나고 얇아진다. 누를 때마다 출렁 늘어나고, 박자가 맞으면 더 크게.</summary>
        public const float MembraneGrowAtFull = 0.45f;
        public const float MembraneThinAtFull = 0.65f;
        public const float PressStretch = 0.12f;
        public const float RhythmStretch = 0.22f;
        public const float RhythmMin = 0.12f;
        public const float RhythmMax = 0.4f;
        private const float StretchDecay = 6f;

        /// <summary>지금 막의 크기 배율 (테스트용).</summary>
        public float MembraneScale { get; private set; }

        /// <summary>탈출 진행 0~1과 출렁임 → 막 크기 배율.</summary>
        public static float Membrane(float progress, float stretch) => 1f + (MembraneGrowAtFull * Mathf.Clamp01(progress)) + stretch;

        /// <summary>앞 누름과의 간격(s)이 박자 창 안이면 더 크게 출렁인다.</summary>
        public static float StretchFor(float interval) => interval >= RhythmMin && interval <= RhythmMax ? RhythmStretch : PressStretch;

        public int VisibleDrops { get; private set; }

        /// <summary>물방울 위치 목록을 그린다. 테스트·캡처에서 직접 부를 수 있다.</summary>
        public void Render(IReadOnlyList<System.Numerics.Vector3> drops, float dropRadius)
        {
            while (_pool.Count < drops.Count)
            {
                // 물방울: 툰 반투명 변형 (굴절 느낌은 림으로 대신, spec/10).
                var drop = Art.Primitives.Create(PrimitiveType.Sphere, $"Drop{_pool.Count}", transform, Art.ToonMaterials.Transparent);
                WorldView.Tint(drop.GetComponent<Renderer>(), DropColor);
                _pool.Add(drop.transform);
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < drops.Count;
                _pool[i].gameObject.SetActive(active);
                if (active)
                {
                    _pool[i].position = drops[i].ToUnity();
                    _pool[i].localScale = Vector3.one * (dropRadius * 2f);
                }
            }

            VisibleDrops = drops.Count;
        }

        private void LateUpdate()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            var simulation = _runner.Driver.Simulation;
            Render(simulation.CaptureSnapshot().Drops, simulation.Settings.Water.DropRadius);
            RenderMembrane(simulation, _runner.Driver.InterpolatedPlayerPosition, Time.time, Time.deltaTime);
        }

        /// <summary>갇혀 있으면 모키를 감싼 물방울 막을 그린다. 테스트에서 직접 부를 수 있다.</summary>
        public void RenderMembrane(GameSimulation simulation, Vector3 playerPosition, float time, float deltaTime)
        {
            var player = simulation.Player;
            bool trapped = player.State == PlayerState.Trapped;
            if (_membrane == null)
            {
                _membrane = Art.Primitives.Create(PrimitiveType.Sphere, "TrappedMembrane", transform, Art.ToonMaterials.Transparent).transform;
                _membrane.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            _membrane.gameObject.SetActive(trapped);
            if (!trapped)
            {
                _lastPresses = 0;
                _stretch = 0f;
                MembraneScale = 0f;
                return;
            }

            if (player.EscapePresses > _lastPresses)
            {
                _stretch += StretchFor(time - _lastPressTime);
                _lastPressTime = time;
            }

            _lastPresses = player.EscapePresses;
            _stretch *= Mathf.Exp(-StretchDecay * deltaTime);
            float progress = player.EscapePresses / (float)Mathf.Max(1, simulation.Water.RequiredEscapePresses);
            MembraneScale = Membrane(progress, _stretch);
            float diameter = player.CollisionRadius * 4f * MembraneScale;
            _membrane.position = playerPosition;
            _membrane.localScale = new Vector3(diameter, diameter * (1f - (0.15f * _stretch)), diameter);
            float alpha = DropColor.a * (1f - (MembraneThinAtFull * Mathf.Clamp01(progress)));
            WorldView.Tint(_membrane.GetComponent<Renderer>(), new Color(DropColor.r, DropColor.g, DropColor.b, alpha));
        }
    }
}
