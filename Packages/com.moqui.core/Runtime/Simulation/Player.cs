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
        public const int NeverTick = int.MinValue / 2;

        public Player(Vector3 position, float collisionRadius, float stamina)
        {
            Position = position;
            CollisionRadius = collisionRadius;
            Stamina = stamina;
            State = PlayerState.Flying;
        }

        public Vector3 Position { get; set; }

        /// <summary>관성 속도. 바람 같은 외력은 포함하지 않는다.</summary>
        public Vector3 Velocity { get; set; }

        /// <summary>관성과 별개로 이번 틱 이동에 그대로 더해지는 외력 속도 (바람, spec/06).</summary>
        public Vector3 ExternalVelocity { get; set; }

        public float CollisionRadius { get; }

        public PlayerState State { get; set; }

        public float Yaw { get; set; }

        /// <summary>포만·탈진·젖은 날개·스킬 등 이동 속도 배율의 곱. 틱 시작마다 다시 계산한다.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>가속·감속 시간 배율 (스킬 와류 제어).</summary>
        public float AccelTimeMultiplier { get; set; } = 1f;

        /// <summary>대시 거리 배율 (포만, spec/04 §5).</summary>
        public float DashDistanceMultiplier { get; set; } = 1f;

        /// <summary>숨은 상태 = Shadow Zone 안 (spec/03). 시각 배율이 0이 되고 디버프 회복이 빨라진다.</summary>
        public bool IsHidden { get; set; }

        /// <summary>이번 틱 Precision 입력. 비행 소음 반경을 줄인다.</summary>
        public bool PrecisionHeld { get; set; }

        public float Stamina { get; set; }

        public float ExhaustedRemaining { get; set; }

        public bool IsExhausted => ExhaustedRemaining > 0f;

        public int LastStaminaSpendTick { get; set; } = NeverTick;

        public int LastDashStartTick { get; set; } = NeverTick;

        public Vector3 DashDirection { get; set; }

        public int DashTicksRemaining { get; set; }

        public float DashStepDistance { get; set; }
    }
}
