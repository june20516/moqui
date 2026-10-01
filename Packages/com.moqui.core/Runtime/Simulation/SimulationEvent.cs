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
}
