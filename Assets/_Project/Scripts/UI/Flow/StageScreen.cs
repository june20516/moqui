using System.Linq;
using Moqui.Core.Simulation;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// Stage 씬의 Pause와 Result (spec/08). Pause 중에는 시뮬레이션 틱과 입력 전달을 멈춘다.
    /// 클리어·사망 후 hud.resultDelay(사망 연출) 뒤 Result를 띄우고, 그 순간 기록·보상을 저장한다.
    /// </summary>
    public sealed class StageScreen : ScreenBase
    {
        /// <summary>스테이지 음악은 AudioDirector가 튼다.</summary>
        protected override string MusicId => null;

        protected override bool IsGameplay => true;

        [SerializeField]
        private SimulationRunner _runner;

        [SerializeField]
        private StageBootstrap _stage;

        [SerializeField]
        private InputActionAsset _controls;

        private InputAction _pause;
        private GameSimulation _subscribed;
        private DeathCause? _deathCause;
        private StageResult _clearResult;
        private float _endedSeconds;

        public GameObject PausePanel { get; private set; }

        public Button ResumeButton { get; private set; }

        public Button PauseRetryButton { get; private set; }

        public Button PauseStageSelectButton { get; private set; }

        public Button PauseSettingsButton { get; private set; }

        public SettingsPanel Settings { get; private set; }

        public GameObject ResultPanel { get; private set; }

        public Text ResultTitle { get; private set; }

        public Text ResultBody { get; private set; }

        public Button EndingButton { get; private set; }

        public Button RetryButton { get; private set; }

        public Button SkillsButton { get; private set; }

        public Button StageSelectButton { get; private set; }

        public bool IsPaused => PausePanel.activeSelf;

        public StageOutcomeInfo Outcome { get; private set; }

        /// <summary>테스트용 연결 (씬에서는 직렬화 필드).</summary>
        public void Connect(SimulationRunner runner, StageBootstrap stage)
        {
            _runner = runner;
            _stage = stage;
        }

        public void Pause()
        {
            if (Outcome != null || IsPaused)
            {
                return;
            }

            _runner.Paused = true;
            Time.timeScale = 0f;
            PausePanel.SetActive(true);
            CursorPolicy.ForMenu();
            UiFactory.Focus(ResumeButton);
        }

        public void Resume()
        {
            Settings.Close();
            PausePanel.SetActive(false);
            Time.timeScale = 1f;
            _runner.Paused = false;
            CursorPolicy.ForGameplay();
        }

        /// <summary>시뮬레이션이 끝났으면 기록하고 Result를 보여 준다.</summary>
        public void ShowResult()
        {
            var simulation = _runner.Driver.Simulation;
            Time.timeScale = 1f;
            _runner.Paused = true;
            PausePanel.SetActive(false);
            Flow.Session.SelectedLevelId ??= _stage.Level.Id;
            var result = _clearResult ?? new StageResult(simulation.Tick, simulation.Humans.Sum(human => human.FrenzyCount), simulation.Humans.Sum(human => human.BiteMarkCount));
            Outcome = Flow.CompleteStage(simulation.Outcome, result, _deathCause, result.ClearSeconds);

            bool cleared = Outcome.Outcome == StageOutcome.Cleared;
            ResultTitle.text = cleared ? "클리어!" : "실패";
            ResultBody.text = cleared ? ClearText(Outcome) : $"사망 원인: {CauseText(Outcome.DeathCause)}\n시간 {Outcome.Seconds:0.0}초\n획득 혈액 포인트 0";
            EndingButton.gameObject.SetActive(Outcome.UnlocksEnding);
            ResultPanel.SetActive(true);
            CursorPolicy.ForMenu();
            UiFactory.Focus(Outcome.UnlocksEnding ? EndingButton : RetryButton);
        }

        protected override void Build()
        {
            var canvas = UiFactory.CreateCanvas("StageMenuCanvas", 20, transform);

            var pause = UiFactory.CreatePanel("PausePanel", canvas, new Vector2(620f, 520f));
            PausePanel = pause.gameObject;
            var pauseTitle = UiFactory.CreateText("Title", pause, "일시정지", 44, TextAnchor.MiddleCenter, 560f, 60f);
            pauseTitle.rectTransform.anchoredPosition = new Vector2(0f, 200f);
            var pauseColumn = UiFactory.CreateColumn("Menu", pause);
            pauseColumn.anchoredPosition = new Vector2(0f, 90f);
            ResumeButton = UiFactory.CreateButton("Resume", pauseColumn, "계속", Resume);
            PauseRetryButton = UiFactory.CreateButton("Retry", pauseColumn, "다시 하기", Retry);
            PauseStageSelectButton = UiFactory.CreateButton("StageSelect", pauseColumn, "스테이지 선택", () => Leave(false));
            PauseSettingsButton = UiFactory.CreateButton("Settings", pauseColumn, "설정", () => Settings.Open(PauseSettingsButton));
            PausePanel.SetActive(false);

            var result = UiFactory.CreatePanel("ResultPanel", canvas, new Vector2(900f, 760f));
            ResultPanel = result.gameObject;
            ResultTitle = UiFactory.CreateText("Title", result, string.Empty, 56, TextAnchor.MiddleCenter, 800f, 80f);
            ResultTitle.rectTransform.anchoredPosition = new Vector2(0f, 300f);
            ResultBody = UiFactory.CreateText("Body", result, string.Empty, 28, TextAnchor.UpperCenter, 800f, 300f);
            ResultBody.rectTransform.anchoredPosition = new Vector2(0f, 90f);
            var resultColumn = UiFactory.CreateColumn("Menu", result, 10f);
            resultColumn.anchoredPosition = new Vector2(0f, -100f);
            EndingButton = UiFactory.CreateButton("Ending", resultColumn, "엔딩 보기", () => Leave(Flow.OpenEnding));
            RetryButton = UiFactory.CreateButton("Retry", resultColumn, "다시 하기", Retry);
            SkillsButton = UiFactory.CreateButton("Skills", resultColumn, "스킬", () => Leave(true));
            StageSelectButton = UiFactory.CreateButton("StageSelect", resultColumn, "스테이지 선택", () => Leave(false));
            ResultPanel.SetActive(false);

            Settings = SettingsPanel.Create(canvas, Flow.Session.Settings, ApplyLookPreferences);
        }

        private void Start()
        {
            ApplyLookPreferences();
        }

        private void ApplyLookPreferences()
        {
            if (_runner != null)
            {
                _runner.ApplyLookPreferences(Flow.Session.Settings.MouseSensitivity, Flow.Session.Settings.InvertY);
            }
        }

        private void Update()
        {
            if (_runner == null || !_runner.IsRunning || Outcome != null)
            {
                return;
            }

            Subscribe(_runner.Driver.Simulation);
            if (PausePressed())
            {
                if (IsPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }

            if (_runner.Driver.Simulation.Outcome != StageOutcome.InProgress)
            {
                _endedSeconds += Time.unscaledDeltaTime;
                if (_endedSeconds >= _runner.Tuning.GetFloat("hud.resultDelay"))
                {
                    ShowResult();
                }
            }
        }

        private bool PausePressed()
        {
            if (_pause == null && _controls != null)
            {
                _pause = _controls.FindAction("Gameplay/Pause", throwIfNotFound: true);
            }

            return _pause != null && _pause.WasPressedThisFrame();
        }

        /// <summary>틱마다 사망 원인과 클리어 결과를 받아 둔다 (이벤트는 그 틱에만 있다).</summary>
        private void Subscribe(GameSimulation simulation)
        {
            if (_subscribed == simulation)
            {
                return;
            }

            _subscribed = simulation;
            _runner.Driver.TickCompleted += OnTick;
        }

        private void OnTick(GameSimulation simulation)
        {
            var died = simulation.Events.OfType<PlayerDied>().FirstOrDefault();
            if (died != null)
            {
                _deathCause = died.Cause;
            }

            var cleared = simulation.Events.OfType<StageCleared>().FirstOrDefault();
            if (cleared != null)
            {
                _clearResult = cleared.Result;
            }
        }

        private void Retry()
        {
            Time.timeScale = 1f;
            Flow.Session.SelectedLevelId ??= _stage.Level.Id;
            Flow.Retry();
        }

        private void Leave(bool openSkills)
        {
            Time.timeScale = 1f;
            Flow.OpenStageSelect(openSkills);
        }

        private void Leave(System.Action navigate)
        {
            Time.timeScale = 1f;
            navigate();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private static string ClearText(StageOutcomeInfo info)
        {
            var reward = info.Reward;
            return $"시간 {info.Seconds:0.0}초\n"
                + $"클리어 +{reward.Clear}\n"
                + $"광분 0회 +{reward.NoFrenzy}\n"
                + $"신중한 흡혈 +{reward.CarefulBite}\n"
                + $"기준 시간 이내 +{reward.ParTime}\n"
                + $"합계 +{reward.Total} 혈액 포인트";
        }

        public static string CauseText(DeathCause? cause)
        {
            switch (cause)
            {
                case DeathCause.Attack:
                    return "인간에게 맞았다";
                case DeathCause.WaterImpact:
                    return "물방울에 갇힌 채 떨어졌다";
                case DeathCause.Web:
                    return "거미줄에 걸렸다";
                case DeathCause.Spray:
                    return "모기약에 중독됐다";
                default:
                    return "알 수 없음";
            }
        }
    }
}
