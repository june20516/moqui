using System.Collections;
using Moqui.Core.Bots;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Simulation;
using Moqui.Unity.UI.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    /// <summary>
    /// 통합 스모크 (tech/verification §2, GOAL D6): 클리어 시나리오 봇을 Stage 씬 안에서 재생해
    /// 헤드리스 Core 결과와 같고, 표현·HUD·오디오가 함께 도는 동안 Error/Exception 로그가 0건인지 확인한다.
    /// </summary>
    public class ScenarioSmokeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Stage.unity";

        /// <summary>틱은 봇 명령이라 프레임 속도와 무관하다. 시간을 빠르게 돌려 테스트 시간을 줄인다.</summary>
        private const float TimeScale = 20f;
        private const float MaxRealSeconds = 240f;

        [SetUp]
        public void SetUp()
        {
            // 클리어하면 Result가 기록을 저장하므로 실제 save.json 대신 메모리 세션을 쓴다.
            TestSessions.UseMemorySession();
        }

        [TearDown]
        public void TearDown()
        {
            GameSession.Replace(null);
            Time.timeScale = 1f;
            StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
            StageBootstrap.RequestedSkills = SkillLoadout.None;
            StageBootstrap.RequestedSeed = null;
            StageBootstrap.RequestedDriverSetup = null;
        }

        [UnityTest]
        public IEnumerator ClearScenario_InStageScene_MatchesHeadlessCore_NoErrorLogs(
            [ValueSource(typeof(CatalogLevels), nameof(CatalogLevels.ClearScenarios))] string scenarioId)
        {
            var source = new UnityDataSource();
            string file = $"scenarios/{scenarioId}.json";
            var scenario = ScenarioDefinition.Parse(source.ReadText(file), file);
            LevelDefinition level = new LevelLoader(source).Load(scenario.LevelId);
            Tuning tuning = TuningLoader.Load(source);
            ScenarioResult expected = FirstClearingRun(tuning, level, scenario);
            Assert.That(expected, Is.Not.Null, "some fixed seed clears headless");

            // 첫 틱부터 봇이 조종하도록 구동기가 만들어지는 순간 연결한다.
            var pilot = new ScenarioRunner(tuning).CreatePilot(scenario);
            StageOutcome outcome = StageOutcome.InProgress;
            int outcomeTick = -1;
            StageBootstrap.RequestedLevelId = level.Id;
            StageBootstrap.RequestedSkills = scenario.Skills;
            StageBootstrap.RequestedSeed = expected.Seed;
            StageBootstrap.RequestedDriverSetup = driver =>
            {
                driver.CommandOverride = pilot.Next;
                driver.TickCompleted += simulation =>
                {
                    pilot.Observe(simulation);
                    if (outcomeTick < 0 && simulation.Outcome != StageOutcome.InProgress)
                    {
                        outcome = simulation.Outcome;
                        outcomeTick = simulation.Tick;
                    }
                };
            };

            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            var runner = Object.FindAnyObjectByType<SimulationRunner>();
            Time.timeScale = TimeScale;
            float started = Time.realtimeSinceStartup;
            while (outcomeTick < 0
                && runner.Driver.Simulation.Tick < scenario.Expect.MaxTicks
                && Time.realtimeSinceStartup - started < MaxRealSeconds)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            var human = runner.Driver.Simulation.Human;
            Assert.That(outcome, Is.EqualTo(expected.Outcome), $"{scenarioId} seed {expected.Seed}");
            Assert.That(outcomeTick, Is.EqualTo(expected.Ticks), "same tick as headless run");
            Assert.That(human.FrenzyCount, Is.EqualTo(expected.Frenzies));
            Assert.That(human.BiteMarkCount, Is.EqualTo(expected.BiteMarks));

            // Result 화면(사망 연출 지연 뒤)까지 띄워 흐름 전체에서 오류가 없는지 본다.
            yield return new WaitForSecondsRealtime(2f);
            LogAssert.NoUnexpectedReceived();
        }

        private static ScenarioResult FirstClearingRun(Tuning tuning, LevelDefinition level, ScenarioDefinition scenario)
        {
            foreach (ulong seed in scenario.Seeds)
            {
                var result = new ScenarioRunner(tuning).Run(level, scenario, seed);
                if (result.Outcome == scenario.Expect.Outcome)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
