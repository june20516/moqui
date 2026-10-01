using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>Ending (spec/08): 정지 이미지 1장 + 텍스트 3줄 + "Thanks for playing". 스킵(확인)하면 Title.</summary>
    public sealed class EndingScreen : ScreenBase
    {
        private static readonly Color ImageColor = new Color(0.16f, 0.1f, 0.22f);

        public Button ContinueButton { get; private set; }

        public Text Lines { get; private set; }

        protected override void Build()
        {
            var canvas = UiFactory.CreateCanvas("EndingCanvas", 0, transform);

            // 정지 이미지 자리 (아트는 M10).
            var image = new GameObject("Still", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(canvas, false);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(960f, 420f);
            rect.anchoredPosition = new Vector2(0f, 220f);
            image.GetComponent<Image>().color = ImageColor;

            Lines = UiFactory.CreateText("Lines", canvas, "베란다의 마지막 한 모금까지, 아무도 모르게.\n오늘 밤도 모키는 배부르게 날아오른다.\n여름은 아직 길다.\n\nThanks for playing", 32, TextAnchor.MiddleCenter, 1200f, 300f);
            Lines.rectTransform.anchoredPosition = new Vector2(0f, -170f);
            ContinueButton = UiFactory.CreateButton("Continue", canvas, "타이틀로", () => Flow.OpenTitle(), 320f);
            ((RectTransform)ContinueButton.transform).anchoredPosition = new Vector2(0f, -420f);
            UiFactory.Focus(ContinueButton);
        }
    }
}
