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

        public SimulationDriver Driver { get; private set; }

        public Tuning Tuning { get; private set; }

        public bool IsRunning => Driver != null;

        public void Begin(Tuning tuning, GameSimulation simulation)
        {
            Tuning = tuning;
            _collector?.Dispose();
            _collector = new CommandCollector(_controls, new LookSettings(tuning));
            Driver = new SimulationDriver(simulation, _collector);
        }

        public static Tuning LoadTuning()
        {
            return TuningLoader.Load(new UnityDataSource());
        }

        private void Update()
        {
            Driver?.Frame(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _collector?.Dispose();
        }
    }
}
