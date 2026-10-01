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
        }
    }
}
