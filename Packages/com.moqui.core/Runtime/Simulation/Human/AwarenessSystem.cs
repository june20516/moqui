using System;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 경계 게이지 0~100 (spec/02 §3). 자극이 없으면 decayDelay 뒤 감소하고, 광분 중에는 감소하지 않는다.
    /// 물린 자국 보정(증가 배율, 감소 배율, 하한)은 spec/04에서 채운다. 기본값은 보정 없음.
    /// </summary>
    public sealed class AwarenessSystem
    {
        private readonly AwarenessSettings _settings;

        public AwarenessSystem(AwarenessSettings settings)
        {
            _settings = settings;
        }

        public float GainMultiplier { get; set; } = 1f;

        public float DecayDivisor { get; set; } = 1f;

        public float Floor { get; set; }

        public void Apply(Human human, in HumanPerception perception, bool playerHidden, int tick, float deltaTime)
        {
            float gain = ((perception.VisionRate + perception.HearingRate) * deltaTime) + perception.InstantGain;
            human.Causes.Decay(deltaTime, _settings.CauseMemory);
            human.Causes.Add(AwarenessCause.Sight, perception.VisionRate * deltaTime * GainMultiplier);
            human.Causes.Add(AwarenessCause.Hearing, ((perception.HearingRate * deltaTime) + perception.InstantGain) * GainMultiplier);
            if (perception.RedZoneTriggered)
            {
                human.Causes.Add(AwarenessCause.Sight, _settings.FrenzyEnter - human.Awareness);
                human.Awareness = _settings.FrenzyEnter;
            }

            if (perception.HasStimulus)
            {
                human.HasStimulus = true;
                human.LastStimulusPosition = perception.StimulusPosition;
                human.LastStimulusTick = tick;
            }

            if (gain > 0f)
            {
                human.Awareness += gain * GainMultiplier;
            }
            else if (!perception.HasStimulus
                && human.State != AwarenessState.Frenzy
                && SimulationTime.HasElapsed(human.LastStimulusTick, tick, _settings.DecayDelay))
            {
                float rate = playerHidden ? _settings.ShadowDecayRate : _settings.DecayRate;
                human.Awareness -= rate * deltaTime / DecayDivisor;
            }

            human.Awareness = Math.Clamp(human.Awareness, Math.Min(Floor, _settings.FrenzyEnter), _settings.FrenzyEnter);
        }
    }
}
