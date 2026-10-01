using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Settings;
using Moqui.Unity.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>Stage 씬에서 프레임마다 시뮬레이션 상태로 HUD를 갱신한다.</summary>
    [RequireComponent(typeof(HudView))]
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField]
        private SimulationRunner _runner;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private CameraRig _cameraRig;

        [SerializeField]
        private HudAudioSource _audio;

        [SerializeField]
        private StageBootstrap _stage;

        private HudView _view;
        private HudPresenter _presenter;
        private TutorialHints _hints;

        public HudState LastState { get; private set; }

        private void Awake()
        {
            _view = GetComponent<HudView>();
            _view.Build();
            _presenter = new HudPresenter(_view, _audio);
            _hints = new TutorialHints(new PlayerPrefsStore());
        }

        private void LateUpdate()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            UpdateDeviceLabels();
            bool firstPerson = _cameraRig != null && _cameraRig.Controller != null && _cameraRig.Controller.IsFirstPerson;
            LastState = HudState.Compute(_runner.Driver.Simulation, _camera, firstPerson, _runner.Tuning);
            _presenter.Present(LastState, Time.time);
            _view.TutorialText.text = _hints.CurrentText(_stage != null ? _stage.Tutorial : null, _view.UseGamepadLabels);
        }

        /// <summary>마지막으로 입력이 들어온 장치에 맞춰 프롬프트 표기를 바꾼다 (spec/08).</summary>
        private void UpdateDeviceLabels()
        {
            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            {
                _view.UseGamepadLabels = true;
            }
            else if ((Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame) || (Mouse.current != null && Mouse.current.wasUpdatedThisFrame))
            {
                _view.UseGamepadLabels = false;
            }
        }
    }
}
