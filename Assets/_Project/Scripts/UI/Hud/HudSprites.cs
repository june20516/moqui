using UnityEngine;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>
    /// 화이트박스 HUD용 절차적 스프라이트 (사각형, 원, 고리, 삼각형 화살표, 비네트). 아트 교체(M10) 전까지 쓴다.
    /// </summary>
    public static class HudSprites
    {
        private const int Size = 64;
        private const float RingInnerRatio = 0.72f;
        private const float VignetteInnerRatio = 0.6f;
        private const float CornerRadius = 1.41421356f;

        private static Sprite _square;
        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _arrow;
        private static Sprite _vignette;
        private static Sprite _weight;

        public static Sprite Square => _square ??= Create("HudSquare", (x, y) => 1f);

        public static Sprite Circle => _circle ??= Create("HudCircle", (x, y) => Mathf.Clamp01((1f - Radius(x, y)) * Size * 0.5f));

        public static Sprite Ring => _ring ??= Create("HudRing", (x, y) =>
        {
            float r = Radius(x, y);
            return r <= 1f && r >= RingInnerRatio ? 1f : 0f;
        });

        /// <summary>오른쪽(+x)을 가리키는 삼각형.</summary>
        public static Sprite Arrow => _arrow ??= Create("HudArrow", (x, y) =>
        {
            float u = (float)x / (Size - 1);
            float v = Mathf.Abs(((float)y / (Size - 1)) - 0.5f) * 2f;
            return v <= 1f - u ? 1f : 0f;
        });

        /// <summary>
        /// 추(케틀벨) 모양: 아래 둥근 몸통 + 위 손잡이 고리. 포만(무거워짐) 표시로, 자국 점(원)과 헷갈리지 않게 한다 (spec/08, M12).
        /// </summary>
        public static Sprite Weight => _weight ??= Create("HudWeight", (x, y) =>
        {
            float u = (((float)x / (Size - 1)) * 2f) - 1f;
            float v = (((float)y / (Size - 1)) * 2f) - 1f;
            float body = Mathf.Sqrt((u * u) + ((v + 0.28f) * (v + 0.28f)));
            float handle = Mathf.Sqrt((u * u) + ((v - 0.42f) * (v - 0.42f)));
            bool inBody = body <= 0.66f;
            bool inHandle = v >= 0.2f && handle <= 0.42f && handle >= 0.24f;
            return inBody || inHandle ? 1f : 0f;
        });

        /// <summary>가운데가 투명하고 테두리로 갈수록 진해지는 비네트.</summary>
        public static Sprite Vignette => _vignette ??= Create("HudVignette", (x, y) => Mathf.SmoothStep(0f, 1f, (Radius(x, y) - VignetteInnerRatio) / (CornerRadius - VignetteInnerRatio)));

        private static float Radius(int x, int y)
        {
            float u = (((float)x / (Size - 1)) * 2f) - 1f;
            float v = (((float)y / (Size - 1)) * 2f) - 1f;
            return Mathf.Sqrt((u * u) + (v * v));
        }

        private static Sprite Create(string name, System.Func<int, int, float> alpha)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[(y * Size) + x] = new Color(1f, 1f, 1f, alpha(x, y));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
