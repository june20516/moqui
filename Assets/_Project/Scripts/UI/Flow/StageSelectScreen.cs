using System.Collections.Generic;
using Moqui.Core.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// Stage Select (spec/08): 이전 스테이지를 클리어해야 다음이 열린다. 클리어 기록(최고 시간, 광분 0회, 최소 자국)을 보여 주고
    /// Skills로 갈 수 있다. 레벨 데이터가 아직 없는 스테이지는 "준비 중"으로 잠근다.
    /// </summary>
    public sealed class StageSelectScreen : ScreenBase
    {
        private static readonly string[] StageTitles =
        {
            "거실 · TV 보다 조는 인간",
            "거실 · TV 보는 인간",
            "열대야 침실 · 누워 휴대폰 보는 인간",
            "화장실 · 볼일 보는 인간",
            "베란다 술자리 · 맥주 마시는 인간",
        };

        private readonly List<Button> _stageButtons = new List<Button>();
        private readonly List<Text> _recordTexts = new List<Text>();

        public IReadOnlyList<Button> StageButtons => _stageButtons;

        public IReadOnlyList<Text> RecordTexts => _recordTexts;

        public Button SkillsButton { get; private set; }

        public Button BackButton { get; private set; }

        public Text PointsText { get; private set; }

        public SkillsPanel Skills { get; private set; }

        public void Refresh()
        {
            var save = Flow.Session.Save;
            PointsText.text = $"혈액 포인트  {save.BloodPoints}";
            for (int i = 0; i < _stageButtons.Count; i++)
            {
                int number = i + 1;
                bool available = Flow.Session.LevelAvailable(number);
                bool unlocked = save.IsUnlocked(number);
                _stageButtons[i].interactable = Flow.CanStart(number);
                string state = !available ? "  (준비 중)" : !unlocked ? "  (잠김)" : string.Empty;
                UiFactory.LabelOf(_stageButtons[i]).text = $"Stage {number}  {StageTitles[i]}{state}";
                _recordTexts[i].text = RecordLine(save.Record(RewardCalculator.LevelId(number)));
            }
        }

        protected override void Build()
        {
            var canvas = UiFactory.CreateCanvas("StageSelectCanvas", 0, transform);
            var title = UiFactory.CreateText("Title", canvas, "스테이지 선택", 52, TextAnchor.MiddleCenter, 1200f, 80f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 430f);
            PointsText = UiFactory.CreateText("Points", canvas, string.Empty, 28, TextAnchor.MiddleCenter, 600f, 44f);
            PointsText.rectTransform.anchoredPosition = new Vector2(0f, 370f);

            var column = UiFactory.CreateColumn("Stages", canvas, 6f);
            column.anchoredPosition = new Vector2(0f, 300f);
            for (int i = 0; i < SaveData.StageCount; i++)
            {
                int number = i + 1;
                _stageButtons.Add(UiFactory.CreateButton($"Stage{number}", column, string.Empty, () => Flow.StartStage(number), 760f));
                _recordTexts.Add(UiFactory.CreateText($"Record{number}", column, string.Empty, 22, TextAnchor.MiddleCenter, 760f, 30f));
            }

            var row = UiFactory.CreateRow("Bottom", canvas, 30f);
            row.anchoredPosition = new Vector2(0f, -420f);
            SkillsButton = UiFactory.CreateButton("Skills", row, "스킬", () => Skills.Open(SkillsButton), 300f);
            BackButton = UiFactory.CreateButton("Back", row, "타이틀로", () => Flow.OpenTitle(), 300f);

            Skills = SkillsPanel.Create(canvas, Flow, Refresh);
            Refresh();
            UiFactory.Focus(_stageButtons[0]);
            if (Flow.Session.OpenSkillsOnStageSelect)
            {
                Flow.Session.OpenSkillsOnStageSelect = false;
                Skills.Open(SkillsButton);
            }
        }

        private static string RecordLine(StageRecord record)
        {
            if (record == null || !record.Cleared)
            {
                return "기록 없음";
            }

            int minutes = (int)(record.BestSeconds / 60f);
            float seconds = record.BestSeconds - (minutes * 60f);
            return $"최고 {minutes}:{seconds:00.0}  ·  광분 0회 {(record.NoFrenzy ? "달성" : "-")}  ·  최소 자국 {record.MinBiteMarks}";
        }
    }
}
