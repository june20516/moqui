namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 호흡 (spec/11 §2). human.breathPeriod 주기의 앞부분 human.exhaleDuration 동안 날숨이 나온다.
    /// 날숨 위치는 코·입(머리 앞쪽 표면), 세기는 human.co2Strength × 취함 배율. 감각 표현(Unity)의 원천 데이터이다.
    /// </summary>
    public sealed class BreathSystem
    {
        private readonly BreathSettings _settings;

        public BreathSystem(BreathSettings settings)
        {
            _settings = settings;
        }

        public float Strength(Human human)
        {
            return _settings.Co2Strength * (human.Definition.Traits.Has(HumanModifier.Drunk) ? _settings.DrunkCo2Mul : 1f);
        }

        public void Step(Human human, int tick)
        {
            int periodTicks = System.Math.Max(1, SimulationTime.ToTicks(_settings.Period));
            int phaseTicks = tick % periodTicks;
            human.BreathPhase = (float)phaseTicks / periodTicks;
            human.IsExhaling = phaseTicks < SimulationTime.ToTicks(_settings.ExhaleDuration);
            human.ExhalePosition = human.HeadCenter + (human.HeadForward * human.HeadShape.Radius);
            human.ExhaleStrength = human.IsExhaling ? Strength(human) : 0f;
        }
    }
}
