using Moqui.Unity.Settings;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>시뮬레이션 상태를 읽어 카메라를 놓는다. 규칙은 바꾸지 않는다.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField]
        private SimulationRunner _runner;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private PlayerViewVisibility _playerVisibility;

        private CameraController _controller;

        public CameraController Controller => _controller;

        private void LateUpdate()
        {
            if (!_runner.IsRunning)
            {
                return;
            }

            if (_controller == null)
            {
                var settings = new CameraSettings(_runner.Tuning);
                var solver = new CameraPoseSolver(settings, _runner.Driver.Simulation.World);
                _controller = new CameraController(settings, solver, new PlayerPrefsStore());
            }

            var collector = _runner.Driver.Collector;
            if (collector.ConsumeToggleView())
            {
                _controller.Toggle();
            }

            if (_playerVisibility != null)
            {
                _playerVisibility.SetFirstPerson(_controller.IsFirstPerson);
            }

            CameraPose pose = _controller.Update(Time.deltaTime, _runner.Driver.InterpolatedPlayerPosition, collector.Look.Yaw, collector.Look.Pitch);
            pose.ApplyTo(_camera);
        }
    }
}
