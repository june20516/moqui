using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>틱마다 바깥(Unity, 테스트)으로 내보내는 1회성 사건 (tech/architecture.md §4.3).</summary>
    public abstract class SimulationEvent
    {
        protected SimulationEvent(int tick)
        {
            Tick = tick;
        }

        public int Tick { get; }
    }

    public enum NoiseSource
    {
        Dash,
    }

    /// <summary>반경 안의 인간 경계를 즉시 올리는 소음 핑 (spec/01 대시, spec/02).</summary>
    public sealed class NoiseEmitted : SimulationEvent
    {
        public NoiseEmitted(int tick, NoiseSource source, Vector3 position, float radius, float awareness)
            : base(tick)
        {
            Source = source;
            Position = position;
            Radius = radius;
            Awareness = awareness;
        }

        public NoiseSource Source { get; }

        public Vector3 Position { get; }

        public float Radius { get; }

        public float Awareness { get; }
    }

    public sealed class AwarenessStateChanged : SimulationEvent
    {
        public AwarenessStateChanged(int tick, string humanId, AwarenessState from, AwarenessState to)
            : base(tick)
        {
            HumanId = humanId;
            From = from;
            To = to;
        }

        public string HumanId { get; }

        public AwarenessState From { get; }

        public AwarenessState To { get; }
    }

    /// <summary>예고 시작. Unity는 손 동작, 판정 위치 표시, 경고음을 낸다 (spec/02 §7).</summary>
    public sealed class AttackTelegraphStarted : SimulationEvent
    {
        public AttackTelegraphStarted(int tick, string humanId, AttackKind kind, Vector3 target, float radius, int telegraphTicks)
            : base(tick)
        {
            HumanId = humanId;
            Kind = kind;
            Target = target;
            Radius = radius;
            TelegraphTicks = telegraphTicks;
        }

        public string HumanId { get; }

        public AttackKind Kind { get; }

        public Vector3 Target { get; }

        public float Radius { get; }

        public int TelegraphTicks { get; }
    }

    public enum DeathCause
    {
        Attack,
        Web,
        WaterImpact,
        Spray,
    }

    public sealed class PlayerDied : SimulationEvent
    {
        public PlayerDied(int tick, DeathCause cause, Vector3 position)
            : base(tick)
        {
            Cause = cause;
            Position = position;
        }

        public DeathCause Cause { get; }

        public Vector3 Position { get; }
    }
}
