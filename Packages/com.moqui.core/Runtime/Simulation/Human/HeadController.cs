using System;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 머리 회전: 목표 각도를 향해 돌리되 yaw/pitch 한계를 넘지 않는다 (spec/02 §3, D-029).
    /// 사람 머리처럼 가속해서 출발하고, 남은 각에 맞춰 감속해 넘치지 않고 멈춘다(최고 속도까지 head.turnAccelTime, M14).
    /// </summary>
    public sealed class HeadController
    {
        private readonly HeadSettings _settings;

        public HeadController(HeadSettings settings)
        {
            _settings = settings;
        }

        public float ClampYaw(float yaw)
        {
            return Math.Clamp(yaw, -_settings.YawLimit, _settings.YawLimit);
        }

        public float ClampPitch(float pitch)
        {
            return Math.Clamp(pitch, -_settings.PitchLimit, _settings.PitchLimit);
        }

        /// <summary>목표에 도달했으면 참.</summary>
        public bool TurnToward(Human human, float targetYaw, float targetPitch, float degreesPerSecond, float deltaTime)
        {
            float yaw = ClampYaw(targetYaw);
            float pitch = ClampPitch(targetPitch);
            human.HeadTargetYaw = yaw;
            human.HeadTargetPitch = pitch;
            float accel = _settings.TurnAccelTime > 0f ? degreesPerSecond / _settings.TurnAccelTime : float.PositiveInfinity;
            float yawVelocity = human.HeadYawVelocity;
            float pitchVelocity = human.HeadPitchVelocity;
            human.HeadYaw = ClampYaw(Ease(human.HeadYaw, yaw, ref yawVelocity, degreesPerSecond, accel, deltaTime));
            human.HeadPitch = ClampPitch(Ease(human.HeadPitch, pitch, ref pitchVelocity, degreesPerSecond, accel, deltaTime));
            human.HeadYawVelocity = yawVelocity;
            human.HeadPitchVelocity = pitchVelocity;
            return human.HeadYaw == yaw && human.HeadPitch == pitch;
        }

        public bool TurnTowardPoint(Human human, System.Numerics.Vector3 point, float degreesPerSecond, float deltaTime)
        {
            human.AnglesToward(point, out float yaw, out float pitch);
            return TurnToward(human, yaw, pitch, degreesPerSecond, deltaTime);
        }

        /// <summary>
        /// 가감속 한 축: 남은 거리에서 멈출 수 있는 속도(√(2·가속·거리))와 최고 속도 중 작은 것을 목표 속도로 삼고,
        /// 속도를 가속도 한도 안에서 그쪽으로 바꾼다. 목표를 지나치면 목표에서 멈춘다.
        /// </summary>
        public static float Ease(float current, float target, ref float velocity, float maxSpeed, float accel, float deltaTime)
        {
            float delta = target - current;
            if (MathF.Abs(delta) < 1e-4f)
            {
                velocity = 0f;
                return target;
            }

            float stoppable = float.IsPositiveInfinity(accel) ? maxSpeed : MathF.Sqrt(2f * accel * MathF.Abs(delta));
            float desired = MathF.Sign(delta) * MathF.Min(maxSpeed, stoppable);
            float change = float.IsPositiveInfinity(accel) ? desired - velocity : Math.Clamp(desired - velocity, -accel * deltaTime, accel * deltaTime);
            velocity += change;
            float step = velocity * deltaTime;
            if (MathF.Sign(step) == MathF.Sign(delta) && MathF.Abs(step) >= MathF.Abs(delta))
            {
                velocity = 0f;
                return target;
            }

            return current + step;
        }
    }
}
