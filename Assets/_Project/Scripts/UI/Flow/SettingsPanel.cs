using System;
using System.Collections.Generic;
using Moqui.Unity.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// 설정 (spec/08 §설정): 마우스 감도 0.25~4배, Y축 반전, 튜토리얼 안내, 기본 시점, 마스터/효과음/음악 볼륨,
    /// 전체화면/창모드, 해상도. 값은 바뀔 때마다 PlayerPrefs에 저장하고 엔진에 반영한다.
    /// 슬라이더 대신 −/+ 버튼과 토글 버튼을 써서 게임패드로도 같은 방식으로 조작한다.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        private const float SensitivityStep = 0.25f;
        private const float VolumeStep = 0.1f;
        private const float RowLabelWidth = 520f;
        private const float StepButtonWidth = 90f;

        private readonly List<Action> _refreshers = new List<Action>();
        private UserSettings _settings;
        private Action _onChanged;
        private Selectable _returnFocus;
        private Button _first;

        public Button CloseButton { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>이름 → 라벨 (테스트·캡처 확인용).</summary>
        public Dictionary<string, Text> Labels { get; } = new Dictionary<string, Text>();

        /// <summary>이름 → (감소, 증가) 또는 (토글, null) 버튼.</summary>
        public Dictionary<string, (Button Primary, Button Secondary)> Controls { get; } = new Dictionary<string, (Button, Button)>();

        public static SettingsPanel Create(Transform canvas, UserSettings settings, Action onChanged)
        {
            var panel = UiFactory.CreatePanel("SettingsPanel", canvas, new Vector2(1000f, 860f));
            var component = panel.gameObject.AddComponent<SettingsPanel>();
            component._settings = settings;
            component._onChanged = onChanged;
            component.Build(panel);
            panel.gameObject.SetActive(false);
            return component;
        }

        public void Open(Selectable returnFocus)
        {
            _returnFocus = returnFocus;
            Refresh();
            gameObject.SetActive(true);
            UiFactory.Focus(_first);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            UiFactory.Focus(_returnFocus);
        }

        private void Build(RectTransform panel)
        {
            var title = UiFactory.CreateText("Title", panel, "설정", 40, TextAnchor.MiddleCenter, 900f, 60f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 380f);
            var column = UiFactory.CreateColumn("Rows", panel, 8f);
            column.anchoredPosition = new Vector2(0f, 320f);

            Stepper(column, "sensitivity", () => $"마우스 감도  {_settings.MouseSensitivity:0.00}배", () => _settings.MouseSensitivity -= SensitivityStep, () => _settings.MouseSensitivity += SensitivityStep);
            Toggle(column, "invertY", () => $"Y축 반전  {OnOff(_settings.InvertY)}", () => _settings.InvertY = !_settings.InvertY);
            Toggle(column, "tutorialHints", () => $"튜토리얼 안내  {OnOff(_settings.TutorialHints)}", () => _settings.TutorialHints = !_settings.TutorialHints);
            Toggle(column, "defaultView", () => $"기본 시점  {(_settings.DefaultView == CameraViewMode.FirstPerson ? "1인칭" : "3인칭")}", () =>
                _settings.DefaultView = _settings.DefaultView == CameraViewMode.FirstPerson ? CameraViewMode.ThirdPerson : CameraViewMode.FirstPerson);
            Stepper(column, "masterVolume", () => $"마스터 볼륨  {Percent(_settings.MasterVolume)}", () => _settings.MasterVolume -= VolumeStep, () => _settings.MasterVolume += VolumeStep);
            Stepper(column, "sfxVolume", () => $"효과음 볼륨  {Percent(_settings.SfxVolume)}", () => _settings.SfxVolume -= VolumeStep, () => _settings.SfxVolume += VolumeStep);
            Stepper(column, "musicVolume", () => $"음악 볼륨  {Percent(_settings.MusicVolume)}", () => _settings.MusicVolume -= VolumeStep, () => _settings.MusicVolume += VolumeStep);
            Toggle(column, "fullscreen", () => $"화면  {(_settings.Fullscreen ? "전체화면" : "창모드")}", () => _settings.Fullscreen = !_settings.Fullscreen);
            Stepper(column, "resolution", () => $"해상도  {_settings.Resolution.x}×{_settings.Resolution.y}", () => _settings.ResolutionIndex -= 1, () => _settings.ResolutionIndex += 1);

            CloseButton = UiFactory.CreateButton("Close", panel, "닫기", Close, 300f);
            ((RectTransform)CloseButton.transform).anchoredPosition = new Vector2(0f, -370f);
        }

        private void Stepper(Transform parent, string name, Func<string> label, Action decrease, Action increase)
        {
            var row = UiFactory.CreateRow(name, parent);
            var text = UiFactory.CreateText("Label", row, string.Empty, 26, TextAnchor.MiddleLeft, RowLabelWidth, UiFactory.ButtonHeight);
            var minus = UiFactory.CreateButton("Decrease", row, "−", () => Change(decrease), StepButtonWidth);
            var plus = UiFactory.CreateButton("Increase", row, "+", () => Change(increase), StepButtonWidth);
            Register(name, text, label, minus, plus);
        }

        private void Toggle(Transform parent, string name, Func<string> label, Action toggle)
        {
            var row = UiFactory.CreateRow(name, parent);
            var text = UiFactory.CreateText("Label", row, string.Empty, 26, TextAnchor.MiddleLeft, RowLabelWidth, UiFactory.ButtonHeight);
            var button = UiFactory.CreateButton("Toggle", row, "바꾸기", () => Change(toggle), (StepButtonWidth * 2f) + 10f);
            Register(name, text, label, button, null);
        }

        private void Register(string name, Text text, Func<string> label, Button primary, Button secondary)
        {
            Labels[name] = text;
            Controls[name] = (primary, secondary);
            _refreshers.Add(() => text.text = label());
            _first = _first != null ? _first : primary;
            text.text = label();
        }

        private void Change(Action change)
        {
            change();
            _settings.ApplyToEngine();
            _onChanged?.Invoke();
            Refresh();
        }

        private void Refresh()
        {
            foreach (var refresh in _refreshers)
            {
                refresh();
            }
        }

        private static string OnOff(bool value) => value ? "켜기" : "끄기";

        private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
