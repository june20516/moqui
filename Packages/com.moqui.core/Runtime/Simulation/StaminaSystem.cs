using System;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 스태미나 (spec/01). 대시할 때만 소모하고, 마지막 소모 후 regenDelay가 지나면 regenRate로 회복한다.
    /// 0이 되면 exhaustedDuration 동안 탈진: 이동 속도 배율, 대시 불가. 숨은 상태에서는 탈진 시간이 더 빨리 줄어든다.
    /// </summary>
    public sealed class StaminaSystem
    {
        private readonly StaminaSettings _settings;
        private readonly HidingSettings _hiding;

        public StaminaSystem(StaminaSettings settings, HidingSettings hiding)
        {
            _settings = settings;
            _hiding = hiding;
        }

        public float SpeedMultiplier(Player player)
        {
            return player.IsExhausted ? _settings.ExhaustedSpeedMul : 1f;
        }

        public bool CanSpend(Player player, float cost)
        {
            return !player.IsExhausted && player.Stamina >= cost;
        }

        public void Spend(Player player, float cost, int tick)
        {
            player.Stamina = Math.Max(0f, player.Stamina - cost);
            player.LastStaminaSpendTick = tick;
            if (player.Stamina <= 0f)
            {
                player.ExhaustedRemaining = _settings.ExhaustedDuration;
            }
        }

        /// <summary>틱 끝에서 탈진 시간 감소와 회복을 처리한다.</summary>
        public void Update(Player player, int tick, float deltaTime)
        {
            if (player.IsExhausted)
            {
                float recovery = player.IsHidden ? _hiding.DebuffRecoveryMul : 1f;
                player.ExhaustedRemaining = Math.Max(0f, player.ExhaustedRemaining - (deltaTime * recovery));
            }

            if (SimulationTime.HasElapsed(player.LastStaminaSpendTick, tick, _settings.RegenDelay))
            {
                player.Stamina = Math.Min(_settings.Max, player.Stamina + (_settings.RegenRate * player.StaminaRegenMultiplier * deltaTime));
            }
        }
    }
}
