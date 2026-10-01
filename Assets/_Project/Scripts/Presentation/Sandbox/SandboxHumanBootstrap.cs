using Moqui.Core.Simulation;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation.Sandbox
{
    /// <summary>Sandbox_Human 씬 시작 시 방·캡슐 인간·시뮬레이션을 구성한다. 인간 그림은 HumanView가 시뮬레이션을 읽어 만든다.</summary>
    public sealed class SandboxHumanBootstrap : MonoBehaviour
    {
        [SerializeField]
        private SimulationRunner _runner;

        private void Awake()
        {
            var tuning = SimulationRunner.LoadTuning();
            var setup = SandboxHumanWorld.CreateSetup();
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), setup);
            WorldView.Build(setup.World, transform);
            _runner.Begin(tuning, simulation);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
