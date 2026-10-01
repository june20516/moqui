using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 표면 부착 (spec/03 벽면 부착, spec/01 상태).
    /// Flying에서 Attach 입력 + suck.attachRange 이내의 attachable 표면 → Attached. 표면이 움직이면 따라간다.
    /// 이동 입력이나 Attach 입력으로 이탈하며, 법선 방향으로 attach.detachOffset만큼 떨어진다.
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

            if (!_world.ClosestSurface(player.Position, _settings.AttachRange, ShapeFlags.Attachable, out var surface))
            {
                return false;
            }

            player.Anchor = SurfaceAnchor.Create(surface.Shape, surface.Point, surface.Normal);
            player.State = PlayerState.Attached;
            player.Velocity = Vector3.Zero;
            player.AttachedTick = tick;
            player.AnchorVelocity = Vector3.Zero;
            PlaceOnAnchor(player);
            events.Add(new PlayerAttached(tick, surface.Shape.Id, surface.Shape.Matches(ShapeFlags.SkinSite)));
            return true;
        }

        /// <summary>비행 중이고 attachRange 안에 붙을 표면이 있는가 (HUD 착지 프롬프트).</summary>
        public bool HasTarget(Player player)
        {
            return player.State == PlayerState.Flying && _world.ClosestSurface(player.Position, _settings.AttachRange, ShapeFlags.Attachable, out _);
        }

        public static bool HasMoveInput(in PlayerCommand command)
        {
            return command.Move.LengthSquared() > 0f || command.Vertical != 0f;
        }

        /// <summary>부착 중 1틱: 이탈 입력이면 이탈하고, 아니면 움직이는 표면을 따라간다.</summary>
        public void StepAttached(Player player, in PlayerCommand command, int tick, float deltaTime, List<SimulationEvent> events)
        {
            if (command.AttachPressed || HasMoveInput(command))
            {
                Detach(player, tick, events);
                return;
            }

            Vector3 previous = player.Position;
            PlaceOnAnchor(player);
            player.AnchorVelocity = (player.Position - previous) / deltaTime;
        }

        /// <summary>부위 움직임으로 튕겨 나가야 하면 처리하고 참을 돌려준다.</summary>
        public bool TryDislodge(Player player, int tick, List<SimulationEvent> events)
        {
            if (player.State != PlayerState.Attached || player.AnchorVelocity.Length() <= _motion.DislodgeSpeed)
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
            player.Anchor = null;
            player.Up = Vector3.UnitY;
            player.AnchorVelocity = Vector3.Zero;
        }
    }
}
