using System;
using System.Collections.Generic;
using Moqui.Core.Meta;
using Moqui.Unity.UI.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// Skills (spec/08, spec/09): 저항/능력치/연속 회피/마법봉/기타 탭, 현재 레벨과 다음 레벨 비용, 구매, 액티브 장착.
    /// 구매·장착은 즉시 저장한다.
    /// </summary>
    public sealed class SkillsPanel : MonoBehaviour
    {
        private const float RowHeight = 50f;

        private static readonly (SkillCategory Category, string Label)[] Tabs =
        {
            (SkillCategory.Resist, "저항"),
            (SkillCategory.Stats, "능력치"),
            (SkillCategory.Chain, "연속 회피"),
            (SkillCategory.Wand, "마법봉"),
            (SkillCategory.Misc, "기타"),
        };

        private static readonly Dictionary<string, string> Effects = new Dictionary<string, string>
        {
            [SkillCatalog.ResistSpray] = "중독 증가·모기향 하한 −20%/레벨",
            [SkillCatalog.ResistWet] = "젖은 날개 −25%, 습기 −20%/레벨. 3레벨 탈출 입력 −1",
            [SkillCatalog.ResistSatiety] = "포만 감속 폭 −25%/레벨",
            [SkillCatalog.SilentWings] = "모든 소음 반경 −12%/레벨",
            [SkillCatalog.SwiftWings] = "비행 속도 +8%/레벨",
            [SkillCatalog.VortexControl] = "가감속 시간 −20%/레벨. 3레벨 8방향 대시",
            [SkillCatalog.Stamina] = "최대 스태미나 +15, 회복 +10%/레벨",
            [SkillCatalog.FeatherLanding] = "착지 반응 확률 −30%/레벨",
            [SkillCatalog.NumbingSaliva] = "흡혈 중 가려움 −15%/레벨",
            [SkillCatalog.ShadowBlend] = "부착 시 시각 −0.05, 광분 진정 −1초/레벨",
            [SkillCatalog.ChainVortex] = "1: 대시 직후 추가 대시 1회. 2: 그 대시 스태미나 없음",
            [SkillCatalog.MagicWand] = "최대 흡혈 속도 +15%, 가속 −1초/레벨",
            [SkillCatalog.CompoundEyes] = "선명 시야 +40u, 체온 감지 +20u/레벨",
            [SkillCatalog.DecoyCharm] = "액티브: 조준 지점에 3초 날갯소리 미끼. 2레벨 쿨타임 감소",
        };

        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly Dictionary<string, SkillRow> _rows = new Dictionary<string, SkillRow>();
        private ScreenFlow _flow;
        private Action _onChanged;
        private Selectable _returnFocus;
        private Text _points;
        private Text _message;

        public SkillCategory CurrentTab { get; private set; }

        public Button CloseButton { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        public IReadOnlyDictionary<string, SkillRow> Rows => _rows;

        public IReadOnlyList<Button> TabButtons => _tabButtons;

        public Text Message => _message;

        public sealed class SkillRow
        {
            public GameObject Root { get; set; }

            public Text Label { get; set; }

            public Button BuyButton { get; set; }

            public Button EquipButton { get; set; }
        }

        public static SkillsPanel Create(Transform canvas, ScreenFlow flow, Action onChanged)
        {
            var panel = UiFactory.CreatePanel("SkillsPanel", canvas, HudView.ReferenceResolution);
            var component = panel.gameObject.AddComponent<SkillsPanel>();
            component._flow = flow;
            component._onChanged = onChanged;
            component.Build(panel);
            panel.gameObject.SetActive(false);
            return component;
        }

        public void Open(Selectable returnFocus)
        {
            _returnFocus = returnFocus;
            gameObject.SetActive(true);
            ShowTab(CurrentTab);
            UiFactory.Focus(_tabButtons[(int)CurrentTab]);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            UiFactory.Focus(_returnFocus);
        }

        public void ShowTab(SkillCategory category)
        {
            CurrentTab = category;
            foreach (var skill in SkillCatalog.All)
            {
                _rows[skill.Id].Root.SetActive(skill.Category == category);
            }

            Refresh();
        }

        public void Buy(string skillId)
        {
            var result = _flow.Purchase(skillId);
            _message.text = result switch
            {
                PurchaseResult.Purchased => $"{SkillCatalog.Get(skillId).Name} 레벨 {_flow.Session.Save.SkillLevel(skillId)}",
                PurchaseResult.NotEnoughPoints => "혈액 포인트가 부족하다",
                _ => "이미 최대 레벨이다",
            };
            Changed();
        }

        public void Equip(string skillId)
        {
            bool equipped = _flow.Session.Save.EquippedActive == skillId ? _flow.Equip(null) : _flow.Equip(skillId);
            _message.text = equipped ? (_flow.Session.Save.EquippedActive == skillId ? "장착했다" : "장착을 풀었다") : "먼저 구매해야 한다";
            Changed();
        }

        private void Changed()
        {
            Refresh();
            _onChanged?.Invoke();
        }

        private void Build(RectTransform panel)
        {
            var title = UiFactory.CreateText("Title", panel, "스킬", 44, TextAnchor.MiddleCenter, 600f, 60f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 470f);
            _points = UiFactory.CreateText("Points", panel, string.Empty, 28, TextAnchor.MiddleCenter, 600f, 40f);
            _points.rectTransform.anchoredPosition = new Vector2(0f, 420f);

            var tabs = UiFactory.CreateRow("Tabs", panel, 12f);
            tabs.anchoredPosition = new Vector2(0f, 350f);
            foreach (var (category, label) in Tabs)
            {
                _tabButtons.Add(UiFactory.CreateButton($"Tab{category}", tabs, label, () => ShowTab(category), 240f));
            }

            var list = UiFactory.CreateColumn("List", panel, 10f);
            list.anchoredPosition = new Vector2(0f, 290f);
            foreach (var skill in SkillCatalog.All)
            {
                var row = UiFactory.CreateRow(skill.Id, list, 14f, RowHeight);
                var label = UiFactory.CreateText("Label", row, string.Empty, 22, TextAnchor.MiddleLeft, 960f, RowHeight);
                string id = skill.Id;
                var buy = UiFactory.CreateButton("Buy", row, string.Empty, () => Buy(id), 200f, RowHeight);
                Button equip = skill.IsActive ? UiFactory.CreateButton("Equip", row, "장착", () => Equip(id), 140f, RowHeight) : null;
                _rows[skill.Id] = new SkillRow { Root = row.gameObject, Label = label, BuyButton = buy, EquipButton = equip };
            }

            _message = UiFactory.CreateText("Message", panel, string.Empty, 24, TextAnchor.MiddleCenter, 900f, 36f);
            _message.rectTransform.anchoredPosition = new Vector2(0f, -380f);
            CloseButton = UiFactory.CreateButton("Close", panel, "닫기", Close, 300f);
            ((RectTransform)CloseButton.transform).anchoredPosition = new Vector2(0f, -450f);
            ShowTab(SkillCategory.Resist);
        }

        private void Refresh()
        {
            var save = _flow.Session.Save;
            _points.text = $"혈액 포인트  {save.BloodPoints}";
            foreach (var skill in SkillCatalog.All)
            {
                var row = _rows[skill.Id];
                int level = save.SkillLevel(skill.Id);
                row.Label.text = $"{skill.Name}  Lv {level}/{skill.MaxLevel}   <size=19>{Effects[skill.Id]}</size>";
                int? cost = _flow.Session.Shop.NextCost(save, skill.Id);
                UiFactory.LabelOf(row.BuyButton).text = cost.HasValue ? $"구매 {cost.Value}" : "최대";
                row.BuyButton.interactable = cost.HasValue && save.BloodPoints >= cost.Value;
                if (row.EquipButton != null)
                {
                    row.EquipButton.interactable = level > 0;
                    UiFactory.LabelOf(row.EquipButton).text = save.EquippedActive == skill.Id ? "해제" : "장착";
                }
            }

            int index = 0;
            foreach (var tab in _tabButtons)
            {
                UiFactory.LabelOf(tab).fontStyle = index++ == (int)CurrentTab ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }
}
