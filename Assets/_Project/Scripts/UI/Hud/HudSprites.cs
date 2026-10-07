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

        private static Sprite _eye;
        private static Sprite _sound;
        private static Sprite _itch;
        private static Sprite _glance;
        private static Sprite _people;

        /// <summary>눈 (들킨 원인: 시야). 아몬드 윤곽 + 가운데 눈동자.</summary>
        public static Sprite Eye => _eye ??= Create("HudEye", (x, y) => EyeShape(U(x), V(y), 0f));

        /// <summary>스피커와 음파 두 줄 (들킨 원인: 소리).</summary>
        public static Sprite Sound => _sound ??= Create("HudSound", (x, y) =>
        {
            float u = U(x);
            float v = V(y);
            bool box = u >= -0.9f && u <= -0.55f && Mathf.Abs(v) <= 0.22f;
            bool cone = u > -0.55f && u <= -0.2f && Mathf.Abs(v) <= 0.22f + ((u + 0.55f) * 1.3f);
            float r = Mathf.Sqrt(((u + 0.2f) * (u + 0.2f)) + (v * v));
            bool facing = u > -0.1f && Mathf.Abs(v) < (u + 0.2f) * 1.2f;
            bool wave = facing && (Mathf.Abs(r - 0.45f) < 0.07f || Mathf.Abs(r - 0.78f) < 0.07f);
            return box || cone || wave ? 1f : 0f;
        });

        /// <summary>긁은 자국 세 줄 (들킨 원인: 가려움).</summary>
        public static Sprite Itch => _itch ??= Create("HudItch", (x, y) =>
        {
            float u = U(x);
            float v = V(y);
            if ((u * u) + (v * v) > 0.8f)
            {
                return 0f;
            }

            foreach (float offset in new[] { -0.55f, 0f, 0.55f })
            {
                // 기울어진 선 v = u + offset 까지 거리.
                if (Mathf.Abs(v - u - offset) / 1.41421356f < 0.09f)
                {
                    return 1f;
                }
            }

            return 0f;
        });

        /// <summary>옆으로 흘긴 눈 + 느낌표 (들킨 원인: 흡혈 중 시선).</summary>
        public static Sprite Glance => _glance ??= Create("HudGlance", (x, y) =>
        {
            float u = U(x);
            float v = V(y);
            bool exclamation = Mathf.Abs(u - 0.72f) < 0.08f && ((v > 0.15f && v < 0.85f) || (v > -0.08f && v < 0.05f));
            return Mathf.Max(EyeShape((u + 0.18f) / 0.78f, (v + 0.3f) / 0.78f, -0.35f), exclamation ? 1f : 0f);
        });

        /// <summary>사람 둘 (들킨 원인: 함께 있던 사람이 알려 줌).</summary>
        public static Sprite People => _people ??= Create("HudPeople", (x, y) =>
        {
            float u = U(x);
            float v = V(y);
            foreach (float cx in new[] { -0.4f, 0.4f })
            {
                float head = ((u - cx) * (u - cx)) + ((v - 0.35f) * (v - 0.35f));
                float bu = (u - cx) / 0.38f;
                float bv = (v + 0.45f) / 0.5f;
                bool body = bv >= 0f && (bu * bu) + (bv * bv) <= 1f;
                if (head <= 0.22f * 0.22f || body)
                {
                    return 1f;
                }
            }

            return 0f;
        });

        /// <summary>들킨 원인 → 아이콘 (원인을 모르면 고리).</summary>
        public static Sprite CauseIcon(Moqui.Core.Simulation.AwarenessCause cause)
        {
            switch (cause)
            {
                case Moqui.Core.Simulation.AwarenessCause.Sight:
                    return Eye;
                case Moqui.Core.Simulation.AwarenessCause.Hearing:
                    return Sound;
                case Moqui.Core.Simulation.AwarenessCause.Itch:
                    return Itch;
                case Moqui.Core.Simulation.AwarenessCause.Glance:
                    return Glance;
                case Moqui.Core.Simulation.AwarenessCause.Alarm:
                    return People;
                default:
                    return Ring;
            }
        }

        private static float U(int x) => (((float)x / (Size - 1)) * 2f) - 1f;

        private static float V(int y) => (((float)y / (Size - 1)) * 2f) - 1f;

        /// <summary>아몬드 눈 윤곽 + 눈동자 (pupilX만큼 옆으로).</summary>
        private static float EyeShape(float u, float v, float pupilX)
        {
            float outer = 0.58f * (1f - (u * u));
            float inner = 0.4f * (1f - ((u / 0.86f) * (u / 0.86f)));
            bool inOuter = Mathf.Abs(u) <= 1f && Mathf.Abs(v) <= outer;
            bool inInner = Mathf.Abs(u) <= 0.86f && Mathf.Abs(v) <= inner;
            bool pupil = ((u - pupilX) * (u - pupilX)) + (v * v) <= 0.26f * 0.26f;
            return (inOuter && !inInner) || pupil ? 1f : 0f;
        }

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
