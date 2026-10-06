using System;
using System.Collections.Generic;
using Moqui.Core.Data;
using Moqui.Unity.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// 플레이 검증 조정 패널 (F10, plan/gulf-improvements.md §11, D-065). 체감 검증 키를 그룹별로 한 칸씩 고치고,
    /// "저장하고 다시 시작"으로 playtest.json에 저장·기록한 뒤 스테이지를 새 값으로 다시 연다. 정본 확정은 에디터 메뉴에서 한다.
    /// </summary>
    public sealed class PlaytestPanel : MonoBehaviour
    {
        private const float StepButtonWidth = 80f;
        private const float ResetButtonWidth = 110f;
        private const float LabelWidth = 1000f;
        private const float FooterButtonWidth = 200f;

        private readonly List<List<GameObject>> _groupRows = new List<List<GameObject>>();
        private readonly List<Action> _refreshers = new List<Action>();
        private PlaytestStore _store;
        private Func<string> _levelId;
        private Action _restart;
        private Action _closed;
        private Text _groupTitle;
        private Text _status;
        private Button _first;

        public PlaytestSession Session { get; private set; }

        public int GroupIndex { get; private set; }

        /// <summary>키 → (라벨, 감소, 증가, 기본) (테스트용).</summary>
        public Dictionary<string, (Text Label, Button Decrease, Button Increase, Button Reset)> Rows { get; } = new Dictionary<string, (Text, Button, Button, Button)>();

        public Button SaveButton { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        public static PlaytestPanel Create(Transform canvas, PlaytestSession session, PlaytestStore store, Func<string> levelId, Action restart, Action closed)
        {
            var panel = UiFactory.CreatePanel("PlaytestPanel", canvas, new Vector2(1500f, 980f));
            var component = panel.gameObject.AddComponent<PlaytestPanel>();
            component.Session = session;
            component._store = store;
            component._levelId = levelId;
            component._restart = restart;
            component._closed = closed;
            component.Build(panel);
            panel.gameObject.SetActive(false);
            return component;
        }

        public void Open()
        {
            Refresh();
            gameObject.SetActive(true);
            UiFactory.Focus(_first);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            _closed?.Invoke();
        }

        public void ShowGroup(int index)
        {
            int count = Session.Catalog.Groups.Count;
            GroupIndex = ((index % count) + count) % count;
            for (int i = 0; i < _groupRows.Count; i++)
            {
                foreach (var row in _groupRows[i])
                {
                    row.SetActive(i == GroupIndex);
                }
            }

            Refresh();
        }

        /// <summary>저장하고 기록한 뒤 다시 시작한다.</summary>
        public void SaveAndRestart()
        {
            _store.Save(Session.Working);
            _store.AppendLog(Session.Commit(DateTime.UtcNow, _levelId()));
            _restart?.Invoke();
        }

        private void Build(RectTransform panel)
        {
            var title = UiFactory.CreateText("Title", panel, "플레이 검증 조정 (F10)", 36, TextAnchor.MiddleCenter, 1400f, 50f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 440f);

            var tabs = UiFactory.CreateRow("Tabs", panel, 10f, UiFactory.ButtonHeight, 1400f);
            tabs.anchoredPosition = new Vector2(0f, 380f);
            _first = UiFactory.CreateButton("PreviousGroup", tabs, "◀", () => ShowGroup(GroupIndex - 1), StepButtonWidth);
            _groupTitle = UiFactory.CreateText("Group", tabs, string.Empty, 30, TextAnchor.MiddleCenter, 600f, UiFactory.ButtonHeight);
            UiFactory.CreateButton("NextGroup", tabs, "▶", () => ShowGroup(GroupIndex + 1), StepButtonWidth);

            var column = UiFactory.CreateColumn("Rows", panel, 6f);
            column.anchoredPosition = new Vector2(0f, 320f);
            foreach (var group in Session.Catalog.Groups)
            {
                var rows = new List<GameObject>();
                foreach (var key in group.Keys)
                {
                    rows.Add(BuildRow(column, key));
                }

                _groupRows.Add(rows);
            }

            var footer = UiFactory.CreateRow("Footer", panel, 8f, UiFactory.ButtonHeight, 1450f);
            footer.anchoredPosition = new Vector2(0f, -360f);
            SaveButton = UiFactory.CreateButton("SaveRestart", footer, "저장하고 다시 시작", SaveAndRestart, 300f);
            UiFactory.CreateButton("ResetAll", footer, "모두 기본값", () => Change(Session.ResetAll), FooterButtonWidth);
            foreach (string slot in PlaytestStore.PresetSlots)
            {
                UiFactory.CreateButton($"SavePreset{slot}", footer, $"{slot}에 저장", () => SavePreset(slot), FooterButtonWidth * 0.75f);
                UiFactory.CreateButton($"LoadPreset{slot}", footer, $"{slot} 불러오기", () => LoadPreset(slot), FooterButtonWidth * 0.85f);
            }

            UiFactory.CreateButton("Close", footer, "닫기", Close, 140f);

            _status = UiFactory.CreateText("Status", panel, string.Empty, 20, TextAnchor.UpperLeft, 1400f, 70f);
            _status.rectTransform.anchoredPosition = new Vector2(0f, -440f);
            ShowGroup(0);
        }

        private GameObject BuildRow(Transform parent, PlaytestKey key)
        {
            var row = UiFactory.CreateRow(key.Key, parent, 8f, 48f, 1400f);
            var label = UiFactory.CreateText("Label", row, string.Empty, 22, TextAnchor.MiddleLeft, LabelWidth, 48f);
            var minus = UiFactory.CreateButton("Decrease", row, "−", () => Change(() => Session.Step(key, -1)), StepButtonWidth, 48f);
            var plus = UiFactory.CreateButton("Increase", row, "+", () => Change(() => Session.Step(key, +1)), StepButtonWidth, 48f);
            var reset = UiFactory.CreateButton("Reset", row, "기본", () => Change(() => Session.Reset(key.Key)), ResetButtonWidth, 48f);
            Rows[key.Key] = (label, minus, plus, reset);
            _refreshers.Add(() => label.text = RowText(key));
            return row.gameObject;
        }

        private string RowText(PlaytestKey key)
        {
            string value = PlaytestOverrides.Format(Session.Value(key.Key));
            string mark = Session.IsOverridden(key.Key) ? "  ●" : string.Empty;
            return $"{key.Key}   {value}   (기본 {PlaytestOverrides.Format(Session.BaseValue(key.Key))}, 범위 {key.Min}~{key.Max}){mark}";
        }

        private void SavePreset(string slot)
        {
            _store.SavePreset(slot, Session.Working);
            Refresh($"프리셋 {slot} 저장");
        }

        private void LoadPreset(string slot)
        {
            if (!_store.HasPreset(slot))
            {
                Refresh($"프리셋 {slot} 없음");
                return;
            }

            Session.LoadWorking(_store.LoadPreset(slot));
            Refresh($"프리셋 {slot} 불러옴 (저장하고 다시 시작하면 적용)");
        }

        private void Change(Action change)
        {
            change();
            Refresh();
        }

        private void Refresh(string notice = null)
        {
            _groupTitle.text = $"{Session.Catalog.Groups[GroupIndex].Title}  ({GroupIndex + 1}/{Session.Catalog.Groups.Count})";
            foreach (var refresh in _refreshers)
            {
                refresh();
            }

            string unsaved = Session.HasUnsavedChanges ? "저장 안 한 변경 있음" : "저장됨";
            string error = _store.LastError != null ? $"  파일 오류: {_store.LastError}" : string.Empty;
            _status.text = $"덮어쓴 키 {Session.Working.Count}개 · {unsaved}{(notice != null ? " · " + notice : string.Empty)}{error}\n파일: {_store.OverridesPath}  (기록: {PlaytestLog.FileName}, 확정: 에디터 Moqui/Playtest/정본으로 올리기)";
        }
    }
}
