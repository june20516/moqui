using System.Linq;
using Moqui.Core.Bots;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Bots
{
    /// <summary>
    /// 봇 경로 조정용 진단 (평소에는 실행하지 않음). 1초마다 위치·상태·단계·경계·중독을 출력한다.
    /// 환경 변수 MOQUI_SCENARIO, MOQUI_SEED, MOQUI_EVERY(틱)로 고른다. 예: dotnet test --filter "FullyQualifiedName~ScenarioDiagnostics"
    /// </summary>
    [Explicit("diagnostic")]
    public class ScenarioDiagnostics
    {
        [Test]
        public void Trace()
        {
            string scenarioId = System.Environment.GetEnvironmentVariable("MOQUI_SCENARIO") ?? "stage03_clear";
            ulong seed = ulong.Parse(System.Environment.GetEnvironmentVariable("MOQUI_SEED") ?? "2");
            int every = int.Parse(System.Environment.GetEnvironmentVariable("MOQUI_EVERY") ?? "60");
            var source = FileSystemDataSource.ForRepoData();
            string file = ScenarioDefinition.FilePath(scenarioId);
            var scenario = ScenarioDefinition.Parse(source.ReadText(file), file);
            var level = new LevelLoader(source).Load(scenario.LevelId);
            var result = new ScenarioRunner(TestSimulations.Tuning).Run(level, scenario, seed, (simulation, step) =>
            {
                foreach (var telegraph in simulation.Events.OfType<AttackTelegraphStarted>())
                {
                    var player = simulation.Player.Position;
                    TestContext.Out.WriteLine($"  t={simulation.Tick * GameSimulation.DeltaTime:F2} telegraph {telegraph.Kind} target=({telegraph.Target.X:F0},{telegraph.Target.Y:F0},{telegraph.Target.Z:F0}) player=({player.X:F0},{player.Y:F0},{player.Z:F0}) arm={simulation.Human.Attack.ArmA} lean={simulation.Human.Attack.PostureTarget.LeanAngle:F0} twist={simulation.Human.Attack.PostureTarget.Twist:F0} rise={simulation.Human.Attack.PostureTarget.Rise:F1}");
                }

                if (simulation.Tick % every != 0)
                {
                    return;
                }

                var p = simulation.Player;
                var h = simulation.Human;
                TestContext.Out.WriteLine($"t={simulation.Tick * GameSimulation.DeltaTime:F1} pos=({p.Position.X:F0},{p.Position.Y:F0},{p.Position.Z:F0}) {p.State} hidden={p.IsHidden} blood={p.BloodGauge:F0} tox={p.Toxin:F0} aw={h?.Awareness:F0} {h?.State} seen={h?.PlayerVisible} occ={h?.PlayerOccluded} atk={h?.Attack.Phase} | {step}");
            });
            TestContext.Out.WriteLine(result.ToString());
        }

        /// <summary>광분 빈도 측정 (M13): 스테이지별 클리어 봇 시드 1~5의 광분 횟수, 반응 공격 수, 최고 경계.</summary>
        [Test]
        public void FrenzyStats()
        {
            var source = FileSystemDataSource.ForRepoData();
            for (int stage = 1; stage <= 5; stage++)
            {
                string scenarioId = $"stage{stage:00}_clear";
                string file = ScenarioDefinition.FilePath(scenarioId);
                var scenario = ScenarioDefinition.Parse(source.ReadText(file), file);
                var level = new LevelLoader(source).Load(scenario.LevelId);
                for (ulong seed = 1; seed <= 5; seed++)
                {
                    int reactions = 0;
                    float maxAwareness = 0f;
                    var result = new ScenarioRunner(TestSimulations.Tuning).Run(level, scenario, seed, (simulation, step) =>
                    {
                        reactions += simulation.Events.OfType<AttackTelegraphStarted>().Count(telegraph => telegraph.Kind == AttackKind.ReactSlap);
                        maxAwareness = System.Math.Max(maxAwareness, simulation.Human.Awareness);
                    });
                    TestContext.Out.WriteLine($"{scenarioId} seed={seed} {result.Outcome} t={result.Ticks * GameSimulation.DeltaTime:F0}s frenzies={result.Frenzies} reactions={reactions} maxAwareness={maxAwareness:F0} marks={result.BiteMarks}");
                }
            }
        }
    }
}
