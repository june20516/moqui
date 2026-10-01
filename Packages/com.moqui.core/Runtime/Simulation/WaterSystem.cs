using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 물방울 라이딩 (spec/05 §1, D-036).
    /// 발생원마다 water.dropInterval로 물방울을 떨어뜨리고, 낙하 중 물방울이 플레이어에 닿으면 Trapped가 된다.
    /// Trapped 중 물방울은 water.trappedFallSpeed로 등속 낙하하고 플레이어는 그 중심에 고정된다.
    /// Dash를 escapePresses회 누르면 탈출(스태미나·쿨타임·소음 없이 위로 대시 1회분)하고 젖은 날개가 걸린다.
    /// 탈출 전에 물방울이 표면에 닿으면 WaterImpact로 사망한다.
    /// </summary>
    public sealed class WaterSystem
    {
        private const float MaxProbeDistance = 10000f;

        private readonly WaterSettings _settings;
        private readonly CollisionWorld _world;
        private readonly FallingBodySystem _fall;
        private readonly DashSystem _dash;
        private readonly IReadOnlyList<Vector3> _sources;
        private readonly List<FallingBody> _drops = new List<FallingBody>();
        private readonly int _intervalTicks;
        private int _nextDropId;

        public WaterSystem(WaterSettings settings, CollisionWorld world, FallingBodySystem fall, DashSystem dash, IReadOnlyList<Vector3> sources)
        {
            _settings = settings;
            _world = world;
            _fall = fall;
            _dash = dash;
            _sources = sources;
            _intervalTicks = Math.Max(1, SimulationTime.ToTicks(settings.DropInterval));
        }

        public IReadOnlyList<FallingBody> Drops => _drops;

        /// <summary>지금까지 생성한 물방울 수.</summary>
        public int SpawnedCount => _nextDropId;

        /// <summary>갇힌 물방울 아래 표면까지 남은 낙하 거리 (HUD 높이 게이지, spec/05). Trapped가 아니면 0.</summary>
        public float TrappedHeightRemaining(Player player)
        {
            var drop = player.TrappedDrop;
            if (drop == null)
            {
                return 0f;
            }

            return _world.SphereSweep(drop.Position, drop.Radius, -Vector3.UnitY, MaxProbeDistance, ShapeFlags.Solid, out var hit) ? hit.Distance : MaxProbeDistance;
        }

        /// <summary>탈출에 필요한 Dash 입력 횟수를 줄이는 값 (스킬 발수 코팅 3레벨, spec/09). 기본 0.</summary>
        public int EscapePressReduction { get; set; }

        /// <summary>젖은 날개 지속시간 배율 (스킬 발수 코팅, spec/09). 기본 1.</summary>
        public float WetDurationMultiplier { get; set; } = 1f;

        public int RequiredEscapePresses => Math.Max(1, _settings.EscapePresses - EscapePressReduction);

        public float WetDuration => _settings.WetDuration * WetDurationMultiplier;

        /// <summary>Trapped 중 입력: 이동은 무시하고 Dash 입력만 센다. 다 채우면 탈출한다.</summary>
        public void StepTrapped(Player player, in PlayerCommand command, int tick, List<SimulationEvent> events)
        {
            if (!command.DashPressed)
            {
                return;
            }

            player.EscapePresses++;
            if (player.EscapePresses < RequiredEscapePresses)
            {
                return;
            }

            _drops.Remove(player.TrappedDrop);
            player.TrappedDrop = null;
            player.EscapePresses = 0;
            player.State = PlayerState.Flying;
            player.WetRemaining = WetDuration;
            _dash.StartFree(player, Vector3.UnitY);
            events.Add(new PlayerEscapedDrop(tick));
        }

        /// <summary>물방울 생성·낙하·착지, 플레이어 포획과 고정, 바닥 충돌 사망.</summary>
        public void Step(Player player, int tick, float deltaTime, List<SimulationEvent> events)
        {
            if (tick % _intervalTicks == 0)
            {
                foreach (var source in _sources)
                {
                    _drops.Add(new FallingBody($"drop{_nextDropId++}", source, _settings.DropRadius));
                }
            }

            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var drop = _drops[i];
                Vector3 previous = drop.Position;
                bool trapping = player.TrappedDrop == drop;
                if (trapping)
                {
                    drop.Velocity = -Vector3.UnitY * _settings.TrappedFallSpeed;
                    drop.Position += drop.Velocity * deltaTime;
                }
                else
                {
                    _fall.Step(drop, deltaTime);
                }

                Vector3 travel = drop.Position - previous;
                if (_world.SphereSweep(previous, drop.Radius, travel, travel.Length(), ShapeFlags.Solid, out var hit))
                {
                    _drops.RemoveAt(i);
                    if (trapping)
                    {
                        player.Position = previous + (Vector3.Normalize(travel) * hit.Distance);
                        Kill(player, tick, events);
                    }

                    continue;
                }

                if (trapping)
                {
                    player.Position = drop.Position;
                }
                else if (CanBeTrapped(player) && SweptContact(previous, drop.Position, player.Position, drop.Radius + player.CollisionRadius))
                {
                    Trap(player, drop, tick, events);
                }
            }
        }

        /// <summary>
        /// 이번 틱 물방울이 지나간 선분과 플레이어 중심의 최단 거리로 닿음을 판정한다.
        /// 물방울은 틱당 수 u씩 떨어지므로 끝 위치끼리만 비교하면 플레이어를 건너뛸 수 있다.
        /// </summary>
        private static bool SweptContact(Vector3 from, Vector3 to, Vector3 point, float radius)
        {
            return Vector3.Distance(ShapeGeometry.ClosestPointOnSegment(from, to, point), point) <= radius;
        }

        private static bool CanBeTrapped(Player player)
        {
            return player.State != PlayerState.Dead && player.State != PlayerState.Trapped;
        }

        private void Trap(Player player, FallingBody drop, int tick, List<SimulationEvent> events)
        {
            player.Anchor = null;
            player.Up = Vector3.UnitY;
            player.State = PlayerState.Trapped;
            player.TrappedDrop = drop;
            player.EscapePresses = 0;
            player.Velocity = Vector3.Zero;
            player.Position = drop.Position;
            drop.Velocity = -Vector3.UnitY * _settings.TrappedFallSpeed;
            events.Add(new PlayerTrapped(tick, drop.Id));
        }

        private static void Kill(Player player, int tick, List<SimulationEvent> events)
        {
            player.TrappedDrop = null;
            player.State = PlayerState.Dead;
            player.Velocity = Vector3.Zero;
            events.Add(new PlayerDied(tick, DeathCause.WaterImpact, player.Position));
        }
    }
}
