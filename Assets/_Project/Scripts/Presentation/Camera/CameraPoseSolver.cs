using Moqui.Core.Collision;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>
    /// 시점별 카메라 포즈 계산 (spec/00). 3인칭 벽 충돌은 Core 충돌 월드에 구 sweep으로 처리한다 (D-027).
    /// pitch 양수는 위를 본다 (Unity의 Euler x는 아래가 양수이므로 부호를 바꾼다).
    /// </summary>
    public sealed class CameraPoseSolver
    {
        private readonly CameraSettings _settings;
        private readonly CollisionWorld _world;

        public CameraPoseSolver(CameraSettings settings, CollisionWorld world)
        {
            _settings = settings;
            _world = world;
        }

        public static Quaternion LookRotation(float yaw, float pitch)
        {
            return Quaternion.Euler(-pitch, yaw, 0f);
        }

        public CameraPose ThirdPerson(Vector3 playerPosition, float yaw, float pitch)
        {
            // 피벗이 천장 안으로 들어가지 않도록 플레이어 중심에서 피벗까지 먼저 sweep한다.
            Vector3 pivot = SweepTo(playerPosition, Vector3.up, _settings.HeightOffset);
            Quaternion rotation = LookRotation(yaw, pitch);
            Vector3 position = SweepTo(pivot, rotation * Vector3.back, _settings.Distance);
            return new CameraPose(position, rotation, _settings.Fov, _settings.NearClip);
        }

        public CameraPose FirstPerson(Vector3 playerPosition, float yaw, float pitch)
        {
            Vector3 eye = playerPosition + (Quaternion.Euler(0f, yaw, 0f) * _settings.FirstPersonEyeOffset);
            return new CameraPose(eye, LookRotation(yaw, pitch), _settings.FirstPersonFov, _settings.FirstPersonNearClip);
        }

        private Vector3 SweepTo(Vector3 origin, Vector3 direction, float distance)
        {
            bool hit = _world.SphereSweep(
                origin.ToCore(),
                _settings.CollisionRadius,
                direction.ToCore(),
                distance,
                ShapeFlags.Obstacle,
                out var collision);
            float travel = hit ? Mathf.Max(0f, collision.Distance - SphereMover.Skin) : distance;
            return origin + (direction * travel);
        }
    }
}
