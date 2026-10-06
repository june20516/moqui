using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Random;
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
            return CanLaunchFrom(player.State)
                && SimulationTime.HasElapsed(player.LastDashStartTick, tick, _settings.Cooldown)
                && _stamina.CanSpend(player, Cost(player));
        }

        /// <summary>비행 중이거나 표면에 붙어 있을 때 대시할 수 있다 (부착 중 대시 = 법선 방향, D-051).</summary>
        private static bool CanLaunchFrom(PlayerState state) => state == PlayerState.Flying || state == PlayerState.Attached;

        /// <summary>연속 와류 스킬 레벨 (spec/09). 0이면 없음, 1 = 추가 대시, 2 = 추가 대시 스태미나 없음.</summary>
        public int ChainLevel { get; set; }

        /// <summary>
        /// 연속 와류 추가 대시 가능 여부: 직전 일반 대시가 끝난 뒤 dash.chainWindow 안이고, 그 체인에서 아직 쓰지 않았다.
        /// 쿨타임은 무시한다. 1레벨은 스태미나가 필요하고 2레벨은 쓰지 않는다.
        /// </summary>
        public bool CanChain(Player player, int tick)
        {
            if (ChainLevel <= 0 || !CanLaunchFrom(player.State) || !player.ChainDashAvailable)
            {
                return false;
            }

            int dashTicks = Math.Max(1, SimulationTime.ToTicks(_settings.Duration));
            int windowEnd = player.LastDashStartTick + dashTicks + SimulationTime.ToTicks(_settings.ChainWindow);
            return tick <= windowEnd && (ChainLevel >= 2 || _stamina.CanSpend(player, Cost(player)));
        }

        /// <summary>일반·연속 대시 중 하나라도 지금 시작할 수 있는가.</summary>
        public bool CanDash(Player player, int tick) => CanStart(player, tick) || CanChain(player, tick);

        /// <summary>Dash 입력이면 진행 방향(입력 없으면 보는 방향)으로 대시를 시작한다 (spec/01, D-051, D-066).</summary>
        public bool TryStart(Player player, in PlayerCommand command, int tick, List<SimulationEvent> events)
        {
            if (!command.DashPressed || !CanDash(player, tick))
            {
                return false;
            }

            Start(player, DashDirectionResolver.Resolve(command), tick, events);
            return true;
        }

        /// <summary>정해진 방향으로 대시를 시작한다 (부착 중 대시 = 표면 법선 방향). 시작 가능 여부는 호출자가 CanDash로 확인한다.</summary>
        public void Start(Player player, Vector3 direction, int tick, List<SimulationEvent> events)
        {
            bool normal = CanStart(player, tick);
            Begin(player, direction);
            player.LastDashStartTick = tick;
            player.ChainDashAvailable = normal;
            if (normal || ChainLevel < 2)
            {
                _stamina.Spend(player, Cost(player), tick);
            }

            events.Add(new NoiseEmitted(tick, NoiseSource.Dash, player.Position, _settings.NoiseRadius * player.NoiseRadiusMultiplier, _settings.NoiseAwareness));
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
