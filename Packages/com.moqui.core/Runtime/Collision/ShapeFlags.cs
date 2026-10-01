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
        All = ~0,
    }
}
