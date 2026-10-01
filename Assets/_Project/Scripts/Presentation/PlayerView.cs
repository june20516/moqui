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
            Quaternion yaw = Quaternion.Euler(0f, player.Yaw, 0f);
            Vector3 localVelocity = Quaternion.Inverse(yaw) * player.Velocity.ToUnity();
            Vector2 target = Lean(localVelocity, simulation.Settings.Flight.Speed);
            _lean = Vector2.Lerp(_lean, target, 1f - Mathf.Exp(-LeanResponse * Time.deltaTime));

            transform.SetPositionAndRotation(_runner.Driver.InterpolatedPlayerPosition, yaw * Quaternion.Euler(_lean.x, 0f, _lean.y));
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
