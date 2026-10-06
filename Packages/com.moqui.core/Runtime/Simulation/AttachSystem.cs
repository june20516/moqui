using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 표면 부착 (spec/03 벽면 부착, spec/01 상태).
    /// Flying에서 Attach 입력 + attach.snapRange 이내의 attachable 표면 → 가장 가까운 점에 Attached (표현은 짧게 미끄러져 붙는다, gulf §2).
    /// 정밀 비행으로 표면 쪽으로 날다 suck.attachRange 안에 닿으면 입력 없이 내려앉는다. 표면이 움직이면 따라간다.
    /// 이동 입력이나 Attach 입력으로 이탈하며(흡혈 중에는 이동 입력 무시), 법선 방향으로 attach.detachOffset만큼 떨어진다.
    /// 부착점의 속도가 human.dislodgeSpeed를 넘으면 튕겨 나간다 (spec/02 §6).
    /// </summary>
    public sealed class AttachSystem
    {
        private readonly AttachSettings _settings;
        private readonly HumanMotionSettings _motion;
        private readonly FlightSettings _flight;
        private readonly SphereMover _mover;
        private readonly CollisionWorld _world;

        public AttachSystem(AttachSettings settings, HumanMotionSettings motion, FlightSettings flight, CollisionWorld world, SphereMover mover)
        {
            _settings = settings;
            _motion = motion;
            _flight = flight;
            _world = world;
            _mover = mover;
        }

        public bool TryAttach(Player player, in PlayerCommand command, int tick, List<SimulationEvent> events)
        {
            if (!command.AttachPressed || player.State != PlayerState.Flying)
            {
                return false;
            }

            if (!_world.ClosestSurface(player.Position, _settings.SnapRange, ShapeFlags.Attachable, out var surface))
            {
                return false;
            }

            AttachTo(player, surface, tick, events);
            return true;
        }

        /// <summary>
        /// 정밀 비행 자동 착지 (gulf §2, D-066): 정밀 비행 + 이동 입력으로 표면 쪽으로 날다(이동 전 속도 방향이 −법선과 autoLandAlign 이상)
        /// 표면에 닿으면(attachRange 안) 내려앉는다. 스치듯 지나가거나 표면에서 멀어지는 중에는 붙지 않는다.
        /// </summary>
        public bool TryAutoLand(Player player, in PlayerCommand command, Vector3 intendedVelocity, int tick, List<SimulationEvent> events)
        {
            if (player.State != PlayerState.Flying || !command.PrecisionHeld || !HasMoveInput(command) || intendedVelocity.LengthSquared() < 1e-6f)
            {
                return false;
            }

            if (!_world.ClosestSurface(player.Position, _settings.AttachRange, ShapeFlags.Attachable, out var surface))
            {
                return false;
            }

            if (Vector3.Dot(Vector3.Normalize(intendedVelocity), -surface.Normal) < _settings.AutoLandAlign)
            {
                return false;
            }

            AttachTo(player, surface, tick, events);
            player.HoldAfterAutoLand = true;
            return true;
        }

        private void AttachTo(Player player, SurfacePoint surface, int tick, List<SimulationEvent> events)
        {
            player.HoldAfterAutoLand = false;
            player.Anchor = SurfaceAnchor.Create(surface.Shape, surface.Point, surface.Normal);
            player.State = PlayerState.Attached;
            player.Velocity = Vector3.Zero;
            player.AttachedTick = tick;
            player.AnchorVelocity = Vector3.Zero;
            PlaceOnAnchor(player);
            events.Add(new PlayerAttached(tick, surface.Shape.Id, surface.Shape.Matches(ShapeFlags.SkinSite)));
        }

        /// <summary>지금 F를 누르면 붙을 지점과 법선 (착지 표시, gulf §2). 비행 중이 아니거나 없으면 거짓.</summary>
        public bool TryGetTarget(Player player, out Vector3 point, out Vector3 normal)
        {
            point = Vector3.Zero;
            normal = Vector3.UnitY;
            if (player.State != PlayerState.Flying || !_world.ClosestSurface(player.Position, _settings.SnapRange, ShapeFlags.Attachable, out var surface))
            {
                return false;
            }

            point = surface.Point;
            normal = surface.Normal;
            return true;
        }

        /// <summary>비행 중이고 snapRange 안에 붙을 표면이 있는가 (HUD 착지 프롬프트, 모키 큐 land.ready).</summary>
        public bool HasTarget(Player player)
        {
            return player.State == PlayerState.Flying && _world.ClosestSurface(player.Position, _settings.SnapRange, ShapeFlags.Attachable, out _);
        }

        public static bool HasMoveInput(in PlayerCommand command)
        {
            return command.Move.LengthSquared() > 0f || command.Vertical != 0f;
        }

        /// <summary>
        /// 흡혈 중인가: Suck을 누른 채 흡혈 세션이 진행 중이다. 이때 몸은 고정되어 이동 입력으로 떨어지지 않는다 (spec/04 §2, M13).
        /// F(떼기)와 대시(긴급 탈출)로는 빠져나올 수 있다.
        /// </summary>
        public static bool IsSucking(Player player, in PlayerCommand command)
        {
            return command.SuckHeld && player.SuckSession != null;
        }

        /// <summary>부착 중 1틱: 이탈 입력이면 이탈하고, 아니면 움직이는 표면을 따라간다.</summary>
        public void StepAttached(Player player, in PlayerCommand command, int tick, float deltaTime, List<SimulationEvent> events)
        {
            // 자동 착지 직후에는 그 이동 키를 놓을 때까지 붙어 있다(누르던 키에 바로 떨어지지 않게). 표면에서 멀어지는 입력이면 뗀다 (gulf §2).
            bool hasMove = HasMoveInput(command);
            if (!hasMove)
            {
                player.HoldAfterAutoLand = false;
            }

            bool held = player.HoldAfterAutoLand && !PointsAwayFromSurface(player, command);
            bool moveDetaches = hasMove && !IsSucking(player, command) && !held;
            if (command.AttachPressed || moveDetaches)
            {
                Detach(player, tick, events);
                return;
            }

            Vector3 previous = player.Position;
            PlaceOnAnchor(player);
            player.AnchorVelocity = (player.Position - previous) / deltaTime;
        }

        /// <summary>이동 입력 방향(카메라 기준 수평 + 상하)이 붙은 표면에서 멀어지는 쪽(법선과 예각)인가.</summary>
        public static bool PointsAwayFromSurface(Player player, in PlayerCommand command)
        {
            Vector3 direction = CameraBasis.FromYaw(command.LookYaw).ToWorld(command.Move) + (Vector3.UnitY * Math.Clamp(command.Vertical, -1f, 1f));
            if (direction.LengthSquared() < 1e-6f || player.Anchor == null)
            {
                return false;
            }

            player.Anchor.Resolve(out _, out Vector3 normal);
            return Vector3.Dot(Vector3.Normalize(direction), normal) > 0.1f;
        }

        /// <summary>부위 움직임으로 튕겨 나가야 하면 처리하고 참을 돌려준다.</summary>
        /// <param name="gripMultiplier">튕김 기준 속도 배율 (흡혈하며 버티기, D-056). 기본 1.</param>
        /// <param name="carriedVelocity">몸 전체 이동만으로 생긴 부착점 속도 (걷는 인간, spec/02 §9). 이것을 빼고 판정한다.</param>
        public bool TryDislodge(Player player, int tick, List<SimulationEvent> events, float gripMultiplier = 1f, Vector3 carriedVelocity = default)
        {
            if (player.State != PlayerState.Attached || (player.AnchorVelocity - carriedVelocity).Length() <= _motion.DislodgeSpeed * gripMultiplier)
            {
                return false;
            }

            Vector3 direction = Vector3.Normalize(player.AnchorVelocity);
            string shapeId = player.Anchor.Shape.Id;
            Release(player);
            player.State = PlayerState.Dislodged;
            player.StunEndTick = tick + SimulationTime.ToTicks(_motion.DislodgeStun);

            // 밀림 방향으로 순간 속도를 주고 일반 감속을 따른다 (spec/01). 감속으로 정확히 dislodgePush만큼 미끄러지는 속도.
            float deceleration = _flight.Speed / _flight.DecelTime;
            player.Velocity = direction * MathF.Sqrt(2f * deceleration * _motion.DislodgePush);
            events.Add(new PlayerDislodged(tick, shapeId, direction));
            return true;
        }

        public void Detach(Player player, int tick, List<SimulationEvent> events)
        {
            player.Anchor.Resolve(out _, out Vector3 normal);
            string shapeId = player.Anchor.Shape.Id;
            Release(player);
            player.State = PlayerState.Flying;
            var move = _mover.MoveStraight(player.Position, player.CollisionRadius, normal * _settings.DetachOffset, ShapeFlags.Solid);
            player.Position = move.Position;
            events.Add(new PlayerDetached(tick, shapeId));
        }

        private void PlaceOnAnchor(Player player)
        {
            player.Anchor.Resolve(out Vector3 point, out Vector3 normal);
            player.Position = point + (normal * (player.CollisionRadius + SphereMover.Skin));
            player.Up = normal;
        }

        private static void Release(Player player)
        {
            player.HoldAfterAutoLand = false;
            player.Anchor = null;
            player.Up = Vector3.UnitY;
            player.AnchorVelocity = Vector3.Zero;
        }
    }
}
