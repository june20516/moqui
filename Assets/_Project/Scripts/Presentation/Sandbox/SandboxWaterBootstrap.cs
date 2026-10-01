using Moqui.Core.Simulation;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation.Sandbox
{
    /// <summary>Sandbox_Water 씬 시작 시 방·발생원·습기 영역과 시뮬레이션을 구성한다.</summary>
    public sealed class SandboxWaterBootstrap : MonoBehaviour
    {
        [SerializeField]
        private SimulationRunner _runner;

        private void Awake()
        {
            var tuning = SimulationRunner.LoadTuning();
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), SandboxWaterWorld.CreateSetup());
            WorldView.Build(simulation.World, transform);
            _runner.Begin(tuning, simulation);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
