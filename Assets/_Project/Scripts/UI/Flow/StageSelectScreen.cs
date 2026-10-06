using System.Collections.Generic;
using System.Linq;
using Moqui.Core.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// Stage Select (spec/08): 스테이지 목록(data/stages.json)의 장 단위 페이지. 목록에서 바로 앞 스테이지를 클리어해야 다음이 열린다.
    /// 클리어 기록(최고 시간, 광분 0회, 최소 자국)을 보여 주고 Skills로 갈 수 있다. 처음에는 진행 중인 장을 연다 (D-061).
    /// </summary>
    public sealed class StageSelectScreen : ScreenBase
    {
        private readonly List<Button> _stageButtons = new List<Button>();
        private readonly List<Text> _recordTexts = new List<Text>();
        private readonly List<string> _slotLevels = new List<string>();
        private int _chapter;

        /// <summary>지금 페이지(장)의 스테이지 버튼. 장의 스테이지 수보다 많은 칸은 숨긴다.</summary>
        public IReadOnlyList<Button> StageButtons => _stageButtons;

        public IReadOnlyList<Text> RecordTexts => _recordTexts;

        public int ChapterIndex => _chapter;

        public Text ChapterText { get; private set; }

        public Button PreviousChapterButton { get; private set; }

        public Button NextChapterButton { get; private set; }

        public Button SkillsButton { get; private set; }

        public Button BackButton { get; private set; }

        public Text PointsText { get; private set; }

        public SkillsPanel Skills { get; private set; }

        private StageCatalog Catalog => Flow.Session.Catalog;

        /// <summary>그 장의 페이지를 연다.</summary>
        public void ShowChapter(int index)
        {
            _chapter = Mathf.Clamp(index, 0, Catalog.Chapters.Count - 1);
            Refresh();
        }

        /// <summary>그 스테이지가 있는 장을 열고 그 버튼을 돌려준다 (목록에 없으면 null).</summary>
        public Button ButtonFor(string levelId)
        {
            var chapter = Catalog.ChapterOf(levelId);
            if (chapter == null)
            {
                return null;
            }

            ShowChapter(IndexOf(chapter));
            int slot = _slotLevels.IndexOf(levelId);
            return slot >= 0 ? _stageButtons[slot] : null;
        }

        public void Refresh()
        {
            var save = Flow.Session.Save;
            var chapter = Catalog.Chapters[_chapter];
            PointsText.text = $"혈액 포인트  {save.BloodPoints}";
            ChapterText.text = $"{chapter.Title}  ({_chapter + 1}/{Catalog.Chapters.Count})";
            PreviousChapterButton.interactable = _chapter > 0;
            NextChapterButton.interactable = _chapter < Catalog.Chapters.Count - 1;
            _slotLevels.Clear();
            for (int i = 0; i < _stageButtons.Count; i++)
            {
                bool used = i < chapter.Stages.Count;
                _stageButtons[i].gameObject.SetActive(used);
                _recordTexts[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                var stage = chapter.Stages[i];
                _slotLevels.Add(stage.LevelId);
                bool unlocked = Flow.CanStart(stage.LevelId);
                _stageButtons[i].interactable = unlocked;
                UiFactory.LabelOf(_stageButtons[i]).text = $"Stage {Catalog.Number(stage.LevelId)}  {stage.Title}{(unlocked ? string.Empty : "  (잠김)")}";
                _recordTexts[i].text = RecordLine(save.Record(stage.LevelId));
            }
        }

        protected override void Build()
        {
            var canvas = UiFactory.CreateCanvas("StageSelectCanvas", 0, transform);
            var title = UiFactory.CreateText("Title", canvas, "스테이지 선택", 52, TextAnchor.MiddleCenter, 1200f, 80f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 430f);
            PointsText = UiFactory.CreateText("Points", canvas, string.Empty, 28, TextAnchor.MiddleCenter, 600f, 44f);
            PointsText.rectTransform.anchoredPosition = new Vector2(0f, 370f);

            var chapterRow = UiFactory.CreateRow("Chapter", canvas, 20f);
            chapterRow.anchoredPosition = new Vector2(0f, 315f);
            PreviousChapterButton = UiFactory.CreateButton("PreviousChapter", chapterRow, "◀", () => ShowChapter(_chapter - 1), 80f);
            ChapterText = UiFactory.CreateText("ChapterTitle", chapterRow, string.Empty, 30, TextAnchor.MiddleCenter, 560f, 44f);
            NextChapterButton = UiFactory.CreateButton("NextChapter", chapterRow, "▶", () => ShowChapter(_chapter + 1), 80f);

            var column = UiFactory.CreateColumn("Stages", canvas, 6f);
            column.anchoredPosition = new Vector2(0f, 250f);
            int slots = Catalog.Chapters.Max(chapter => chapter.Stages.Count);
            for (int i = 0; i < slots; i++)
            {
                int slot = i;
                _stageButtons.Add(UiFactory.CreateButton($"Stage{slot + 1}", column, string.Empty, () => Flow.StartStage(_slotLevels[slot]), 760f));
                _recordTexts.Add(UiFactory.CreateText($"Record{slot + 1}", column, string.Empty, 22, TextAnchor.MiddleCenter, 760f, 30f));
            }

            var row = UiFactory.CreateRow("Bottom", canvas, 30f);
            row.anchoredPosition = new Vector2(0f, -420f);
            SkillsButton = UiFactory.CreateButton("Skills", row, "스킬", () => Skills.Open(SkillsButton), 300f);
            BackButton = UiFactory.CreateButton("Back", row, "타이틀로", () => Flow.OpenTitle(), 300f);

            Skills = SkillsPanel.Create(canvas, Flow, Refresh);
            _chapter = IndexOf(Catalog.ChapterOf(ProgressLevel()));
            Refresh();
            UiFactory.Focus(_stageButtons[Mathf.Max(0, _slotLevels.IndexOf(ProgressLevel()))]);
            if (Flow.Session.OpenSkillsOnStageSelect)
            {
                Flow.Session.OpenSkillsOnStageSelect = false;
                Skills.Open(SkillsButton);
            }
        }

        /// <summary>진행 중인 스테이지: 아직 클리어하지 않은 첫 스테이지 (모두 클리어했으면 마지막).</summary>
        private string ProgressLevel()
        {
            var save = Flow.Session.Save;
            var next = Catalog.Stages.FirstOrDefault(stage => !(save.Record(stage.LevelId)?.Cleared ?? false));
            return (next ?? Catalog.Stages[Catalog.Stages.Count - 1]).LevelId;
        }

        private int IndexOf(ChapterDefinition chapter)
        {
            for (int i = 0; i < Catalog.Chapters.Count; i++)
            {
                if (Catalog.Chapters[i] == chapter)
                {
                    return i;
                }
            }

            return 0;
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
