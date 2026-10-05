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
        private static readonly Color ActiveReadyColor = new Color(0.75f, 0.55f, 1f, 0.9f);
        private static readonly Color ActiveCoolingColor = new Color(0.35f, 0.32f, 0.4f, 0.8f);
        private static readonly Color ActiveInUseColor = new Color(1f, 0.85f, 0.4f, 0.95f);
        private static readonly Color ToxinColor = new Color(0.45f, 0.85f, 0.35f);
        private static readonly Color CoilFloorColor = new Color(0.85f, 0.85f, 0.75f);
        private const float ToxinVignetteStep = 0.12f;
        private const float PulseSpeed = 6f;
        private const float PulseDepth = 0.35f;

        private readonly List<Image> _biteDots = new List<Image>();
        private readonly List<RectTransform> _toxinTicks = new List<RectTransform>();
        private readonly float[] _toxinTickFractions = new float[3];
        private bool _built;

        public Image BloodFill { get; private set; }

        public Text BloodText { get; private set; }

        public Image SatietyIcon { get; private set; }

        /// <summary>자국 점 스프라이트 (포만 아이콘과 구분되는지 검사용).</summary>
        public Sprite BiteDotSprite => _biteDots.Count > 0 ? _biteDots[0].sprite : null;

        /// <summary>포만 아이콘 아래 글자.</summary>
        public Text SatietyLabel { get; private set; }

        public Image Eye { get; private set; }

        public Image EyeFill { get; private set; }

        public Image CalmRing { get; private set; }

        public Text FrenzyText { get; private set; }

        public Text StatusText { get; private set; }

        public Image Vignette { get; private set; }

        public Image StaminaFill { get; private set; }

        public RectTransform DashTick { get; private set; }

        public Text WetText { get; private set; }

        public GameObject ToxinRoot { get; private set; }

        public Image ToxinFill { get; private set; }

        public RectTransform ToxinFloorMarker { get; private set; }

        public Text ToxinTierText { get; private set; }

        public Image ToxinVignette { get; private set; }

        /// <summary>중독 게이지 눈금 (tier1·tier2·tier3).</summary>
        public System.Collections.Generic.IReadOnlyList<RectTransform> ToxinTicks => _toxinTicks;

        public GameObject HumidityRoot { get; private set; }

        public Image HumidityFill { get; private set; }

        public Image ItchRing { get; private set; }

        public Image Crosshair { get; private set; }

        public Text PromptText { get; private set; }

        public RectTransform HeadArrow { get; private set; }

        public RectTransform AttackWarning { get; private set; }

        public RectTransform HidingArrow { get; private set; }

        public Text TutorialText { get; private set; }

        public GameObject ActiveSkillRoot { get; private set; }

        public Image ActiveSkillIcon { get; private set; }

        public Text ActiveSkillText { get; private set; }

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
            ToxinVignette = CreateImage("ToxinVignette", Root, HudSprites.Vignette, Color.clear);
            Stretch(ToxinVignette.rectTransform);

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
            ApplyBottom(state, time);
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
                    return UseGamepadLabels ? $"A ×{escapePresses}!" : $"우클릭 ×{escapePresses}!";
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

            // 포만: 추 모양 + "포만" 글자 (자국 점과 다른 모양, M12).
            SatietyIcon = CreateImage("SatietyIcon", top, HudSprites.Weight, SatietyNormalColor);
            Place(SatietyIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(24f, 2f), new Vector2(26f, 26f));
            SatietyLabel = CreateText("SatietyLabel", top, 14, TextAnchor.MiddleCenter);
            SatietyLabel.text = "포만";
            Place(SatietyLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(24f, -22f), new Vector2(48f, 18f));
            for (int i = 0; i < MaxBiteDots; i++)
            {
                var dot = CreateImage($"BiteDot{i}", top, HudSprites.Circle, BloodColor);
                Place(dot.rectTransform, new Vector2(1f, 0.5f), new Vector2(60f + (i * 16f), 0f), new Vector2(10f, 10f));
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

            var toxin = CreateRect("Toxin", Root, new Vector2(0f, 0f), new Vector2(200f, 74f), new Vector2(320f, 12f));
            ToxinRoot = toxin.gameObject;
            var toxinBack = CreateImage("ToxinBack", toxin, HudSprites.Square, BarBackColor);
            Stretch(toxinBack.rectTransform);
            ToxinFill = CreateImage("ToxinFill", toxin, HudSprites.Square, ToxinColor);
            Stretch(ToxinFill.rectTransform);
            ToxinFill.type = Image.Type.Filled;
            ToxinFill.fillMethod = Image.FillMethod.Horizontal;
            for (int i = 0; i < _toxinTickFractions.Length; i++)
            {
                var tick = CreateImage($"ToxinTick{i}", toxin, HudSprites.Square, new Color(1f, 1f, 1f, 0.7f)).rectTransform;
                tick.anchorMin = new Vector2(0f, 0f);
                tick.anchorMax = new Vector2(0f, 1f);
                tick.sizeDelta = new Vector2(2f, 4f);
                _toxinTicks.Add(tick);
            }

            ToxinFloorMarker = CreateImage("ToxinFloor", toxin, HudSprites.Square, CoilFloorColor).rectTransform;
            ToxinFloorMarker.anchorMin = new Vector2(0f, 0f);
            ToxinFloorMarker.anchorMax = new Vector2(0f, 1f);
            ToxinFloorMarker.sizeDelta = new Vector2(4f, 10f);
            ToxinTierText = CreateText("ToxinTier", toxin, 18, TextAnchor.MiddleLeft);
            Place(ToxinTierText.rectTransform, new Vector2(0f, 1f), new Vector2(80f, 16f), new Vector2(160f, 24f));

            var humidity = CreateRect("Humidity", Root, new Vector2(0f, 0f), new Vector2(470f, 74f), new Vector2(150f, 12f));
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
            var active = CreateRect("ActiveSkill", Root, new Vector2(1f, 0f), new Vector2(-110f, 70f), new Vector2(72f, 72f));
            ActiveSkillRoot = active.gameObject;
            ActiveSkillIcon = CreateImage("Icon", active, HudSprites.Circle, ActiveReadyColor);
            Stretch(ActiveSkillIcon.rectTransform);
            ActiveSkillText = CreateText("Cooldown", active, 20, TextAnchor.MiddleCenter);
            Place(ActiveSkillText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -22f), new Vector2(140f, 28f));
            var activeLabel = CreateText("Name", active, 18, TextAnchor.MiddleCenter);
            activeLabel.text = "미끼";
            Stretch(activeLabel.rectTransform);
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

        private void ApplyBottom(HudState state, float time)
        {
            StaminaFill.fillAmount = state.StaminaFraction;
            StaminaFill.color = state.Exhausted ? ExhaustedColor : StaminaColor;
            var tickParent = (RectTransform)DashTick.parent;
            DashTick.anchoredPosition = new Vector2(state.DashCostFraction * tickParent.rect.width, 0f);
            WetText.text = state.Wet ? $"젖은 날개 {state.WetRemaining:F1}s" : string.Empty;
            ApplyToxin(state, time);
            HumidityRoot.SetActive(state.HumidityVisible);
            HumidityFill.fillAmount = state.HumidityFraction;
            PromptText.text = PromptLabel(state.Prompt, state.EscapePressesRemaining);
            ActiveSkillRoot.SetActive(state.ActiveSkillVisible);
            bool ready = state.ActiveSkillCooldown <= 0f;
            ActiveSkillIcon.color = state.ActiveSkillInUse ? ActiveInUseColor : ready ? ActiveReadyColor : ActiveCoolingColor;
            ActiveSkillText.text = ready ? (UseGamepadLabels ? "Y" : "Q") : $"{state.ActiveSkillCooldown:0.0}s";
        }

        /// <summary>중독 단계 눈금 위치를 정한다 (tier1·tier2·tier3, 게이지 최대 대비).</summary>
        public void SetToxinTiers(float tier1, float tier2, float tier3)
        {
            Build();
            _toxinTickFractions[0] = tier1;
            _toxinTickFractions[1] = tier2;
            _toxinTickFractions[2] = tier3;
        }

        public static string ToxinTierLabel(int tier)
        {
            switch (tier)
            {
                case 3:
                    return "중독 · 랜덤";
                case 2:
                    return "중독 · 반전";
                case 1:
                    return "중독 · 끊김";
                default:
                    return "중독";
            }
        }

        private void ApplyToxin(HudState state, float time)
        {
            ToxinRoot.SetActive(state.ToxinVisible);
            var bar = (RectTransform)ToxinFill.transform.parent;
            float width = bar.rect.width;
            for (int i = 0; i < _toxinTicks.Count; i++)
            {
                _toxinTicks[i].anchoredPosition = new Vector2(_toxinTickFractions[i] * width, 0f);
            }

            ToxinFill.fillAmount = state.ToxinFraction;
            ToxinFloorMarker.gameObject.SetActive(state.ToxinFloorFraction > 0f);
            ToxinFloorMarker.anchoredPosition = new Vector2(state.ToxinFloorFraction * width, 0f);
            ToxinTierText.text = ToxinTierLabel(state.ToxinTier);
            ToxinTierText.color = state.StutterActive || state.RandomActive ? SatietyHighlightColor : Color.white;

            // 단계별 화면 가장자리 녹색 일렁임 (spec/06). 반전·랜덤도 화면을 뒤집지 않고 아이콘·가장자리로만 알린다.
            float strength = state.ToxinTier * ToxinVignetteStep;
            ToxinVignette.color = state.ToxinTier > 0 ? Pulse(new Color(ToxinColor.r, ToxinColor.g, ToxinColor.b, strength), time) : Color.clear;
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
