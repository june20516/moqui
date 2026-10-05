using Moqui.Core.Data;

namespace Moqui.Unity.Presentation.Senses
{
    /// <summary>모기 감각 표현 수치 (spec/11, spec/tuning.md senses·hiding). 표현 전용이며 판정에 쓰지 않는다.</summary>
    public sealed class SensesSettings
    {
        public SensesSettings(Tuning tuning)
        {
            ClearRange = tuning.GetFloat("senses.clearRange");
            FogFullRange = tuning.GetFloat("senses.fogFullRange");
            FogMaxDensity = tuning.GetFloat("senses.fogMaxDensity");
            FogBlurPixels = tuning.GetFloat("senses.fogBlurPixels");
            Co2VisibleRange = tuning.GetFloat("senses.co2VisibleRange");
            HeatRange = tuning.GetFloat("senses.heatRange");
            Co2PuffInterval = tuning.GetFloat("senses.co2PuffInterval");
            Co2PuffLifetime = tuning.GetFloat("senses.co2PuffLifetime");
            Co2RiseSpeed = tuning.GetFloat("senses.co2RiseSpeed");
            Co2ForwardSpeed = tuning.GetFloat("senses.co2ForwardSpeed");
            Co2PuffStartRadius = tuning.GetFloat("senses.co2PuffStartRadius");
            Co2PuffEndRadius = tuning.GetFloat("senses.co2PuffEndRadius");
            HeatGlowScale = tuning.GetFloat("senses.heatGlowScale");
            BiteMarkDotRadius = tuning.GetFloat("senses.biteMarkDotRadius");
            CueRange = tuning.GetFloat("hiding.cueRange");
            CueIntensitySafe = tuning.GetFloat("hiding.cueIntensitySafe");
            CueIntensitySuspicious = tuning.GetFloat("hiding.cueIntensitySuspicious");
            CueIntensityFrenzy = tuning.GetFloat("hiding.cueIntensityFrenzy");
            SteamClearRangeMul = tuning.GetFloat("humid.steamClearRangeMul");
            PerchDelay = tuning.GetFloat("perch.delay");
            PerchBlendTime = tuning.GetFloat("perch.blendTime");
            PerchClearRangeMul = tuning.GetFloat("perch.clearRangeMul");
            PerchFogFullRangeMul = tuning.GetFloat("perch.fogFullRangeMul");
        }

        public float ClearRange { get; }

        public float FogFullRange { get; }

        public float FogMaxDensity { get; }

        public float FogBlurPixels { get; }

        public float Co2VisibleRange { get; }

        public float HeatRange { get; }

        public float Co2PuffInterval { get; }

        public float Co2PuffLifetime { get; }

        public float Co2RiseSpeed { get; }

        public float Co2ForwardSpeed { get; }

        public float Co2PuffStartRadius { get; }

        public float Co2PuffEndRadius { get; }

        public float HeatGlowScale { get; }

        public float BiteMarkDotRadius { get; }

        public float CueRange { get; }

        public float CueIntensitySafe { get; }

        public float CueIntensitySuspicious { get; }

        public float CueIntensityFrenzy { get; }

        public float SteamClearRangeMul { get; }

        /// <summary>관망 (M13): 붙은 뒤 시작까지 (s), 넓어지는 시간 (s), 선명·최대 흐림 거리 배율.</summary>
        public float PerchDelay { get; }

        public float PerchBlendTime { get; }

        public float PerchClearRangeMul { get; }

        public float PerchFogFullRangeMul { get; }
    }
}
