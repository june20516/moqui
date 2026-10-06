using System.Collections.Generic;
using Moqui.Core.Tutorial;
using Moqui.Unity.Settings;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>
    /// 튜토리얼 안내 문구와 켜기/끄기 설정 (spec/08 §튜토리얼, §설정 "튜토리얼 안내").
    /// 진행 판정은 Core TutorialTracker가 하고, 여기서는 현재 안내를 장치에 맞는 문구로 바꾼다.
    /// </summary>
    public sealed class TutorialHints
    {
        public const string PreferenceKey = "settings.tutorialHints";
        private const string On = "1";
        private const string Off = "0";

        private static readonly Dictionary<string, (string Keyboard, string Gamepad)> Texts = new Dictionary<string, (string, string)>
        {
            ["move"] = ("WASD로 이동, Space/C로 오르내리기", "왼쪽 스틱으로 이동, RT/LT로 오르내리기"),
            ["look"] = ("마우스로 둘러보기", "오른쪽 스틱으로 둘러보기"),
            ["precision"] = ("Shift를 누른 채 움직이면 정밀 비행 (조용함)", "LB를 누른 채 움직이면 정밀 비행 (조용함)"),
            ["dash"] = ("우클릭으로 진행 방향 대시 (소리가 크다)", "A로 대시 (소리가 크다)"),
            ["attach"] = ("가구 가까이에서 F로 착지", "가구 가까이에서 B로 착지"),
            ["hide"] = ("푸른빛 그림자 속에 숨으면 들키지 않는다", "푸른빛 그림자 속에 숨으면 들키지 않는다"),
            ["co2"] = ("분홍빛 숨결(CO₂)을 따라 인간에게 다가가기", "분홍빛 숨결(CO₂)을 따라 인간에게 다가가기"),
            ["suck"] = ("따뜻한 피부에 착지한 뒤 마우스 왼쪽 버튼을 누르고 있으면 지팡이를 꽂아 흡혈 — 꽂고 있는 동안은 몸이 붙들려요", "따뜻한 피부에 착지한 뒤 X를 누르고 있으면 지팡이를 꽂아 흡혈 — 꽂고 있는 동안은 몸이 붙들려요"),
            ["detach"] = ("F로 지팡이를 빼고 떨어져 나오기 — 대시로 억지로 뽑으면 더 가려워진다", "B로 지팡이를 빼고 떨어져 나오기 — 대시로 억지로 뽑으면 더 가려워진다"),
            ["flightMode"] = ("비행 방식은 설정에서 바꿀 수 있다: 호버(W = 수평 앞) / 자유 비행(W = 보는 방향)", "비행 방식은 설정에서 바꿀 수 있다: 호버(앞 = 수평) / 자유 비행(앞 = 보는 방향)"),
            ["visionZones"] = ("인간 시야(정면)에 들어가면 경계가 오른다. 가까울수록 빠르다", "인간 시야(정면)에 들어가면 경계가 오른다. 가까울수록 빠르다"),
            ["frenzyHide"] = ("광분하면 화면 가장자리 화살표를 따라 그림자에 숨기", "광분하면 화면 가장자리 화살표를 따라 그림자에 숨기"),
            ["reactionDodge"] = ("흡혈 중 인간이 움찔하면 F로 바로 떨어져 나오기", "흡혈 중 인간이 움찔하면 B로 바로 떨어져 나오기"),
        };

        private readonly IPreferenceStore _store;

        public TutorialHints(IPreferenceStore store)
        {
            _store = store;
        }

        /// <summary>기본값은 켜짐.</summary>
        public bool Enabled
        {
            get => _store.GetString(PreferenceKey, On) != Off;
            set => _store.SetString(PreferenceKey, value ? On : Off);
        }

        public static bool HasText(string step)
        {
            return Texts.ContainsKey(step);
        }

        /// <summary>지금 보여 줄 문구. 꺼져 있거나 안내가 없거나 끝났으면 빈 문자열.</summary>
        public string CurrentText(TutorialTracker tracker, bool gamepad)
        {
            string step = tracker?.CurrentStep;
            if (!Enabled || step == null || !Texts.TryGetValue(step, out var text))
            {
                return string.Empty;
            }

            return gamepad ? text.Gamepad : text.Keyboard;
        }
    }
}
