using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation.Senses;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation.Stage
{
    /// <summary>
    /// Stage 씬은 하나이고 레벨 데이터만 바꿔 끼운다 (tech/architecture.md §6). 선택한 레벨 ID로 시뮬레이션과 화이트박스를 만든다.
    /// </summary>
    public sealed class StageBootstrap : MonoBehaviour
    {
        public const string DefaultLevelId = "stage01";

        [SerializeField]
        private SimulationRunner _runner;

        [SerializeField]
        private LevelMaterials _materials;

        [SerializeField]
        private SensesView _senses;

        /// <summary>다음에 열 레벨. 화면 흐름(M8)이 정한다.</summary>
        public static string RequestedLevelId { get; set; } = DefaultLevelId;

        public LevelDefinition Level { get; private set; }

        private void Awake()
        {
            var tuning = SimulationRunner.LoadTuning();
            Level = new LevelLoader(new UnityDataSource()).Load(RequestedLevelId);
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), Level.CreateSetup());
            var visuals = LevelView.Build(Level, simulation.World, transform, _materials);
            _senses.Bind(simulation, new SensesSettings(tuning), visuals, _materials);
            _runner.Begin(tuning, simulation);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
