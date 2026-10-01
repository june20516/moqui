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

        /// <summary>
        /// 1인칭 부착 상태: 카메라 up을 표면 법선 쪽으로 맞추고, 눈 위치 오프셋도 표면 기준(법선 = 위)으로 둔다 (spec/00).
        /// 시선이 법선과 거의 평행하면 up을 정할 수 없으므로 몸 정면(yaw)을 up으로 쓴다.
        /// </summary>
        public CameraPose FirstPersonAttached(Vector3 playerPosition, float yaw, float pitch, Vector3 surfaceNormal)
        {
            const float ParallelThresholdDegrees = 1f;
            Vector3 direction = LookConstraint.Direction(yaw, pitch);
            Vector3 normal = surfaceNormal.normalized;
            Vector3 tangent = Vector3.ProjectOnPlane(direction, normal);
            bool nearlyParallel = Vector3.Angle(direction, normal) < ParallelThresholdDegrees || tangent.sqrMagnitude < 1e-6f;
            Vector3 up = nearlyParallel ? Quaternion.Euler(0f, yaw, 0f) * Vector3.forward : normal;
            Vector3 forwardOnSurface = nearlyParallel ? Vector3.ProjectOnPlane(up, normal).normalized : tangent.normalized;
            Vector3 offset = _settings.FirstPersonEyeOffset;
            Vector3 eye = playerPosition + (normal * offset.y) + (forwardOnSurface * offset.z);
            return new CameraPose(eye, Quaternion.LookRotation(direction, up), _settings.FirstPersonFov, _settings.FirstPersonNearClip);
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
