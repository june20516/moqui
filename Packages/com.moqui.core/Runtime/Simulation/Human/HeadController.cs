using System;

namespace Moqui.Core.Simulation
{
    /// <summary>머리 회전: 목표 각도를 향해 주어진 속도로 돌리되 yaw/pitch 한계를 넘지 않는다 (spec/02 §3, D-029).</summary>
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
            float step = degreesPerSecond * deltaTime;
            human.HeadYaw = ClampYaw(MoveTowards(human.HeadYaw, yaw, step));
            human.HeadPitch = ClampPitch(MoveTowards(human.HeadPitch, pitch, step));
            return human.HeadYaw == yaw && human.HeadPitch == pitch;
        }

        public bool TurnTowardPoint(Human human, System.Numerics.Vector3 point, float degreesPerSecond, float deltaTime)
        {
            human.AnglesToward(point, out float yaw, out float pitch);
            return TurnToward(human, yaw, pitch, degreesPerSecond, deltaTime);
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            float delta = target - current;
            return MathF.Abs(delta) <= maxDelta ? target : current + (MathF.Sign(delta) * maxDelta);
        }
    }
}
