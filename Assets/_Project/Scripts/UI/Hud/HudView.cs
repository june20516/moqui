using System.Collections.Generic;
using Moqui.Core.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>
    /// HUD 그림 (spec/08 HUD 표). uGUI 계층을 코드로 만들고 HudState를 반영한다.
    /// 기준 해상도 1920×1080, 너비·높이 절반씩 맞춤으로 크기를 바꿔 해상도마다 같은 배치를 유지한다.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        public const int MaxBiteDots = 12;

        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Color SafeColor = new Color(0.35f, 0.85f, 0.4f);
        private static readonly Color SuspiciousColor = new Color(1f, 0.82f, 0.2f);
        private static readonly Color FrenzyColor = new Color(0.95f, 0.15f, 0.12f);
        private static readonly Color BloodColor = new Color(0.85f, 0.08f, 0.12f);
        private static readonly Color BarBackColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color StaminaColor = new Color(0.35f, 0.8f, 1f);
        private static readonly Color ExhaustedColor = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color HumidityColor = new Color(0.6f, 0.85f, 0.95f);
        private static readonly Color SatietyNormalColor = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color SatietyHighlightColor = new Color(1f, 0.6f, 0.2f);
        private static readonly Color HidingColor = new Color(0.4f, 0.65f, 1f);
        private static readonly Color ItchColor = new Color(1f, 0.45f, 0.2f);
        private static readonly Color SuspiciousVignette = new Color(1f, 0.85f, 0.3f, 0.25f);
        private static readonly Color FrenzyVignette = new Color(1f, 0.1f, 0.1f, 0.45f);
        private const float PulseSpeed = 6f;
        private const float PulseDepth = 0.35f;

        private readonly List<Image> _biteDots = new List<Image>();
        private bool _built;

        public Image BloodFill { get; private set; }

        public Text BloodText { get; private set; }

        public Image SatietyIcon { get; private set; }

        public Image Eye { get; private set; }

        public Image EyeFill { get; private set; }

        public Image CalmRing { get; private set; }

        public Text FrenzyText { get; private set; }

        public Text StatusText { get; private set; }

        public Image Vignette { get; private set; }

        public Image StaminaFill { get; private set; }

        public RectTransform DashTick { get; private set; }

        public Text WetText { get; private set; }

        public GameObject HumidityRoot { get; private set; }

        public Image HumidityFill { get; private set; }

        public Image ItchRing { get; private set; }

        public Image Crosshair { get; private set; }

        public Text PromptText { get; private set; }

        public RectTransform HeadArrow { get; private set; }

        public RectTransform AttackWarning { get; private set; }

        public RectTransform HidingArrow { get; private set; }

        public Text TutorialText { get; private set; }

        public RectTransform Root { get; private set; }

        public int VisibleBiteDots
        {
            get
            {
                int count = 0;
                foreach (var dot in _biteDots)
                {
                    count += dot.gameObject.activeSelf ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>프롬프트를 게임패드 표기로 쓸지.</summary>
        public bool UseGamepadLabels { get; set; }

        public void Build()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            Root = (RectTransform)transform;

            Vignette = CreateImage("AwarenessVignette", Root, HudSprites.Vignette, Color.clear);
            Stretch(Vignette.rectTransform);

            BuildTop();
            BuildEdges();
            BuildBottom();
            BuildCenter();
        }

        public void Apply(HudState state, float time)
        {
            Build();
            ApplyTop(state, time);
            ApplyEdges(state, time);
            ApplyBottom(state);
            ApplyCenter(state);
        }

        public string PromptLabel(HudPrompt prompt, int escapePresses)
        {
            switch (prompt)
            {
                case HudPrompt.Attach:
                    return UseGamepadLabels ? "B: 착지" : "F: 착지";
                case HudPrompt.Suck:
                    return UseGamepadLabels ? "X 홀드: 흡혈" : "LMB 홀드: 흡혈";
                case HudPrompt.Detach:
                    return UseGamepadLabels ? "B: 이탈" : "F: 이탈";
                case HudPrompt.Escape:
                    return UseGamepadLabels ? $"A ×{escapePresses}!" : $"Shift ×{escapePresses}!";
                default:
                    return string.Empty;
            }
        }

        public static Color AwarenessColor(AwarenessState state)
        {
            switch (state)
            {
                case AwarenessState.Frenzy:
                    return FrenzyColor;
                case AwarenessState.Suspicious:
                    return SuspiciousColor;
                default:
                    return SafeColor;
            }
        }

        private void BuildTop()
        {
            var top = CreateRect("Top", Root, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(480f, 26f));
            var bloodBack = CreateImage("BloodBack", top, HudSprites.Square, BarBackColor);
            Stretch(bloodBack.rectTransform);
            BloodFill = CreateImage("BloodFill", top, HudSprites.Square, BloodColor);
            Stretch(BloodFill.rectTransform);
            BloodFill.type = Image.Type.Filled;
            BloodFill.fillMethod = Image.FillMethod.Horizontal;
            BloodText = CreateText("BloodText", top, 20, TextAnchor.MiddleCenter);
            Stretch(BloodText.rectTransform);

            SatietyIcon = CreateImage("SatietyIcon", top, HudSprites.Circle, SatietyNormalColor);
            Place(SatietyIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(24f, 0f), new Vector2(24f, 24f));
            for (int i = 0; i < MaxBiteDots; i++)
            {
                var dot = CreateImage($"BiteDot{i}", top, HudSprites.Circle, BloodColor);
                Place(dot.rectTransform, new Vector2(1f, 0.5f), new Vector2(52f + (i * 16f), 0f), new Vector2(10f, 10f));
                _biteDots.Add(dot);
            }

            var eyeRoot = CreateRect("Eye", Root, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(56f, 56f));
            Eye = CreateImage("EyeBack", eyeRoot, HudSprites.Circle, SafeColor);
            Stretch(Eye.rectTransform);
            EyeFill = CreateImage("EyeFill", eyeRoot, HudSprites.Circle, new Color(1f, 1f, 1f, 0.45f));
            Stretch(EyeFill.rectTransform);
            EyeFill.type = Image.Type.Filled;
            EyeFill.fillMethod = Image.FillMethod.Vertical;
            CalmRing = CreateImage("CalmRing", eyeRoot, HudSprites.Ring, Color.white);
            Place(CalmRing.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76f, 76f));
            CalmRing.type = Image.Type.Filled;
            CalmRing.fillMethod = Image.FillMethod.Radial360;
            CalmRing.fillOrigin = (int)Image.Origin360.Top;
            FrenzyText = CreateText("FrenzyText", eyeRoot, 20, TextAnchor.MiddleCenter);
            Stretch(FrenzyText.rectTransform);
            StatusText = CreateText("StatusText", eyeRoot, 20, TextAnchor.MiddleCenter);
            Place(StatusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -30f), new Vector2(160f, 28f));
        }

        private void BuildEdges()
        {
            HeadArrow = CreateImage("HeadArrow", Root, HudSprites.Arrow, SafeColor).rectTransform;
            HeadArrow.sizeDelta = new Vector2(40f, 40f);
            HidingArrow = CreateImage("HidingArrow", Root, HudSprites.Arrow, HidingColor).rectTransform;
            HidingArrow.sizeDelta = new Vector2(34f, 34f);
            AttackWarning = CreateImage("AttackWarning", Root, HudSprites.Circle, FrenzyColor).rectTransform;
            AttackWarning.sizeDelta = new Vector2(120f, 120f);
        }

        private void BuildBottom()
        {
            var stamina = CreateRect("Stamina", Root, new Vector2(0f, 0f), new Vector2(200f, 40f), new Vector2(320f, 18f));
            var back = CreateImage("StaminaBack", stamina, HudSprites.Square, BarBackColor);
            Stretch(back.rectTransform);
            StaminaFill = CreateImage("StaminaFill", stamina, HudSprites.Square, StaminaColor);
            Stretch(StaminaFill.rectTransform);
            StaminaFill.type = Image.Type.Filled;
            StaminaFill.fillMethod = Image.FillMethod.Horizontal;
            DashTick = CreateImage("DashTick", stamina, HudSprites.Square, Color.white).rectTransform;
            DashTick.anchorMin = new Vector2(0f, 0f);
            DashTick.anchorMax = new Vector2(0f, 1f);
            DashTick.sizeDelta = new Vector2(3f, 6f);
            WetText = CreateText("WetText", stamina, 20, TextAnchor.MiddleLeft);
            Place(WetText.rectTransform, new Vector2(1f, 0.5f), new Vector2(100f, 0f), new Vector2(180f, 28f));

            var humidity = CreateRect("Humidity", Root, new Vector2(0f, 0f), new Vector2(115f, 72f), new Vector2(150f, 12f));
            HumidityRoot = humidity.gameObject;
            var humidityBack = CreateImage("HumidityBack", humidity, HudSprites.Square, BarBackColor);
            Stretch(humidityBack.rectTransform);
            HumidityFill = CreateImage("HumidityFill", humidity, HudSprites.Square, HumidityColor);
            Stretch(HumidityFill.rectTransform);
            HumidityFill.type = Image.Type.Filled;
            HumidityFill.fillMethod = Image.FillMethod.Horizontal;
            var humidityLabel = CreateText("HumidityLabel", humidity, 16, TextAnchor.MiddleLeft);
            humidityLabel.text = "습기";
            Place(humidityLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(30f, 0f), new Vector2(50f, 22f));

            PromptText = CreateText("Prompt", Root, 30, TextAnchor.MiddleCenter);
            Place(PromptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(800f, 44f));
            TutorialText = CreateText("Tutorial", Root, 26, TextAnchor.MiddleCenter);
            Place(TutorialText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(1000f, 40f));
        }

        private void BuildCenter()
        {
            ItchRing = CreateImage("ItchRing", Root, HudSprites.Ring, ItchColor);
            Place(ItchRing.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            ItchRing.type = Image.Type.Filled;
            ItchRing.fillMethod = Image.FillMethod.Radial360;
            ItchRing.fillOrigin = (int)Image.Origin360.Top;
            Crosshair = CreateImage("Crosshair", Root, HudSprites.Circle, Color.white);
            Place(Crosshair.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));
        }

        private void ApplyTop(HudState state, float time)
        {
            BloodFill.fillAmount = state.BloodFraction;
            BloodText.text = $"{Mathf.FloorToInt(state.BloodFraction * 100f)}%";
            SatietyIcon.color = state.SatietyHighlighted ? SatietyHighlightColor : SatietyNormalColor;
            for (int i = 0; i < _biteDots.Count; i++)
            {
                _biteDots[i].gameObject.SetActive(i < state.BiteMarks);
            }

            Eye.transform.parent.gameObject.SetActive(state.HasHuman);
            Color stateColor = AwarenessColor(state.Awareness);
            bool frenzy = state.Awareness == AwarenessState.Frenzy;
            Eye.color = frenzy ? Pulse(stateColor, time) : stateColor;
            EyeFill.gameObject.SetActive(state.Awareness == AwarenessState.Suspicious);
            EyeFill.fillAmount = state.AwarenessFill;
            CalmRing.gameObject.SetActive(frenzy);
            CalmRing.fillAmount = state.CalmProgress;
            FrenzyText.text = frenzy && state.FrenzyMinRemaining > 0f ? state.FrenzyMinRemaining.ToString("F1") : string.Empty;
            StatusText.text = state.Hidden ? "은신" : state.Occluded ? "가려짐" : string.Empty;

            switch (state.Awareness)
            {
                case AwarenessState.Frenzy:
                    Vignette.color = Pulse(FrenzyVignette, time);
                    break;
                case AwarenessState.Suspicious:
                    Vignette.color = SuspiciousVignette;
                    break;
                default:
                    Vignette.color = Color.clear;
                    break;
            }
        }

        private void ApplyEdges(HudState state, float time)
        {
            PlaceMarker(HeadArrow, state.HeadArrow);
            HeadArrow.GetComponent<Image>().color = AwarenessColor(state.Awareness);
            PlaceMarker(HidingArrow, state.HidingDirection);
            PlaceMarker(AttackWarning, state.AttackWarning);
            AttackWarning.GetComponent<Image>().color = Pulse(new Color(FrenzyColor.r, FrenzyColor.g, FrenzyColor.b, 0.6f), time);
        }

        private void ApplyBottom(HudState state)
        {
            StaminaFill.fillAmount = state.StaminaFraction;
            StaminaFill.color = state.Exhausted ? ExhaustedColor : StaminaColor;
            var tickParent = (RectTransform)DashTick.parent;
            DashTick.anchoredPosition = new Vector2(state.DashCostFraction * tickParent.rect.width, 0f);
            WetText.text = state.Wet ? $"젖은 날개 {state.WetRemaining:F1}s" : string.Empty;
            HumidityRoot.SetActive(state.HumidityVisible);
            HumidityFill.fillAmount = state.HumidityFraction;
            PromptText.text = PromptLabel(state.Prompt, state.EscapePressesRemaining);
        }

        private void ApplyCenter(HudState state)
        {
            ItchRing.gameObject.SetActive(state.ItchVisible);
            ItchRing.fillAmount = state.ItchFraction;
            Crosshair.gameObject.SetActive(state.CrosshairVisible);
        }

        private void PlaceMarker(RectTransform marker, EdgeMarker edge)
        {
            marker.gameObject.SetActive(edge.Visible);
            if (!edge.Visible)
            {
                return;
            }

            // 앵커를 뷰포트 점에 두면 캔버스 크기(해상도)와 무관하게 같은 비율 위치에 놓인다.
            marker.anchorMin = edge.Viewport;
            marker.anchorMax = edge.Viewport;
            marker.anchoredPosition = Vector2.zero;
            marker.localRotation = Quaternion.Euler(0f, 0f, edge.AngleDegrees);
        }

        private static Color Pulse(Color color, float time)
        {
            float pulse = 1f - (PulseDepth * (0.5f + (0.5f * Mathf.Sin(time * PulseSpeed))));
            return new Color(color.r, color.g, color.b, color.a * pulse);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, anchor, position, size);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(string name, Transform parent, int size, TextAnchor alignment)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = UiFonts.Default;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
