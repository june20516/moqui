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
        private bool _tiersSet;
        private MokiCueSystem _cues;
        private TextCuePresenter _cuePresenter;
        private SimulationDriver _cueDriver;

        /// <summary>모키 표현 큐 표시기 (지금은 글자, spec/12).</summary>
        public TextCuePresenter CuePresenter => _cuePresenter;

        public HudState LastState { get; private set; }

        private void Awake()
        {
            _view = GetComponent<HudView>();
            _view.Build();
            _presenter = new HudPresenter(_view, _audio);
            _tiersSet = false;
            _hints = new TutorialHints(new PlayerPrefsStore());
            _cuePresenter = gameObject.AddComponent<TextCuePresenter>();
        }

        /// <summary>모키 표현 큐: 시뮬레이션 틱마다 큐를 내고, 모키 머리 옆에 작은 글자로 보여 준다 (spec/12).</summary>
        private void BindCues()
        {
            if (_cues != null)
            {
                return;
            }

            _cues = new MokiCueSystem(_runner.Tuning.GetFloat("hud.satietyHighlightMul"));
            _cueDriver = _runner.Driver;
            _cueDriver.TickCompleted += OnTick;
            var driver = _cueDriver;
            float headHeight = _runner.Tuning.GetFloat("player.visualHeight") * 0.6f;
            _cuePresenter.Bind(_camera, () => driver.InterpolatedPlayerPosition + (Vector3.up * headHeight));
        }

        private void OnTick(Moqui.Core.Simulation.GameSimulation simulation)
        {
            _cues.Step(simulation, _cuePresenter);
        }

        private void OnDestroy()
        {
            if (_cueDriver != null)
            {
                _cueDriver.TickCompleted -= OnTick;
            }
        }

        private void LateUpdate()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            UpdateDeviceLabels();
            BindCues();
            if (!_tiersSet)
            {
                var toxin = _runner.Driver.Simulation.Settings.Toxin;
                _view.SetToxinTiers(toxin.Tier1 / Moqui.Core.Simulation.ToxinSystem.GaugeMax, toxin.Tier2 / Moqui.Core.Simulation.ToxinSystem.GaugeMax, toxin.Tier3 / Moqui.Core.Simulation.ToxinSystem.GaugeMax);
                _tiersSet = true;
            }

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
