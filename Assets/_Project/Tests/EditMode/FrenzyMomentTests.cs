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
