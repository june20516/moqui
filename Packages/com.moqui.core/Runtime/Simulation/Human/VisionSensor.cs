using System;
using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 시각 (spec/02 §1). 눈(머리 중심)에서 Yellow/Red 원뿔과 시야 확보를 판정한다.
    /// 시야 확보 광선은 vision.losCheckInterval마다 다시 쏘고, 그 사이에는 직전 결과를 쓴다.
    /// </summary>
    public sealed class VisionSensor
    {
        private const float RadiansToDegrees = 180f / MathF.PI;

        private readonly VisionSettings _settings;
        private readonly CollisionWorld _world;
        private readonly int _losIntervalTicks;
        private readonly float _steamVisionMul;
        private readonly float _drunkVisionMul;
        private readonly float _lightVisionMul;

        /// <param name="steamVisionMul">증기 속 플레이어에 대한 시각 배율 (humid.steamVisionMul, spec/05 §2).</param>
        /// <param name="drunkVisionMul">취한 타겟의 시각 증가 배율 (drunk.visionRateMul, spec/06).</param>
        /// <param name="lightVisionMul">켜진 조명 영역 안 플레이어에 대한 시각 배율 (light.visionMul, spec/06 M14).</param>
        public VisionSensor(VisionSettings settings, CollisionWorld world, float steamVisionMul = 1f, float drunkVisionMul = 1f, float lightVisionMul = 1f)
        {
            _lightVisionMul = lightVisionMul;
            _settings = settings;
            _world = world;
            _steamVisionMul = steamVisionMul;
            _drunkVisionMul = drunkVisionMul;
            _losIntervalTicks = Math.Max(1, SimulationTime.ToTicks(settings.LosCheckInterval));
        }

        public void Sense(Human human, Player player, int tick, ref HumanPerception perception)
        {
            if (player.State == PlayerState.Dead)
            {
                return;
            }

            Vector3 eye = human.HeadCenter;
            Vector3 toPlayer = player.Position - eye;
            float distance = toPlayer.Length();
            float angle = AngleBetween(human.HeadForward, toPlayer);
            bool inYellow = distance <= _settings.YellowRange && angle <= _settings.YellowHalfAngle;
            bool inRed = distance <= _settings.RedRange && angle <= _settings.RedHalfAngle;

            if (tick - human.LineOfSightCheckedTick >= _losIntervalTicks)
            {
                human.LineOfSightCached = HasLineOfSight(eye, toPlayer, distance, player.CollisionRadius);
                human.LineOfSightCheckedTick = tick;
            }

            float multiplier = SuckEventSystem.HiddenByFreezing(human, player) ? 0f : VisionMultiplier(player);
            bool visible = human.LineOfSightCached && multiplier > 0f;
            perception.PlayerSeen = inYellow && visible;
            perception.RedZoneTriggered = inRed && visible;
            perception.PlayerOccluded = inYellow && !human.LineOfSightCached;
            if (perception.PlayerSeen)
            {
                float t = Math.Clamp(distance / _settings.YellowRange, 0f, 1f);
                float drunk = human.IsDrunk ? _drunkVisionMul : 1f;
                perception.VisionRate += Lerp(_settings.YellowRateNear, _settings.YellowRateFar, t) * multiplier * drunk;
                perception.AddStimulus(player.Position);
            }
        }

        public float VisionMultiplier(Player player)
        {
            float multiplier = 1f;
            if (player.State == PlayerState.Attached)
            {
                multiplier *= _settings.AttachedMul;
            }

            if (player.IsHidden)
            {
                multiplier *= _settings.ShadowMul;
            }

            if (player.InSteam)
            {
                multiplier *= _steamVisionMul;
            }

            if (player.InLight)
            {
                multiplier *= _lightVisionMul;
            }

            return multiplier;
        }

        private bool HasLineOfSight(Vector3 eye, Vector3 toPlayer, float distance, float playerRadius)
        {
            float rayLength = distance - playerRadius;
            if (rayLength <= 0f)
            {
                return true;
            }

            return !_world.Raycast(eye, toPlayer, rayLength, ShapeFlags.Obstacle, out _, ShapeFlags.Body);
        }

        private static float AngleBetween(Vector3 a, Vector3 b)
        {
            float lengths = a.Length() * b.Length();
            if (lengths <= 0f)
            {
                return 0f;
            }

            return MathF.Acos(Math.Clamp(Vector3.Dot(a, b) / lengths, -1f, 1f)) * RadiansToDegrees;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * t);
        }
    }
}
