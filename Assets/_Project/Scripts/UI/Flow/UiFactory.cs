using System;
using Moqui.Unity.UI.Hud;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// 화이트박스 메뉴 UI를 코드로 만든다 (아트는 M10). 모든 화면은 마우스와 게임패드(방향키/스틱 + A/B)로 조작한다:
    /// EventSystem은 Input System UI 모듈의 기본 UI 액션을 쓰고, 버튼은 자동 내비게이션을 쓴다.
    /// </summary>
    public static class UiFactory
    {
        public const float ButtonWidth = 420f;
        public const float ButtonHeight = 56f;
        public const float RowWidth = 1400f;
        private static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.12f, 1f);
        private static readonly Color ButtonColor = new Color(0.22f, 0.2f, 0.32f, 1f);
        private static readonly Color ButtonHighlight = new Color(0.45f, 0.35f, 0.65f, 1f);
        private static readonly Color ButtonPressed = new Color(0.6f, 0.45f, 0.85f, 1f);
        private static readonly Color ButtonDisabled = new Color(0.18f, 0.18f, 0.2f, 0.6f);

        /// <summary>오버레이 캔버스 (기준 1920×1080, HUD와 같은 배율 규칙).</summary>
        public static RectTransform CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudView.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)root.transform;
        }

        /// <summary>씬에 EventSystem이 없으면 만든다 (Input System UI 모듈, 기본 UI 액션 = 마우스·키보드·게임패드).</summary>
        public static EventSystem EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                return existing;
            }

            var system = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return system.GetComponent<EventSystem>();
        }

        public static RectTransform CreatePanel(string name, Transform parent, Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = PanelColor;
            return rect;
        }

        /// <summary>
        /// 세로로 쌓는 목록. 위쪽 가장자리(pivot top)를 기준으로 놓아 항목 수가 늘어도 위치가 흔들리지 않게 한다.
        /// 자식은 고정 크기여야 한다 (레이아웃 그룹 안의 ContentSizeFitter는 한 번에 계산되지 않는다).
        /// </summary>
        public static RectTransform CreateColumn(string name, Transform parent, float spacing = 12f)
        {
            var column = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            column.transform.SetParent(parent, false);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var rect = (RectTransform)column.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(RowWidth, 0f);
            return rect;
        }

        /// <summary>가로로 놓는 고정 크기 행 (가운데 정렬).</summary>
        public static RectTransform CreateRow(string name, Transform parent, float spacing = 10f, float height = ButtonHeight, float width = RowWidth)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var rect = (RectTransform)row.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>이 이름의 버튼은 누를 때 취소음을 낸다 (뒤로·닫기·아니오).</summary>
        public static readonly string[] CancelButtonNames = { "Back", "Close", "No" };

        public static Button CreateButton(string name, Transform parent, string label, Action onClick, float width = ButtonWidth, float height = ButtonHeight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(width, height);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            go.GetComponent<Image>().color = Color.white;
            button.colors = new ColorBlock
            {
                normalColor = ButtonColor,
                highlightedColor = ButtonHighlight,
                selectedColor = ButtonHighlight,
                pressedColor = ButtonPressed,
                disabledColor = ButtonDisabled,
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            var text = CreateText("Label", go.transform, label, 26, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            // 버튼 소리 (spec/10 sfx_ui_*): 선택 시 select, 누르면 confirm, 뒤로 가기 버튼은 cancel.
            go.AddComponent<UiSelectSound>();
            string clickSound = Array.IndexOf(CancelButtonNames, name) >= 0 ? Presentation.Audio.AudioIds.UiCancel : Presentation.Audio.AudioIds.UiConfirm;
            button.onClick.AddListener(() => Presentation.Audio.AudioOutput.Ensure()?.PlayOneShot(clickSound));
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            return button;
        }

        public static Text CreateText(string name, Transform parent, string content, int size, TextAnchor alignment, float width = 0f, float height = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiFonts.Default;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (width > 0f)
            {
                text.rectTransform.sizeDelta = new Vector2(width, height > 0f ? height : size * 1.6f);
            }

            return text;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>버튼 라벨 Text.</summary>
        public static Text LabelOf(Button button)
        {
            return button.GetComponentInChildren<Text>();
        }

        /// <summary>첫 선택 요소에 포커스를 둔다 (spec/08: 게임패드 조작).</summary>
        public static void Focus(Selectable selectable)
        {
            var system = EnsureEventSystem();
            if (selectable != null)
            {
                // 화면이 여는 첫 포커스는 플레이어의 선택이 아니므로 선택음을 내지 않는다.
                UiSelectSound.Suppressed = true;
                try
                {
                    system.SetSelectedGameObject(selectable.gameObject);
                }
                finally
                {
                    UiSelectSound.Suppressed = false;
                }
            }
        }
    }
}
