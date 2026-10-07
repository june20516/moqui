using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    public enum PlayerState
    {
        Flying,
        Dashing,
        Attached,
        Trapped,
        Dislodged,

        /// <summary>거미줄에 걸림: 이동 입력을 막고 web.struggleTime 뒤 사망 (spec/06).</summary>
        Webbed,
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
        /// <summary>대시 발동 지연 (s, 포만 디버프). 0이면 바로 튀어 나간다.</summary>
        public float DashDelay { get; set; }

        /// <summary>숨 고르는 중인 대시가 튀어 나갈 틱. 없으면 NeverTick.</summary>
        public int PendingDashTick { get; set; } = NeverTick;

        /// <summary>숨 고르는 중인 대시의 방향 (누른 순간의 입력·시점으로 정한다).</summary>
        public System.Numerics.Vector3 PendingDashDirection { get; set; }

        public bool IsChargingDash => PendingDashTick != NeverTick;

        /// <summary>숨은 상태 = Shadow Zone 안 (spec/03). 시각 배율이 0이 되고 디버프 회복이 빨라진다.</summary>
        public bool IsHidden { get; set; }

        /// <summary>이번 틱 Precision 입력. 비행 소음 반경을 줄인다.</summary>
        public bool PrecisionHeld { get; set; }

        /// <summary>정밀 비행 자동 착지 직후, 이동 키를 놓기 전까지 그 입력으로 떨어지지 않는다 (gulf §2).</summary>
        public bool HoldAfterAutoLand { get; set; }

        /// <summary>마지막으로 표면에서 떨어진 틱 (떼기·튕김·부착 대시). 자동 착지 지연에 쓴다.</summary>
        public int LastReleaseTick { get; set; } = NeverTick;

        /// <summary>이번 틱 Suck 입력을 누르고 있는가 (흡혈 중 이벤트, D-056).</summary>
        public bool SuckHeld { get; set; }

        public float Stamina { get; set; }

        /// <summary>중독 게이지 0~100 (spec/06).</summary>
        public float Toxin { get; set; }

        /// <summary>현재 위치의 모기향 중독 하한 (HUD 표시).</summary>
        public float ToxinFloor { get; set; }

        /// <summary>거미줄에 걸린 틱.</summary>
        public int WebbedTick { get; set; } = NeverTick;

        /// <summary>소음 반경 배율 (선풍기 마스킹, spec/06). 틱마다 다시 계산한다.</summary>
        public float NoiseRadiusMultiplier { get; set; } = 1f;

        public float ExhaustedRemaining { get; set; }

        public bool IsExhausted => ExhaustedRemaining > 0f;

        public int LastStaminaSpendTick { get; set; } = NeverTick;

        public int LastDashStartTick { get; set; } = NeverTick;

        /// <summary>직전 대시가 일반 대시라 연속 와류 추가 대시를 쓸 수 있는가 (체인당 1회, spec/09).</summary>
        public bool ChainDashAvailable { get; set; }

        /// <summary>스태미나 회복 배율 (젖은 날개, 스킬). 틱 시작마다 다시 계산한다.</summary>
        public float StaminaRegenMultiplier { get; set; } = 1f;

        /// <summary>대시 스태미나 비용에 더하는 값 (젖은 날개). 틱 시작마다 다시 계산한다.</summary>
        public float DashCostAdd { get; set; }

        // ---- 물 (spec/05) ----

        /// <summary>갇혀 있는 물방울. Trapped가 아니면 null.</summary>
        public FallingBody TrappedDrop { get; set; }

        /// <summary>Trapped 중 누른 탈출(Dash) 입력 횟수.</summary>
        public int EscapePresses { get; set; }

        /// <summary>젖은 날개 남은 시간 (s).</summary>
        public float WetRemaining { get; set; }

        public bool IsWet => WetRemaining > 0f;

        /// <summary>습기 게이지 (0~100).</summary>
        public float Humidity { get; set; }

        /// <summary>강한 습기(증기) 영역 안.</summary>
        public bool InSteam { get; set; }

        /// <summary>켜진 조명 영역 안인가 (spec/06 조명 스위치, M14).</summary>
        public bool InLight { get; set; }

        // ---- 흡혈 (spec/04) ----

        /// <summary>흡혈 게이지 b (0~100%). 100이면 Stage Clear.</summary>
        public float BloodGauge { get; set; }

        /// <summary>진행 중인 흡혈 세션. 없으면 null.</summary>
        public SuckSession SuckSession { get; set; }

        // ---- 부착 (spec/03) ----

        /// <summary>부착 중인 표면 점. 부착하지 않았으면 null.</summary>
        public SurfaceAnchor Anchor { get; set; }

        /// <summary>캐릭터 up 벡터. 부착 중에는 표면 법선, 비행 중에는 월드 위.</summary>
        public Vector3 Up { get; set; } = Vector3.UnitY;

        public int AttachedTick { get; set; } = NeverTick;

        /// <summary>부착점이 이번 틱에 움직인 속도 (부위 움직임 → 튕겨남 판정).</summary>
        public Vector3 AnchorVelocity { get; set; }

        /// <summary>튕겨남 경직이 끝나는 틱 (spec/02 §6).</summary>
        public int StunEndTick { get; set; } = NeverTick;

        public Vector3 DashDirection { get; set; }

        public int DashTicksRemaining { get; set; }

        public float DashStepDistance { get; set; }
    }
}
