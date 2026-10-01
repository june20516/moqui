using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 볼텍스 대시 (spec/01). dash.duration(틱으로 반올림) 동안 dash.distance × 대시 배율을 등속으로 이동한다.
    /// 장애물에 닿으면 그 지점에서 멈춘다. 끝나면 대시 방향으로 flight.speed의 속도를 남긴다.
    /// </summary>
    public sealed class DashSystem
    {
        private readonly DashSettings _settings;
        private readonly FlightSettings _flight;
        private readonly StaminaSystem _stamina;
        private readonly SphereMover _mover;

        public DashSystem(DashSettings settings, FlightSettings flight, StaminaSystem stamina, SphereMover mover)
        {
            _settings = settings;
            _flight = flight;
            _stamina = stamina;
            _mover = mover;
        }

        public bool CanStart(Player player, int tick)
        {
            return player.State == PlayerState.Flying
                && SimulationTime.HasElapsed(player.LastDashStartTick, tick, _settings.Cooldown)
                && _stamina.CanSpend(player, Cost(player));
        }

        public bool TryStart(Player player, in PlayerCommand command, int tick, bool allowDiagonal, List<SimulationEvent> events)
        {
            if (!command.DashPressed || !CanStart(player, tick))
            {
                return false;
            }

            Begin(player, DashDirectionResolver.Resolve(command, allowDiagonal));
            player.LastDashStartTick = tick;
            _stamina.Spend(player, Cost(player), tick);
            events.Add(new NoiseEmitted(tick, NoiseSource.Dash, player.Position, _settings.NoiseRadius, _settings.NoiseAwareness));
            return true;
        }

        /// <summary>대시 스태미나 비용 = dash.staminaCost + 젖은 날개 추가 비용 (spec/05).</summary>
        public float Cost(Player player)
        {
            return _settings.StaminaCost + player.DashCostAdd;
        }

        /// <summary>스태미나·쿨타임·소음 없이 대시 1회분을 시작한다 (물방울 탈출, spec/05, D-036).</summary>
        public void StartFree(Player player, Vector3 direction)
        {
            Begin(player, direction);
        }

        private void Begin(Player player, Vector3 direction)
        {
            int ticks = Math.Max(1, SimulationTime.ToTicks(_settings.Duration));
            float distance = _settings.Distance * player.DashDistanceMultiplier;
            player.State = PlayerState.Dashing;
            player.DashDirection = direction;
            player.DashTicksRemaining = ticks;
            player.DashStepDistance = distance / ticks;
        }

        /// <summary>대시 중 1틱 이동. 대시가 끝나면 Flying으로 돌아간다.</summary>
        public void Step(Player player)
        {
            var result = _mover.MoveStraight(player.Position, player.CollisionRadius, player.DashDirection * player.DashStepDistance, ShapeFlags.Solid);
            player.Position = result.Position;
            player.DashTicksRemaining--;
            if (result.Collided || player.DashTicksRemaining <= 0)
            {
                Finish(player, result);
            }
        }

        private void Finish(Player player, MoveResult result)
        {
            Vector3 residual = player.DashDirection * _flight.Speed;
            if (result.Collided)
            {
                float into = Vector3.Dot(residual, result.LastNormal);
                residual = into < 0f ? residual - (result.LastNormal * into) : residual;
            }

            player.Velocity = residual;
            player.DashTicksRemaining = 0;
            player.State = PlayerState.Flying;
        }
    }
}
