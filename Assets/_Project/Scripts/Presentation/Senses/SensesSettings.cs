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
            CueRange = tuning.GetFloat("hiding.cueRange");
            SteamClearRangeMul = tuning.GetFloat("humid.steamClearRangeMul");
        }

        public float ClearRange { get; }

        public float FogFullRange { get; }

        public float FogMaxDensity { get; }

        public float FogBlurPixels { get; }

        public float Co2VisibleRange { get; }

        public float HeatRange { get; }

        public float CueRange { get; }

        public float SteamClearRangeMul { get; }
    }
}
