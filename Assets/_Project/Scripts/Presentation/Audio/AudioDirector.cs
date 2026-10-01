using Moqui.Core.Simulation;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation.Audio
{
    /// <summary>
    /// 스테이지 소리 연결 (spec/10): 시작 시 스테이지 음악·환경음을 켜고,
    /// 틱마다 이벤트·상태 효과음을, 프레임마다 반복음(날개·흡혈·광분·코골이·증기)을 맞춘다.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField]
        private SimulationRunner _runner;

        [SerializeField]
        private StageBootstrap _stage;

        private readonly AudioCues _cues = new AudioCues();
        private AudioOutput _output;
        private SimulationDriver _subscribedDriver;
        private string _ambienceId;

        public string AmbienceId => _ambienceId;

        private void Update()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            if (_subscribedDriver == null)
            {
                Begin();
            }

            if (_output == null)
            {
                return;
            }

            var simulation = _runner.Driver.Simulation;
            MokiPose pose = MokiPoses.From(simulation.Player, simulation.LastCommand.SuckHeld, simulation.Settings.Flight.Speed);
            // 일시정지 중에는 상태 반복음을 끈다 (음악·환경음은 그대로).
            foreach (var loop in AudioCues.Loops(simulation, pose))
            {
                _output.SetLoop(loop.Id, loop.Playing && !_runner.Paused, loop.Pitch);
            }
        }

        private void Begin()
        {
            _subscribedDriver = _runner.Driver;
            _subscribedDriver.TickCompleted += OnTick;
            _output = AudioOutput.Ensure();
            if (_output == null)
            {
                return;
            }

            _output.PlayMusic(AudioIds.BgmStage);
            _ambienceId = _stage != null && _stage.Level != null ? AudioIds.AmbienceForLevel(_stage.Level.Id) : null;
            if (_ambienceId != null)
            {
                _output.SetLoop(_ambienceId, true);
            }
        }

        private void OnTick(GameSimulation simulation)
        {
            // 파괴된 Unity 오브젝트는 C# `?.`로 걸러지지 않으므로 Unity 비교로 확인한다.
            if (_output == null)
            {
                return;
            }

            foreach (string id in _cues.OneShots(simulation))
            {
                _output.PlayOneShot(id);
            }
        }

        private void OnDestroy()
        {
            if (_subscribedDriver != null)
            {
                _subscribedDriver.TickCompleted -= OnTick;
            }

            // 다음 화면으로 넘어가도 반복음이 남지 않게 끈다. 음악은 다음 화면이 바꾼다.
            // 앱 종료 때는 출구가 먼저 파괴될 수 있다.
            if (_output != null)
            {
                _output.StopAllLoops();
            }
        }
    }
}
