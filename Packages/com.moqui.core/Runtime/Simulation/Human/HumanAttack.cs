using System.Numerics;

namespace Moqui.Core.Simulation
{
    public enum AttackKind
    {
        Clap,
        Slap,
        BlindSwat,
        ReactSlap,
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
    }
}
