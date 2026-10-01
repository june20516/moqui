using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>졸음 수정자 (spec/06).</summary>
    public sealed class DozeSettings
    {
        public DozeSettings(Tuning tuning)
        {
            SleepDuration = tuning.GetRange("doze.sleepDuration");
            WakeDuration = tuning.GetRange("doze.wakeDuration");
            WakeTelegraph = tuning.GetFloat("doze.wakeTelegraph");
            HearingMul = tuning.GetFloat("doze.hearingMul");
            ReactionMul = tuning.GetFloat("doze.reactionMul");
            ResleepDelay = tuning.GetFloat("doze.resleepDelay");
            FrenzyDurationMul = tuning.GetFloat("doze.frenzyDurationMul");
        }

        public FloatRange SleepDuration { get; }

        public FloatRange WakeDuration { get; }

        public float WakeTelegraph { get; }

        public float HearingMul { get; }

        public float ReactionMul { get; }

        public float ResleepDelay { get; }

        public float FrenzyDurationMul { get; }
    }

    /// <summary>호흡과 CO₂ (spec/11 §2). 취한 타겟의 CO₂ 배율 포함.</summary>
    public sealed class BreathSettings
    {
        public BreathSettings(Tuning tuning)
        {
            Period = tuning.GetFloat("human.breathPeriod");
            ExhaleDuration = tuning.GetFloat("human.exhaleDuration");
            Co2Strength = tuning.GetFloat("human.co2Strength");
            DrunkCo2Mul = tuning.GetFloat("drunk.co2Mul");
        }

        public float Period { get; }

        public float ExhaleDuration { get; }

        public float Co2Strength { get; }

        public float DrunkCo2Mul { get; }
    }
}
