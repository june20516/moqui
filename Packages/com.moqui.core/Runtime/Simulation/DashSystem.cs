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
                && _stamina.CanSpend(player, _settings.StaminaCost);
        }

        public bool TryStart(Player player, in PlayerCommand command, int tick, bool allowDiagonal, List<SimulationEvent> events)
        {
            if (!command.DashPressed || !CanStart(player, tick))
            {
                return false;
            }

            int ticks = Math.Max(1, SimulationTime.ToTicks(_settings.Duration));
            float distance = _settings.Distance * player.DashDistanceMultiplier;
            player.State = PlayerState.Dashing;
            player.DashDirection = DashDirectionResolver.Resolve(command, allowDiagonal);
            player.DashTicksRemaining = ticks;
            player.DashStepDistance = distance / ticks;
            player.LastDashStartTick = tick;
            _stamina.Spend(player, _settings.StaminaCost, tick);
            events.Add(new NoiseEmitted(tick, NoiseSource.Dash, player.Position, _settings.NoiseRadius, _settings.NoiseAwareness));
            return true;
        }

        /// <summary>대시 중 1틱 이동. 대시가 끝나면 Flying으로 돌아간다.</summary>
        public void Step(Player player)
        {
            var result = _mover.MoveStraight(player.Position, player.CollisionRadius, player.DashDirection * player.DashStepDistance, ShapeFlags.Obstacle);
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
