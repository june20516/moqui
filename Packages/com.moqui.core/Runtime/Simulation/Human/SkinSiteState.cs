using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>SkinSite 부위 하나의 상태: 독립적인 가려움 게이지(0~100)와 물린 자국 여부 (spec/04 §1).</summary>
    public sealed class SkinSiteState
    {
        public SkinSiteState(string partId, SkinSiteType type, CollisionShape shape)
        {
            PartId = partId;
            Type = type;
            Shape = shape;
        }

        public string PartId { get; }

        public SkinSiteType Type { get; }

        public CollisionShape Shape { get; }

        public float Itch { get; set; }

        public bool HasBiteMark { get; set; }
    }
}
