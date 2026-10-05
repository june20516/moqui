using System.Numerics;

namespace Moqui.Core.Simulation
{
    public enum AttackKind
    {
        Clap,
        Slap,
        BlindSwat,
        ReactSlap,

        /// <summary>모기약 분사 (spec/06). 판정 대신 연무를 만든다.</summary>
        Spray,

        /// <summary>취한 타겟의 무작위 휘두르기 (spec/06).</summary>
        DrunkSwat,
    }

    public enum AttackPhase
    {
        Idle,
        Telegraph,
        Active,
        Recovery,
    }

    /// <summary>
    /// 진행 중인 공격 하나 (spec/02 §7). 인간은 한 번에 하나만 공격하고, 회복 중에도 새 공격을 하지 않는다.
    /// 목표는 예고 시작 시점에 고정된다.
    /// </summary>
    public sealed class HumanAttack
    {
        public AttackPhase Phase { get; set; } = AttackPhase.Idle;

        public AttackKind Kind { get; set; }

        public Vector3 Target { get; set; }

        public float Radius { get; set; }

        public int TelegraphEndTick { get; set; }

        public int ActiveEndTick { get; set; }

        public int RecoveryEndTick { get; set; }

        public bool IsBusy => Phase != AttackPhase.Idle;

        // ---- 몸 동작 (spec/02, D-052·D-053) ----

        /// <summary>예고를 시작한 틱.</summary>
        public int StartTick { get; set; }

        /// <summary>때리는 팔 (박수는 두 팔). 없으면 −1.</summary>
        public int ArmA { get; set; } = -1;

        public int ArmB { get; set; } = -1;

        /// <summary>목표에 닿으려고 취하는 자세와 그 자세까지 걸리는 시간.</summary>
        public PostureState PostureTarget { get; set; } = PostureState.Rest;

        public PostureState PostureStart { get; set; } = PostureState.Rest;

        public float PostureTime { get; set; }

        /// <summary>이 공격의 손 최고 속도 (u/s).</summary>
        public float HandPeakSpeed { get; set; }

        /// <summary>팔별 손 경로 (어깨 기준 상대 위치): 예고 시작 손(= 돌아올 휴식 손), 타격 시작·끝.</summary>
        public Vector3[] HandStart { get; } = new Vector3[2];

        public Vector3[] StrikeStart { get; } = new Vector3[2];

        public Vector3[] StrikeEnd { get; } = new Vector3[2];

        /// <summary>직전 틱 손바닥 (월드). 판정은 이 점에서 지금 손바닥까지 지나간 선분이다.</summary>
        public Vector3[] PreviousHand { get; } = new Vector3[2];

        public int ActiveTicks { get; set; }

        public int RecoveryStartTick { get; set; }

        /// <summary>회복 시간 (틱). 타격 길이가 정해지면 그 뒤에 붙는다.</summary>
        public int RecoveryTicks { get; set; }

        public PostureState RecoveryPostureStart { get; set; } = PostureState.Rest;

        /// <summary>팔 슬롯(0 = ArmA, 1 = ArmB)의 팔 번호.</summary>
        public int ArmAt(int slot) => slot == 0 ? ArmA : ArmB;
    }
}
