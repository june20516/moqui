using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>물방울과 젖은 날개 (spec/05 §1).</summary>
    public sealed class WaterSettings
    {
        public WaterSettings(Tuning tuning)
        {
            DropInterval = tuning.GetFloat("water.dropInterval");
            DropRadius = tuning.GetFloat("water.dropRadius");
            TrappedFallSpeed = tuning.GetFloat("water.trappedFallSpeed");
            EscapePresses = tuning.GetInt("water.escapePresses");
            MinSourceHeight = tuning.GetFloat("water.minSourceHeight");
            WetDuration = tuning.GetFloat("wetWings.duration");
            WetSpeedMul = tuning.GetFloat("wetWings.speedMul");
            WetRegenMul = tuning.GetFloat("wetWings.regenMul");
            WetDashCostAdd = tuning.GetFloat("wetWings.dashCostAdd");
        }

        public float DropInterval { get; }

        public float DropRadius { get; }

        public float TrappedFallSpeed { get; }

        public int EscapePresses { get; }

        public float MinSourceHeight { get; }

        public float WetDuration { get; }

        public float WetSpeedMul { get; }

        public float WetRegenMul { get; }

        public float WetDashCostAdd { get; }
    }

    /// <summary>습기와 증기 (spec/05 §2).</summary>
    public sealed class HumidSettings
    {
        public HumidSettings(Tuning tuning)
        {
            GainStrong = tuning.GetFloat("humid.gainStrong");
            GainWeak = tuning.GetFloat("humid.gainWeak");
            Decay = tuning.GetFloat("humid.decay");
            SteamVisionMul = tuning.GetFloat("humid.steamVisionMul");
            SteamClearRangeMul = tuning.GetFloat("humid.steamClearRangeMul");
        }

        public float GainStrong { get; }

        public float GainWeak { get; }

        public float Decay { get; }

        public float SteamVisionMul { get; }

        public float SteamClearRangeMul { get; }
    }
}
