using System.Linq;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.UI.Hud;
using NUnit.Framework;

namespace Moqui.Unity.Tests
{
    /// <summary>왜 들켰나 (gulf §10, D-067): 광분 순간 느린 화면·원인 문구, 결과 화면 경계 타임라인.</summary>
    public class FrenzyMomentTests
    {
        [Test]
        public void Trigger_SlowsBrieflyThenRestores_ShowsCauseText()
        {
            var moment = new FrenzyMoment();
            moment.Trigger(AwarenessCause.Hearing, 10f);

            Assert.That(moment.Update(10.1f, 1f), Is.EqualTo(FrenzyMoment.SlowTimeScale));
            Assert.That(moment.TextVisible, Is.True);
            Assert.That(moment.Text, Does.Contain("날갯소리"));

            Assert.That(moment.Update(10f + FrenzyMoment.SlowSeconds + 0.01f, FrenzyMoment.SlowTimeScale), Is.EqualTo(1f), "restored after the slow window");
            Assert.That(moment.Update(10f + FrenzyMoment.TextSeconds + 0.01f, 1f), Is.EqualTo(1f));
            Assert.That(moment.TextVisible, Is.False);
        }

        [Test]
        public void Paused_TimeScaleIsLeftAlone()
        {
            var moment = new FrenzyMoment();
            moment.Trigger(AwarenessCause.Sight, 0f);

            Assert.That(moment.Update(0.1f, 0f), Is.EqualTo(0f), "pause wins");
            Assert.That(moment.Update(5f, 0f), Is.EqualTo(0f));
        }

        /// <summary>판이 끝나면 느린 화면을 취소한다: 결과 화면이 느려지지 않는다 (리뷰 M14).</summary>
        [Test]
        public void Cancel_StopsSlowMotionAndText()
        {
            var moment = new FrenzyMoment();
            moment.Trigger(AwarenessCause.Sight, 0f);
            moment.Cancel();

            Assert.That(moment.Update(0.1f, 1f), Is.EqualTo(1f));
            Assert.That(moment.TextVisible, Is.False);
        }

        [Test]
        public void Timeline_Downsample_KeepsThePeakOfEachBucket()
        {
            var samples = new System.Collections.Generic.List<(float, AwarenessCause)>();
            for (int i = 0; i < 1000; i++)
            {
                samples.Add((i == 537 ? 99f : 10f, i == 537 ? AwarenessCause.Hearing : AwarenessCause.Sight));
            }

            var bars = AwarenessTimeline.Downsample(samples, 200);

            Assert.That(bars.Count, Is.EqualTo(200));
            Assert.That(bars.Max(bar => bar.Awareness), Is.EqualTo(99f));
            Assert.That(bars.Count(bar => bar.Cause == AwarenessCause.Hearing), Is.EqualTo(1));
        }

        /// <summary>원인마다 다른 아이콘 (색이 아니라 모양으로 읽힌다, 플레이 피드백 2026-10-07).</summary>
        [Test]
        public void EveryCause_HasItsOwnIcon()
        {
            var causes = new[] { AwarenessCause.Sight, AwarenessCause.Hearing, AwarenessCause.Itch, AwarenessCause.Glance, AwarenessCause.Alarm };
            var icons = causes.Select(HudSprites.CauseIcon).ToList();

            Assert.That(icons, Is.Unique);
            Assert.That(icons.All(icon => icon != null), Is.True);
        }

        [Test]
        public void EveryCause_HasTextAndColor()
        {
            foreach (AwarenessCause cause in System.Enum.GetValues(typeof(AwarenessCause)))
            {
                Assert.That(FrenzyMoment.Label(cause), Does.StartWith("들켰다!"));
                Assert.That(FrenzyMoment.ColorOf(cause).a, Is.EqualTo(1f));
            }
        }

        [Test]
        public void Timeline_SamplesEveryHalfSecond()
        {
            var tuning = TuningLoader.Load(new UnityDataSource());
            var level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var timeline = new AwarenessTimeline();

            for (int i = 0; i < SimulationTime.ToTicks(2f); i++)
            {
                simulation.Step(PlayerCommand.None);
                timeline.Observe(simulation);
            }

            Assert.That(timeline.Samples.Count, Is.EqualTo(4));
        }
    }
}
