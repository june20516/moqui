using System.Collections.Generic;
using Moqui.Core.Data.Levels;
using UnityEngine;

namespace Moqui.Unity.Presentation.Stage
{
    /// <summary>
    /// 방 분위기 조명 (표현 전용, spec/assets/lighting.md, M14): 방 데이터의 lights로 Unity 조명을 만들고,
    /// 실제 빛처럼 시간에 따라 흔든다(TV 장면 전환, 형광등 끊김, 불꽃 일렁임...). 빛나는 가구(TV 화면 등)는 같은 빛으로 칠한다.
    /// 게임 규칙(조명 스위치 판정)과는 무관하다.
    /// </summary>
    public sealed class RoomLightingView : MonoBehaviour
    {
        /// <summary>스포트 조명의 안쪽 원뿔 비율: 가장자리가 부드럽게 흐려진다.</summary>
        public const float InnerSpotRatio = 0.55f;

        /// <summary>그림자 진하기: 밤 방은 반사광이 있어 그림자가 완전히 검지 않다.</summary>
        public const float ShadowStrength = 0.75f;

        /// <summary>빛나는 가구가 빛 색 쪽으로 끌려가는 정도 (툰 셰이더 _SelfIllumination).</summary>
        public const float GlowSelfIllumination = 1f;

        /// <summary>쿠키 텍스처가 아직 없을 때 쓰는 절차 쿠키 해상도.</summary>
        public const int CookieSize = 128;

        public const string WindowCookieId = "tex-light-cookie-window";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SelfIlluminationId = Shader.PropertyToID("_SelfIllumination");

        private readonly List<Entry> _entries = new List<Entry>();
        private MaterialPropertyBlock _block;
        private bool _built;

        private sealed class Entry
        {
            public RoomLightDefinition Definition;
            public Light Light;
            public Renderer Glow;
            public uint Seed;
            public Color BaseColor;
        }

        /// <summary>만든 조명 (테스트·캡처용).</summary>
        public IReadOnlyList<Light> Lights
        {
            get
            {
                var lights = new List<Light>(_entries.Count);
                foreach (var entry in _entries)
                {
                    lights.Add(entry.Light);
                }

                return lights;
            }
        }

        /// <summary>방의 조명을 만든다. visuals는 LevelView가 만든 형상 ID → 그림 (빛나는 가구 찾기).</summary>
        public void Build(RoomDefinition room, IReadOnlyDictionary<string, GameObject> visuals)
        {
            _block = new MaterialPropertyBlock();
            foreach (var definition in room.Lights)
            {
                var light = new GameObject($"RoomLight_{definition.Id}").AddComponent<Light>();
                light.transform.SetParent(transform, false);
                light.transform.position = definition.Position.ToUnity();
                light.type = definition.Type == RoomLightType.Spot ? LightType.Spot : LightType.Point;
                if (light.type == LightType.Spot)
                {
                    light.transform.rotation = Quaternion.LookRotation(definition.Direction.ToUnity().normalized);
                    light.spotAngle = definition.SpotAngle;
                    light.innerSpotAngle = definition.SpotAngle * InnerSpotRatio;
                }

                var baseColor = new Color(definition.Color.X, definition.Color.Y, definition.Color.Z);
                light.color = baseColor;
                light.intensity = definition.Intensity;
                light.range = definition.Range;
                light.shadows = definition.Shadows ? LightShadows.Soft : LightShadows.None;
                light.shadowStrength = ShadowStrength;
                light.cookie = CookieFor(definition.Cookie, light.type);

                Renderer glow = null;
                if (definition.GlowShape.Length > 0 && visuals != null && visuals.TryGetValue(definition.GlowShape, out var glowObject))
                {
                    glow = glowObject.GetComponent<Renderer>();
                }

                _entries.Add(new Entry { Definition = definition, Light = light, Glow = glow, Seed = AmbientLightCurves.Seed(definition.Id), BaseColor = baseColor });
            }

            _built = true;
            Render(0f);
        }

        /// <summary>시각 time(s)의 조명 상태를 반영한다. 테스트에서 직접 부를 수 있다.</summary>
        public void Render(float time)
        {
            foreach (var entry in _entries)
            {
                LightSample sample = AmbientLightCurves.Sample(entry.Definition.Flicker, time, entry.Seed);
                Color color = entry.BaseColor * sample.Tint;
                entry.Light.color = color;
                entry.Light.intensity = entry.Definition.Intensity * sample.Intensity;
                if (entry.Glow != null)
                {
                    // 빛나는 면은 빛과 같은 색·밝기로 (TV 화면이 방을 비추는 빛과 함께 바뀐다).
                    entry.Glow.GetPropertyBlock(_block);
                    _block.SetColor(BaseColorId, Color.Lerp(Color.black, color, Mathf.Clamp01(0.35f + (0.55f * sample.Intensity))));
                    _block.SetFloat(SelfIlluminationId, GlowSelfIllumination);
                    entry.Glow.SetPropertyBlock(_block);
                }
            }
        }

        private void Update()
        {
            if (_built)
            {
                Render(Time.time);
            }
        }

        /// <summary>
        /// 쿠키 텍스처: 애셋(spec/assets)이 들어오기 전에는 창틀 무늬를 절차로 만든다.
        /// 스포트 조명만 2D 쿠키를 쓴다. 모르는 ID면 쿠키 없음.
        /// </summary>
        private static Texture2D CookieFor(string cookieId, LightType type)
        {
            if (type != LightType.Spot || cookieId != WindowCookieId)
            {
                return null;
            }

            return CreateWindowCookie(CookieSize);
        }

        /// <summary>2×3 창살(창틀 두께 6%) + 가장자리로 갈수록 어두워지는 창 무늬. 흰색 = 빛이 지나감.</summary>
        public static Texture2D CreateWindowCookie(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.R8, false)
            {
                name = "ProceduralWindowCookie",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            const float Frame = 0.06f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float paneU = Mathf.Repeat(u * 2f, 1f);
                    float paneV = Mathf.Repeat(v * 3f, 1f);
                    bool mullion = paneU < Frame || paneU > 1f - Frame || paneV < Frame * 1.5f || paneV > 1f - (Frame * 1.5f);
                    float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 8f);
                    byte value = (byte)(mullion ? 0 : Mathf.RoundToInt(255f * edge));
                    pixels[(y * size) + x] = new Color32(value, value, value, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
