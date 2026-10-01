using Moqui.Core.Data.Levels;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Core.Tutorial;
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

        [SerializeField]
        private Gimmicks.GimmickView _gimmicks;

        /// <summary>다음에 열 레벨. 화면 흐름(M8)이 정한다.</summary>
        public static string RequestedLevelId { get; set; } = DefaultLevelId;

        /// <summary>가지고 들어가는 스킬 (spec/09). 화면 흐름이 저장 데이터에서 정한다.</summary>
        public static SkillLoadout RequestedSkills { get; set; } = SkillLoadout.None;

        public LevelDefinition Level { get; private set; }

        /// <summary>레벨에 튜토리얼 안내가 있으면 진행 판정기. 없으면 null.</summary>
        public TutorialTracker Tutorial { get; private set; }

        private void Awake()
        {
            // 스킬 수치 효과는 Tuning에 반영해 시뮬레이션·시점·감각 표현이 같은 값을 쓰게 한다 (D-043).
            var tuning = SkillEffects.Apply(SimulationRunner.LoadTuning(), RequestedSkills);
            Level = new LevelLoader(new UnityDataSource()).Load(RequestedLevelId);
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), Level.CreateSetup(RequestedSkills));
            var visuals = LevelView.Build(Level, simulation.World, transform, _materials);
            var senses = new SensesSettings(tuning);
            _senses.Bind(simulation, senses, visuals, _materials);
            _gimmicks.Bind(simulation, senses, _materials);
            _runner.Begin(tuning, simulation);
            if (Level.Tutorial.Count > 0)
            {
                Tutorial = new TutorialTracker(Level.Tutorial, new TutorialSettings(tuning));
                _runner.Driver.TickCompleted += Tutorial.Observe;
            }

            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
