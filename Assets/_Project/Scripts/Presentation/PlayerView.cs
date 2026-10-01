using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>플레이어 그림을 보간 위치와 시뮬레이션 yaw에 맞춘다.</summary>
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField]
        private SimulationRunner _runner;

        private void LateUpdate()
        {
            if (!_runner.IsRunning)
            {
                return;
            }

            var player = _runner.Driver.Simulation.Player;
            transform.SetPositionAndRotation(_runner.Driver.InterpolatedPlayerPosition, Quaternion.Euler(0f, player.Yaw, 0f));
        }
    }
}
