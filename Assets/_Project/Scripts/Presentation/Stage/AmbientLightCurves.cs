using Moqui.Core.Data.Levels;
using UnityEngine;

namespace Moqui.Unity.Presentation.Stage
{
    /// <summary>한 순간의 조명 상태: 기본 세기·색에 곱하는 배율.</summary>
    public readonly struct LightSample
    {
        public LightSample(float intensity, Color tint)
        {
            Intensity = intensity;
            Tint = tint;
        }

        public float Intensity { get; }

        public Color Tint { get; }

        public static LightSample Steady => new LightSample(1f, Color.white);
    }

    /// <summary>
    /// 분위기 조명의 시간 변화 (표현 전용, spec/assets/lighting.md, M14). 실제 빛처럼 일정하지 않게, 그러나 산만하지 않게 흔든다.
    /// 같은 시각·같은 씨앗이면 같은 값을 낸다(캡처·테스트 재현). 평균 세기는 1 근처다.
    /// </summary>
    public static class AmbientLightCurves
    {
        /// <summary>TV 장면 길이 범위(s): 이 사이에서 장면마다 다르다.</summary>
        public const float TvSceneMin = 1.5f;
        public const float TvSceneMax = 5f;

        /// <summary>형광등이 0.25초 창마다 짧게 끊길 확률 (평균 약 17초에 한 번).</summary>
        public const float FluorescentStutterChance = 0.015f;
        public const float FluorescentWindow = 0.25f;

        /// <summary>창밖으로 차가 지나가는 간격(s)과 그 창에 차가 있을 확률.</summary>
        public const float CityCarWindow = 6f;
        public const float CityCarChance = 0.35f;

        private static readonly Color TvCool = new Color(0.8f, 0.9f, 1.15f);
        private static readonly Color TvWarm = new Color(1.15f, 0.95f, 0.8f);
        private static readonly Color Headlight = new Color(1.25f, 1.2f, 1.15f);

        public static LightSample Sample(RoomLightFlicker kind, float time, uint seed)
        {
            float offset = Hash01(seed, 0) * 100f;
            switch (kind)
            {
                case RoomLightFlicker.Lamp:
                    // 백열등: 전압이 아주 느리게 오르내린다 (±3%).
                    return new LightSample(1f + (0.06f * (Noise(time * 0.3f, offset) - 0.5f)), Color.white);
                case RoomLightFlicker.Tv:
                    return Tv(time, seed, offset);
                case RoomLightFlicker.Fluorescent:
                    return Fluorescent(time, seed);
                case RoomLightFlicker.Phone:
                    return Phone(time, seed, offset);
                case RoomLightFlicker.Ember:
                    // 불꽃: 느린 숨 + 빠른 떨림 두 겹, 깊게 (0.75~1.25).
                    float slow = Noise(time * 1.7f, offset);
                    float fast = Noise(time * 9f, offset + 37f);
                    return new LightSample(0.75f + (0.35f * slow) + (0.15f * fast), Color.Lerp(new Color(1f, 0.85f, 0.75f), Color.white, slow));
                case RoomLightFlicker.City:
                    return City(time, seed, offset);
                default:
                    return LightSample.Steady;
            }
        }

        /// <summary>TV: 장면마다 밝기·색이 툭 바뀌고(컷), 장면 안에서는 잔물결이 인다.</summary>
        private static LightSample Tv(float time, uint seed, float offset)
        {
            // 장면 경계를 찾는다: 장면 길이는 장면 번호의 해시로 정하므로 앞에서부터 더해 간다.
            // 한 주기(최대 64장면)마다 반복해 시간이 길어져도 계산량이 일정하다.
            float cycle = 0f;
            for (uint i = 0; i < 64; i++)
            {
                cycle += SceneLength(seed, i);
            }

            float local = Mathf.Repeat(time, cycle);
            uint scene = 0;
            float start = 0f;
            while (scene < 63 && start + SceneLength(seed, scene) <= local)
            {
                start += SceneLength(seed, scene);
                scene++;
            }

            float brightness = 0.55f + (0.6f * Hash01(seed, 1000 + scene));
            Color tint = Color.Lerp(TvCool, TvWarm, Hash01(seed, 2000 + scene));
            float ripple = 0.12f * (Noise(time * 4f, offset) - 0.5f);
            return new LightSample(brightness + ripple, tint);
        }

        private static float SceneLength(uint seed, uint scene)
        {
            return Mathf.Lerp(TvSceneMin, TvSceneMax, Hash01(seed, 3000 + scene));
        }

        /// <summary>형광등: 거의 일정하다가 드물게 0.25초 동안 파르르 끊긴다.</summary>
        private static LightSample Fluorescent(float time, uint seed)
        {
            uint window = (uint)Mathf.FloorToInt(time / FluorescentWindow);
            if (Hash01(seed, 4000 + window) < FluorescentStutterChance)
            {
                bool off = Mathf.Repeat(time * 24f, 1f) < 0.5f;
                return new LightSample(off ? 0.2f : 0.9f, Color.white);
            }

            return new LightSample(1f, Color.white);
        }

        /// <summary>휴대폰: 스크롤에 따라 밝기가 조금씩 바뀌고, 가끔 화면 전환으로 잠깐 어두워진다.</summary>
        private static LightSample Phone(float time, uint seed, float offset)
        {
            float scroll = 1f + (0.24f * (Noise(time * 1.2f, offset) - 0.5f));
            uint window = (uint)Mathf.FloorToInt(time / 0.4f);
            float dip = Hash01(seed, 5000 + window) < 0.03f ? 0.55f : 1f;
            return new LightSample(scroll * dip, Color.white);
        }

        /// <summary>도시 불빛: 아주 느린 흐름 + 가끔 창밖을 지나가는 차의 하얀 빛.</summary>
        private static LightSample City(float time, uint seed, float offset)
        {
            float drift = 1f + (0.16f * (Noise(time * 0.1f, offset) - 0.5f));
            uint window = (uint)Mathf.FloorToInt(time / CityCarWindow);
            float car = 0f;
            if (Hash01(seed, 6000 + window) < CityCarChance)
            {
                float phase = Mathf.Repeat(time / CityCarWindow, 1f) - 0.5f;
                car = Mathf.Exp(-(phase * phase) / (2f * 0.06f * 0.06f));
            }

            return new LightSample(drift + (0.45f * car), Color.Lerp(Color.white, Headlight, car));
        }

        /// <summary>문자열 ID → 씨앗 (FNV-1a, 실행마다 같다).</summary>
        public static uint Seed(string id)
        {
            uint hash = 2166136261;
            foreach (char c in id)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash;
        }

        private static float Noise(float x, float offset)
        {
            return Mathf.Clamp01(Mathf.PerlinNoise(x + offset, offset * 0.37f));
        }

        private static float Hash01(uint seed, uint index)
        {
            uint h = seed ^ (index * 0x9E3779B9u);
            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            h *= 0x846CA68Bu;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
