using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>플레이어 그림을 보간 위치와 시뮬레이션 yaw에 맞추고, 이동 방향으로 몸을 기울인다 (spec/10 이동 기울기).</summary>
    public sealed class PlayerView : MonoBehaviour
    {
        /// <summary>최고 비행 속도일 때의 기울기 (도).</summary>
        public const float MaxLeanDegrees = 25f;

        /// <summary>기울기가 목표를 따라가는 속도 (1/s).</summary>
        private const float LeanResponse = 10f;

        [SerializeField]
        private SimulationRunner _runner;

        private Vector2 _lean;

        private void LateUpdate()
        {
            if (!_runner.IsRunning)
            {
                return;
            }

            var simulation = _runner.Driver.Simulation;
            var player = simulation.Player;
            if (player.State == Core.Simulation.PlayerState.Attached)
            {
                // 벽·천장에 붙으면 몸의 up을 표면 법선에 맞춰 "앉은" 자세로 보이게 한다 (spec/03, M12).
                _lean = Vector2.zero;
                transform.SetPositionAndRotation(_runner.Driver.InterpolatedPlayerPosition, AttachedRotation(player.Up.ToUnity(), player.Yaw));
                return;
            }

            Quaternion yaw = Quaternion.Euler(0f, player.Yaw, 0f);
            Vector3 localVelocity = Quaternion.Inverse(yaw) * player.Velocity.ToUnity();
            Vector2 target = Lean(localVelocity, simulation.Settings.Flight.Speed);
            _lean = Vector2.Lerp(_lean, target, 1f - Mathf.Exp(-LeanResponse * Time.deltaTime));

            transform.SetPositionAndRotation(_runner.Driver.InterpolatedPlayerPosition, yaw * Quaternion.Euler(_lean.x, 0f, _lean.y));
        }

        /// <summary>부착 중 몸 방향: up = 표면 법선, 앞 = 시점 방향을 표면에 투영한 방향 (투영이 거의 0이면 월드 위쪽을 투영).</summary>
        public static Quaternion AttachedRotation(Vector3 surfaceNormal, float yawDegrees)
        {
            Vector3 up = surfaceNormal.normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward, up);
            if (forward.sqrMagnitude < 1e-4f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.up, up);
            }

            return Quaternion.LookRotation(forward.normalized, up);
        }

        /// <summary>로컬 속도 → (앞뒤 pitch, 좌우 roll) 기울기. 앞으로 가면 앞으로, 오른쪽으로 가면 오른쪽으로 기운다.</summary>
        public static Vector2 Lean(Vector3 localVelocity, float flightSpeed)
        {
            float forward = Mathf.Clamp(localVelocity.z / flightSpeed, -1f, 1f);
            float right = Mathf.Clamp(localVelocity.x / flightSpeed, -1f, 1f);
            return new Vector2(forward * MaxLeanDegrees, -right * MaxLeanDegrees);
        }
    }
}
