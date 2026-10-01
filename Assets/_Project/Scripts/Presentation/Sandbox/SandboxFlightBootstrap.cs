using Moqui.Core.Simulation;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation.Sandbox
{
    /// <summary>Sandbox_Flight 씬 시작 시 화이트박스 방과 시뮬레이션을 구성한다.</summary>
    public sealed class SandboxFlightBootstrap : MonoBehaviour
    {
        [SerializeField]
        private SimulationRunner _runner;

        private void Awake()
        {
            var tuning = SimulationRunner.LoadTuning();
            var world = SandboxFlightWorld.Create();
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), world, SandboxFlightWorld.PlayerSpawn);
            WorldView.Build(world, transform);
            _runner.Begin(tuning, simulation);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
