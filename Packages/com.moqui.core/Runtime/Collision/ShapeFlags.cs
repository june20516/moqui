using System;

namespace Moqui.Core.Collision
{
    /// <summary>형상의 역할 (tech/architecture.md §4.5). 질의는 마스크와 하나라도 겹치는 형상만 본다.</summary>
    [Flags]
    public enum ShapeFlags
    {
        None = 0,
        Obstacle = 1 << 0,
        Attachable = 1 << 1,
        SkinSite = 1 << 2,
        ShadowZone = 1 << 3,
        Hazard = 1 << 4,
        Wind = 1 << 5,
        Glass = 1 << 6,

        /// <summary>인간 몸 캡슐. 시야 판정 광선에서 제외할 때 쓴다 (D-029).</summary>
        Body = 1 << 7,

        /// <summary>약한 습기 영역 볼륨 (spec/05 §2).</summary>
        HumidWeak = 1 << 8,

        /// <summary>강한 습기(증기) 영역 볼륨 (spec/05 §2).</summary>
        HumidStrong = 1 << 9,
        All = ~0,
    }
}
