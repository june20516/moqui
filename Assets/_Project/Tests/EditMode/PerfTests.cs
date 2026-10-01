using Moqui.Unity.Diagnostics;
using Moqui.Unity.UI.Flow;
using NUnit.Framework;

namespace Moqui.Unity.Tests
{
    /// <summary>성능 측정 도구 (M11): 프레임 통계와 실행 인자.</summary>
    public class PerfTests
    {
        [Test]
        public void FrameStats_SummarizesSamples()
        {
            var stats = new FrameStats();
            for (int ms = 1; ms <= 100; ms++)
            {
                stats.Add(ms);
            }

            Assert.That(stats.Count, Is.EqualTo(100));
            Assert.That(stats.Average, Is.EqualTo(50.5f).Within(1e-4f));
            Assert.That(stats.Percentile(95f), Is.EqualTo(95f));
            Assert.That(stats.Percentile(99f), Is.EqualTo(99f));
            Assert.That(stats.Max, Is.EqualTo(100f));
            Assert.That(stats.OverBudgetRatio(FrameStats.BudgetMs(60f)), Is.EqualTo(0.84f).Within(1e-4f), "frames over 16.67 ms");
        }

        [Test]
        public void FrameStats_NoSamples_Throws()
        {
            Assert.That(() => new FrameStats().Average, Throws.InvalidOperationException);
        }

        [Test]
        public void PerfRunner_ReportPathOnlyWithArgument()
        {
            Assert.That(PerfRunner.ReportPathFrom(new[] { "Moqui.exe", "-moquiPerf", "Logs/perf.md" }), Is.EqualTo("Logs/perf.md"));
            Assert.That(PerfRunner.ReportPathFrom(new[] { "Moqui.exe" }), Is.Null);
            Assert.That(PerfRunner.ReportPathFrom(new[] { "Moqui.exe", "-moquiPerf" }), Is.Null, "missing path");
        }
    }
}
