using Moqui.Core.Data;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moqui.Unity.Simulation
{
    /// <summary>씬에서 시뮬레이션을 프레임마다 구동한다. 시뮬레이션 구성(월드, 시작 위치)은 Begin으로 받는다.</summary>
    public sealed class SimulationRunner : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset _controls;

        private CommandCollector _collector;
        private bool _paused;
        private float _sensitivityScale = 1f;
        private bool _invertY;

        public SimulationDriver Driver { get; private set; }

        public Tuning Tuning { get; private set; }

        public bool IsRunning => Driver != null;

        /// <summary>
        /// 일시정지 (spec/08 Pause): 틱을 진행하지 않고 입력도 샘플링하지 않는다. 재개할 때 정지 중 눌림을 버린다.
        /// </summary>
        public bool Paused
        {
            get => _paused;
            set
            {
                if (_paused && !value)
                {
                    _collector?.ClearLatches();
                }

                _paused = value;
            }
        }

        /// <summary>사용자 설정(감도·세로 반전)을 입력 수집기에 반영한다.</summary>
        public void ApplyLookPreferences(float sensitivityScale, bool invertY)
        {
            _sensitivityScale = sensitivityScale;
            _invertY = invertY;
            if (_collector != null)
            {
                _collector.SensitivityScale = sensitivityScale;
                _collector.InvertY = invertY;
            }
        }

        public void Begin(Tuning tuning, GameSimulation simulation)
        {
            Tuning = tuning;
            _collector?.Dispose();
            _collector = new CommandCollector(_controls, new LookSettings(tuning))
            {
                SensitivityScale = _sensitivityScale,
                InvertY = _invertY,
            };
            Driver = new SimulationDriver(simulation, _collector);
        }

        /// <summary>
        /// tuning.json을 읽고, 사람이 플레이 검증 중이면 playtest.json 덮어쓰기를 얹는다 (D-065).
        /// 배치 모드(테스트·캡처·성능)에서는 덮어쓰지 않는다.
        /// </summary>
        public static Tuning LoadTuning()
        {
            var source = new UnityDataSource();
            return PlaytestTuning.Apply(TuningLoader.Load(source), source, new PlaytestStore());
        }

        /// <summary>덮어쓰기 없이 정본 tuning.json만 (조정 패널의 기본값 표시용).</summary>
        public static Tuning LoadBaseTuning()
        {
            return TuningLoader.Load(new UnityDataSource());
        }

        private void Update()
        {
            if (!_paused)
            {
                Driver?.Frame(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            _collector?.Dispose();
        }
    }
}
