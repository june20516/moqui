using System.Collections.Generic;
using System.Linq;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>
    /// spec/04 설계 검증: "적게 물고 길게 빠는" 플레이가 실제로 유리한가.
    /// 같은 무대(인간 + 양쪽 Shadow Zone)에서 긴 세션 2회와 짧은 세션 6회를 시드 20개로 비교한다.
    /// 클리어하지 못한 시도(사망·제한 시간 초과)의 클리어 시간은 제한 시간으로 센다 (D-035).
    /// </summary>
    public class DesignValidationTests
    {
        private const int SeedCount = 20;
        private const ulong FirstSeed = 1000UL;
        private const float TimeLimitSeconds = 600f;

        [Test]
        public void LongSessions_VersusShortSessions_LowerAwarenessAndFasterClear()
        {
            var longRuns = RunStrategy(sessions: 2);
            var shortRuns = RunStrategy(sessions: 6);

            var longSummary = Summarize("long ", longRuns);
            var shortSummary = Summarize("short", shortRuns);

            Assert.That(longSummary.Cleared, Is.GreaterThan(shortSummary.Cleared), "long sessions clear more often");
            Assert.That(longSummary.AverageAwareness, Is.LessThan(shortSummary.AverageAwareness), "fewer bites keep the human calmer");
            Assert.That(longSummary.AverageClearSeconds, Is.LessThan(shortSummary.AverageClearSeconds), "session acceleration makes long sessions faster");
            Assert.That(longRuns.Count(r => r.Outcome == StageOutcome.Died) + shortRuns.Count(r => r.Outcome == StageOutcome.Died), Is.EqualTo(0), "the bot hides instead of dying");
        }

        private static (int Cleared, double AverageAwareness, double AverageClearSeconds) Summarize(string name, List<StrategyResult> runs)
        {
            int cleared = runs.Count(r => r.Outcome == StageOutcome.Cleared);
            double awareness = runs.Average(r => r.AverageAwareness);
            double clearSeconds = runs.Average(r => r.Outcome == StageOutcome.Cleared ? r.Ticks * GameSimulation.DeltaTime : TimeLimitSeconds);
            TestContext.Out.WriteLine($"{name}: cleared {cleared}/{runs.Count}, avg awareness {awareness:F1}, avg clear {clearSeconds:F1}s (failures = {TimeLimitSeconds}s), avg marks {runs.Average(r => r.BiteMarks):F1}, avg flees {runs.Average(r => r.Flees):F1}");
            TestContext.Out.WriteLine($"{name} runs: " + string.Join(" ", runs.Select(r => $"{r.Outcome}@{r.Ticks * GameSimulation.DeltaTime:F0}s/m{r.BiteMarks}")));
            return (cleared, awareness, clearSeconds);
        }

        private static List<StrategyResult> RunStrategy(int sessions)
        {
            var results = new List<StrategyResult>();
            for (ulong seed = FirstSeed; seed < FirstSeed + SeedCount; seed++)
            {
                var human = TestHumans.Seated(actions: new[] { TestHumans.Scroll });
                var simulation = TestHumans.Simulation(SessionStrategyBot.HideSpot, SessionStrategyBot.CreateWorld(), human, seed);
                var bot = new SessionStrategyBot(simulation, sessions);
                results.Add(bot.Run(SecondsToTicks(TimeLimitSeconds)));
            }

            return results;
        }
    }
}
