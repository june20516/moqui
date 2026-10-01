using System;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 습기 게이지와 젖은 날개 (spec/05 §1~2).
    /// 강한/약한 습기 영역 안에서 게이지가 오르고(부착 중에도), 밖에서는 내린다. 100이면 젖은 날개가 걸리고,
    /// 100인 채로 영역 안에 있으면 지속시간이 매 틱 갱신된다. 젖은 날개 남은 시간과 습기 감소는 숨은 상태에서 빨라진다.
    /// </summary>
    public sealed class HumiditySystem
    {
        public const float GaugeMax = 100f;

        private readonly HumidSettings _humid;
        private readonly WaterSettings _water;
        private readonly HidingSettings _hiding;
        private readonly CollisionWorld _world;
        private readonly WaterSystem _waterSystem;

        public HumiditySystem(HumidSettings humid, WaterSettings water, HidingSettings hiding, CollisionWorld world, WaterSystem waterSystem)
        {
            _humid = humid;
            _water = water;
            _hiding = hiding;
            _world = world;
            _waterSystem = waterSystem;
        }

        /// <summary>습기 증가 배율 (스킬 발수 코팅, spec/09). 기본 1.</summary>
        public float GainMultiplier { get; set; } = 1f;

        /// <summary>틱 시작: 젖은 날개 효과를 이동·스태미나·대시에 반영한다.</summary>
        public void ApplyWetEffects(Player player)
        {
            if (!player.IsWet)
            {
                return;
            }

            player.SpeedMultiplier *= _water.WetSpeedMul;
            player.StaminaRegenMultiplier *= _water.WetRegenMul;
            player.DashCostAdd += _water.WetDashCostAdd;
        }

        /// <summary>틱 끝: 습기 영역 판정, 게이지 증감, 젖은 날개 시간 감소·갱신.</summary>
        public void Step(Player player, float deltaTime)
        {
            bool strong = _world.AnyOverlap(player.Position, 0f, ShapeFlags.HumidStrong);
            bool weak = !strong && _world.AnyOverlap(player.Position, 0f, ShapeFlags.HumidWeak);
            player.InSteam = strong && player.State != PlayerState.Dead;
            float recovery = player.IsHidden ? _hiding.DebuffRecoveryMul : 1f;

            if (player.IsWet)
            {
                player.WetRemaining = Math.Max(0f, player.WetRemaining - (deltaTime * recovery));
            }

            if (strong || weak)
            {
                float gain = (strong ? _humid.GainStrong : _humid.GainWeak) * GainMultiplier;
                player.Humidity = Math.Min(GaugeMax, player.Humidity + (gain * deltaTime));
                if (player.Humidity >= GaugeMax)
                {
                    player.WetRemaining = _waterSystem.WetDuration;
                }
            }
            else
            {
                player.Humidity = Math.Max(0f, player.Humidity - (_humid.Decay * recovery * deltaTime));
            }
        }
    }
}
