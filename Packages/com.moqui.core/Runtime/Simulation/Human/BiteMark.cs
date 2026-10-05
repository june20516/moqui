using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 물린 자국 하나 (spec/04 §4, M12): 그 세션에서 주둥이를 꽂은 피부 표면 지점. 부위가 움직이면 함께 움직인다.
    /// 같은 부위를 여러 번 물면 자국도 여러 개다.
    /// </summary>
    public sealed class BiteMark
    {
        public BiteMark(string partId, SurfaceAnchor anchor)
        {
            PartId = partId;
            Anchor = anchor;
        }

        public string PartId { get; }

        public SurfaceAnchor Anchor { get; }

        /// <summary>자국의 현재 위치와 피부 바깥 방향 (월드).</summary>
        public void Resolve(out Vector3 position, out Vector3 normal)
        {
            Anchor.Resolve(out position, out normal);
        }
    }
}
