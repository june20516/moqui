using System;
using System.Numerics;
using Moqui.Core.Simulation;

namespace Moqui.Core.Bots
{
    /// <summary>봇의 비행 조종: 목표 점까지 커맨드를 만든다. 가까우면 정밀 비행으로 감속한다.</summary>
    public static class BotPilot
    {
        public const float ArriveDistance = 0.8f;

        private const float PrecisionDistance = 12f;
        private const float DeadZone = 0.5f;
        private const float RadiansToDegrees = 180f / MathF.PI;

        /// <summary>흡혈 중 시선 이벤트(돌림·응시·되돌림)가 오면 흡혈을 멈추고 붙은 채 얼어 있어야 한다 (spec/04 §8, D-056).</summary>
        public static bool ShouldFreeze(Human human)
        {
            return human != null && human.SuckEvent.Kind == SuckEventKind.Glance;
        }

        public static bool Arrived(Player player, Vector3 target)
        {
            return Vector3.Distance(player.Position, target) < ArriveDistance;
        }

        public static PlayerCommand FlyTo(Player player, Vector3 target, bool precise = false)
        {
            if (player.State == PlayerState.Attached)
            {
                // 이동 입력으로 이탈한다 (spec/03).
                return new PlayerCommand { Vertical = 1f };
            }

            Vector3 delta = target - player.Position;
            float distance = delta.Length();
            if (distance < ArriveDistance)
            {
                return PlayerCommand.None;
            }

            var horizontal = new Vector2(delta.X, delta.Z);
            return new PlayerCommand
            {
                LookYaw = MathF.Atan2(delta.X, delta.Z) * RadiansToDegrees,
                Move = horizontal.Length() > DeadZone ? new Vector2(0f, 1f) : Vector2.Zero,
                Vertical = MathF.Abs(delta.Y) > DeadZone ? MathF.Sign(delta.Y) : 0f,
                PrecisionHeld = precise || distance < PrecisionDistance,
            };
        }
    }
}
