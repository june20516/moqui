using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>레벨 데이터 검사 규칙 (tech/verification.md §1). 레벨 로더(M7)가 모든 레벨에 적용한다.</summary>
    public static class LevelChecks
    {
        private const float MaxProbeDistance = 10000f;

        /// <summary>점 바로 아래 착지면(장애물 표면)까지의 높이. 아래에 아무것도 없으면 무한대.</summary>
        public static float HeightAboveLanding(CollisionWorld world, Vector3 point)
        {
            return world.Raycast(point, -Vector3.UnitY, MaxProbeDistance, ShapeFlags.Solid, out var hit) ? hit.Distance : float.PositiveInfinity;
        }

        /// <summary>물방울 발생원이 착지면에서 water.minSourceHeight 이상 위에 있는가 (spec/05 §1, 탈출 시간 보장).</summary>
        public static bool DripSourceHighEnough(CollisionWorld world, Vector3 source, WaterSettings water)
        {
            return HeightAboveLanding(world, source) >= water.MinSourceHeight;
        }
    }
}
