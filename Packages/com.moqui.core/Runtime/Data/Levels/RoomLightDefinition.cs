using System.Numerics;

namespace Moqui.Core.Data.Levels
{
    /// <summary>분위기 조명의 종류.</summary>
    public enum RoomLightType
    {
        Point,
        Spot,
    }

    /// <summary>분위기 조명이 시간에 따라 흔들리는 방식 (표현 전용, spec/assets/lighting.md).</summary>
    public enum RoomLightFlicker
    {
        /// <summary>일정하다 (달빛, LED).</summary>
        None,

        /// <summary>백열등: 아주 느리고 얕은 숨결.</summary>
        Lamp,

        /// <summary>TV: 장면이 바뀔 때마다 밝기·색이 튀고, 장면 안에서 잔물결.</summary>
        Tv,

        /// <summary>형광등: 거의 일정하다가 가끔 짧게 끊긴다.</summary>
        Fluorescent,

        /// <summary>휴대폰 화면: 스크롤마다 조금씩 바뀌고, 가끔 잠깐 꺼졌다 켜진다.</summary>
        Phone,

        /// <summary>촛불·등불: 빠르고 깊은 일렁임.</summary>
        Ember,

        /// <summary>창밖 도시 불빛: 아주 느린 흐름, 가끔 지나가는 차의 빛.</summary>
        City,
    }

    /// <summary>
    /// 방 분위기 조명 (data/rooms/*.json "lights", M14). 표현 전용이며 게임 규칙에는 쓰지 않는다.
    /// 규칙이 있는 조명 스위치는 레벨 데이터의 lights (spec/06)다.
    /// </summary>
    public sealed class RoomLightDefinition
    {
        public string Id { get; set; } = string.Empty;

        public RoomLightType Type { get; set; }

        public Vector3 Position { get; set; }

        /// <summary>스포트 조명이 비추는 방향 (정규화 전). 점 조명이면 쓰지 않는다.</summary>
        public Vector3 Direction { get; set; } = -Vector3.UnitY;

        /// <summary>선형 RGB (0~1).</summary>
        public Vector3 Color { get; set; } = Vector3.One;

        public float Intensity { get; set; }

        /// <summary>빛이 닿는 거리 (u).</summary>
        public float Range { get; set; }

        /// <summary>스포트 조명의 원뿔 각 (°).</summary>
        public float SpotAngle { get; set; } = 60f;

        public RoomLightFlicker Flicker { get; set; }

        public bool Shadows { get; set; }

        /// <summary>빛과 함께 스스로 빛나는 가구 형상 ID (예: TV 화면). 없으면 빈 문자열.</summary>
        public string GlowShape { get; set; } = string.Empty;

        /// <summary>빛 무늬 텍스처 애셋 ID (spec/assets). 없으면 빈 문자열.</summary>
        public string Cookie { get; set; } = string.Empty;

        /// <summary>
        /// 이 인간 부위(캡슐)의 끝(b)을 따라간다 (예: 휴대폰 화면 빛이 손을 따라 얼굴을 아래에서 비춤, gulf §13). 없으면 빈 문자열(고정 위치).
        /// </summary>
        public string AttachPart { get; set; } = string.Empty;

        public static RoomLightDefinition Parse(JsonAccess json)
        {
            var light = new RoomLightDefinition
            {
                Id = json.Get("id").String(),
                Type = json.Get("type").Enum<RoomLightType>(),
                Position = json.Get("position").Vector3(),
                Color = json.Get("color").Vector3(),
                Intensity = json.Get("intensity").Float(),
                Range = json.Get("range").Float(),
            };
            if (json.Has("direction"))
            {
                light.Direction = json.Get("direction").Vector3();
            }

            if (json.Has("spotAngle"))
            {
                light.SpotAngle = json.Get("spotAngle").Float();
            }

            if (json.Has("flicker"))
            {
                light.Flicker = json.Get("flicker").Enum<RoomLightFlicker>();
            }

            if (json.Has("shadows"))
            {
                light.Shadows = json.Get("shadows").Bool();
            }

            if (json.Has("glowShape"))
            {
                light.GlowShape = json.Get("glowShape").String();
            }

            if (json.Has("cookie"))
            {
                light.Cookie = json.Get("cookie").String();
            }

            if (json.Has("attachPart"))
            {
                light.AttachPart = json.Get("attachPart").String();
            }

            if (light.Intensity < 0f || light.Range <= 0f)
            {
                throw json.Error("intensity must be >= 0 and range > 0");
            }

            if (light.Type == RoomLightType.Spot && light.Direction.LengthSquared() <= 0f)
            {
                throw json.Get("direction").Error("spot light needs a non-zero direction");
            }

            return light;
        }
    }
}
