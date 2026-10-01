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

        /// <summary>미끼 마법 (spec/09). 인간은 진짜 소음처럼 듣는다.</summary>
        Decoy,
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

    public sealed class PlayerAttached : SimulationEvent
    {
        public PlayerAttached(int tick, string shapeId, bool isSkinSite)
            : base(tick)
        {
            ShapeId = shapeId;
            IsSkinSite = isSkinSite;
        }

        public string ShapeId { get; }

        public bool IsSkinSite { get; }
    }

    public sealed class PlayerDetached : SimulationEvent
    {
        public PlayerDetached(int tick, string shapeId)
            : base(tick)
        {
            ShapeId = shapeId;
        }

        public string ShapeId { get; }
    }

    /// <summary>부위가 빠르게 움직여 튕겨 나감 (spec/02 §6). 사망이 아니다.</summary>
    public sealed class PlayerDislodged : SimulationEvent
    {
        public PlayerDislodged(int tick, string shapeId, Vector3 direction)
            : base(tick)
        {
            ShapeId = shapeId;
            Direction = direction;
        }

        public string ShapeId { get; }

        public Vector3 Direction { get; }
    }

    public sealed class DozeStateChanged : SimulationEvent
    {
        public DozeStateChanged(int tick, string humanId, DozeState from, DozeState to)
            : base(tick)
        {
            HumanId = humanId;
            From = from;
            To = to;
        }

        public string HumanId { get; }

        public DozeState From { get; }

        public DozeState To { get; }
    }

    /// <summary>졸던 인간이 깨기 doze.wakeTelegraph 전에 머리를 움찔한다 (spec/06).</summary>
    public sealed class DozeWakeTelegraph : SimulationEvent
    {
        public DozeWakeTelegraph(int tick, string humanId)
            : base(tick)
        {
            HumanId = humanId;
        }

        public string HumanId { get; }
    }

    public sealed class PlayerTrapped : SimulationEvent
    {
        public PlayerTrapped(int tick, string dropId)
            : base(tick)
        {
            DropId = dropId;
        }

        public string DropId { get; }
    }

    public sealed class PlayerEscapedDrop : SimulationEvent
    {
        public PlayerEscapedDrop(int tick)
            : base(tick)
        {
        }
    }

    public sealed class SuckSessionEnded : SimulationEvent
    {
        public SuckSessionEnded(int tick, string partId, float amount, bool biteMark)
            : base(tick)
        {
            PartId = partId;
            Amount = amount;
            BiteMark = biteMark;
        }

        public string PartId { get; }

        /// <summary>이 세션에서 빤 양 (%).</summary>
        public float Amount { get; }

        public bool BiteMark { get; }
    }

    /// <summary>스테이지 결과 (spec/04 §6, spec/09 보상 계산 입력).</summary>
    public sealed class StageResult
    {
        public StageResult(int clearTicks, int frenzyCount, int biteMarkCount)
        {
            ClearTicks = clearTicks;
            FrenzyCount = frenzyCount;
            BiteMarkCount = biteMarkCount;
        }

        public int ClearTicks { get; }

        public float ClearSeconds => ClearTicks * GameSimulation.DeltaTime;

        public int FrenzyCount { get; }

        public int BiteMarkCount { get; }
    }

    public sealed class StageCleared : SimulationEvent
    {
        public StageCleared(int tick, StageResult result)
            : base(tick)
        {
            Result = result;
        }

        public StageResult Result { get; }
    }

    /// <summary>인간의 모기약 분사가 연무를 만든 순간 (spec/06).</summary>
    public sealed class SprayReleased : SimulationEvent
    {
        public SprayReleased(int tick, Vector3 position)
            : base(tick)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>거미줄에 걸림 (spec/06).</summary>
    public sealed class PlayerWebbed : SimulationEvent
    {
        public PlayerWebbed(int tick, Vector3 position)
            : base(tick)
        {
            Position = position;
        }

        public Vector3 Position { get; }
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
