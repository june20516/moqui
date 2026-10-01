using System;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 약한 관성 비행 (spec/01 이동, D-016). 수평과 수직 속도가 각각 목표 속도를 향해 접근한다.
    /// 입력이 있으면 최대 가속도 speed / accelTime, 없으면 최대 감속도 speed / decelTime을 쓴다.
    /// </summary>
    public sealed class FlightSystem
    {
        private readonly FlightSettings _settings;

        public FlightSystem(FlightSettings settings)
        {
            _settings = settings;
        }

        /// <summary>이번 틱의 새 관성 속도를 계산해 player.Velocity에 넣는다.</summary>
        public void UpdateVelocity(Player player, in PlayerCommand command, float deltaTime)
        {
            float speedMul = player.SpeedMultiplier * (command.PrecisionHeld ? _settings.PrecisionSpeedMul : 1f);
            float accelTime = _settings.AccelTime * player.AccelTimeMultiplier;
            float decelTime = _settings.DecelTime * player.AccelTimeMultiplier;

            Vector2 move = ClampLength(command.Move);
            Vector3 horizontalTarget = CameraBasis.FromYaw(command.LookYaw).ToWorld(move) * (_settings.Speed * speedMul);
            float verticalInput = Math.Clamp(command.Vertical, -1f, 1f);
            float verticalTarget = verticalInput * _settings.VerticalSpeed * speedMul;

            Vector3 current = player.Velocity;
            var horizontal = new Vector3(current.X, 0f, current.Z);
            bool hasHorizontalInput = move.LengthSquared() > 0f;
            float horizontalRate = _settings.Speed / (hasHorizontalInput ? accelTime : decelTime);
            horizontal = MoveTowards(horizontal, horizontalTarget, horizontalRate * deltaTime);

            bool hasVerticalInput = verticalInput != 0f;
            float verticalRate = _settings.VerticalSpeed / (hasVerticalInput ? accelTime : decelTime);
            float vertical = MoveTowards(current.Y, verticalTarget, verticalRate * deltaTime);

            player.Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
        }

        private static Vector2 ClampLength(Vector2 move)
        {
            float lengthSquared = move.LengthSquared();
            return lengthSquared > 1f ? move / MathF.Sqrt(lengthSquared) : move;
        }

        private static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
        {
            Vector3 delta = target - current;
            float length = delta.Length();
            return length <= maxDelta || length == 0f ? target : current + (delta / length * maxDelta);
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            float delta = target - current;
            return MathF.Abs(delta) <= maxDelta ? target : current + (MathF.Sign(delta) * maxDelta);
        }
    }
}
