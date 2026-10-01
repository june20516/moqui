using System.IO;
using System.Linq;
using Moqui.Core.Bots;
using Moqui.Core.Data.Levels;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Bots
{
    /// <summary>시나리오 봇 (tech/verification.md §3): data/scenarios/*.json을 재생하고 expect와 비교한다.</summary>
    public class ScenarioTests
    {
        private static readonly string[] Scenarios = { "stage01_clear", "stage01_detect", "stage02_clear", "stage02_detect", "stage03_clear", "stage03_detect", "stage04_clear", "stage04_detect", "stage05_clear", "stage05_detect" };

        [TestCaseSource(nameof(Scenarios))]
        public void Scenario_MeetsExpectationOnEnoughSeeds(string scenarioId)
        {
            var source = FileSystemDataSource.ForRepoData();
            string file = ScenarioDefinition.FilePath(scenarioId);
            var scenario = ScenarioDefinition.Parse(source.ReadText(file), file);
            var level = new LevelLoader(source).Load(scenario.LevelId);
            var runner = new ScenarioRunner(TestSimulations.Tuning);

            var results = scenario.Seeds.Select(seed => runner.Run(level, scenario, seed)).ToList();
            foreach (var result in results)
            {
                TestContext.Out.WriteLine($"{scenarioId} {result} -> {(result.Meets(scenario.Expect) ? "ok" : "FAIL")}");
            }

            int successes = results.Count(r => r.Meets(scenario.Expect));
            Assert.That(successes, Is.GreaterThanOrEqualTo(scenario.MinSuccesses), $"{successes}/{results.Count} met the expectation");
        }

        [Test]
        public void ScenarioFiles_AllParse()
        {
            var source = FileSystemDataSource.ForRepoData();
            var files = Directory.GetFiles(Path.Combine(RepoPaths.Data, "scenarios"), "*.json");

            foreach (string path in files)
            {
                string id = Path.GetFileNameWithoutExtension(path);
                Assert.DoesNotThrow(() => ScenarioDefinition.Parse(source.ReadText(ScenarioDefinition.FilePath(id)), id), id);
            }

            Assert.That(files.Length, Is.GreaterThanOrEqualTo(4));
        }
    }
}
