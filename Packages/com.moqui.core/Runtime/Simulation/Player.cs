using System.Numerics;

namespace Moqui.Core.Simulation
{
    public enum PlayerState
    {
        Flying,
        Dashing,
        Attached,
        Trapped,
        Dislodged,
        Dead,
    }

    /// <summary>플레이어 엔티티. 규칙은 시스템에 있고 여기에는 상태만 둔다.</summary>
    public sealed class Player
    {
        public Player(Vector3 position, float collisionRadius)
        {
            Position = position;
            CollisionRadius = collisionRadius;
            State = PlayerState.Flying;
        }

        public Vector3 Position { get; set; }

        /// <summary>관성 속도 (외력인 바람은 포함하지 않는다).</summary>
        public Vector3 Velocity { get; set; }

        public float CollisionRadius { get; }

        public PlayerState State { get; set; }

        public float Yaw { get; set; }

        /// <summary>포만·탈진·젖은 날개·스킬 등 이동 속도 배율의 곱. 각 시스템이 틱마다 다시 계산한다.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>가속·감속 시간 배율 (스킬 와류 제어).</summary>
        public float AccelTimeMultiplier { get; set; } = 1f;
    }
}
