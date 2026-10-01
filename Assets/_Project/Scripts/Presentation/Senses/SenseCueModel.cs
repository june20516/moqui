using Moqui.Core.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation.Senses
{
    /// <summary>체온 표시와 은신처 표시의 세기 규칙 (spec/11 §3, §4).</summary>
    public static class SenseCueModel
    {
        /// <summary>체온 빛 세기: heatRange 밖 0, 가까울수록 1에 가깝다.</summary>
        public static float HeatIntensity(float distance, float heatRange)
        {
            return distance > heatRange ? 0f : 1f - Mathf.Clamp01(distance / heatRange);
        }

        /// <summary>은신처 표시 세기: 인간의 어그로 상태에 따라 3단계. 인간이 없으면 Safe 단계.</summary>
        public static float ShadowCueIntensity(SensesSettings settings, AwarenessState? state)
        {
            switch (state)
            {
                case AwarenessState.Frenzy:
                    return settings.CueIntensityFrenzy;
                case AwarenessState.Suspicious:
                    return settings.CueIntensitySuspicious;
                default:
                    return settings.CueIntensitySafe;
            }
        }
    }
}
