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
            var result = new ScenarioRunner(TestSimulations.Settings).Run(level, scenario, seed, (simulation, step) =>
            {
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
    }
}
