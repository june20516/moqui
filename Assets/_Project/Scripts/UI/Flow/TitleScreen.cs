using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>Title (spec/08): 시작, 설정, 데이터 초기화(확인 대화상자), 종료.</summary>
    public sealed class TitleScreen : ScreenBase
    {
        public Button StartButton { get; private set; }

        public Button SettingsButton { get; private set; }

        public Button ResetButton { get; private set; }

        public Button QuitButton { get; private set; }

        public SettingsPanel Settings { get; private set; }

        public ConfirmDialog ResetConfirm { get; private set; }

        protected override void Build()
        {
            var canvas = UiFactory.CreateCanvas("TitleCanvas", 0, transform);
            var title = UiFactory.CreateText("Title", canvas, "Moqui", 72, TextAnchor.MiddleCenter, 1400f, 120f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 260f);

            var column = UiFactory.CreateColumn("Menu", canvas);
            column.anchoredPosition = new Vector2(0f, 70f);
            StartButton = UiFactory.CreateButton("Start", column, "시작", () => Flow.OpenStageSelect());
            SettingsButton = UiFactory.CreateButton("Settings", column, "설정", () => Settings.Open(SettingsButton));
            ResetButton = UiFactory.CreateButton("Reset", column, "데이터 초기화", () => ResetConfirm.Open(ResetButton));
            QuitButton = UiFactory.CreateButton("Quit", column, "종료", () => Flow.Quit());

            Settings = SettingsPanel.Create(canvas, Flow.Session.Settings, null);
            ResetConfirm = ConfirmDialog.Create(canvas, "저장 데이터(혈액 포인트, 스킬, 클리어 기록)를 모두 지울까요?", () => Flow.ResetData());
            UiFactory.Focus(StartButton);
        }
    }
}
