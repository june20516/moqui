using Moqui.Core.Simulation;

namespace Moqui.Unity.Simulation
{
    /// <summary>
    /// 프레임 시간을 누적해 이번 프레임에 돌릴 틱 수를 정한다 (tech/architecture.md §4.1).
    /// 프레임이 크게 밀리면 따라잡기를 MaxTicksPerFrame으로 제한한다 (멈춤 뒤 순간이동 방지).
    /// </summary>
    public sealed class SimulationClock
    {
        public const int MaxTicksPerFrame = 5;

        private float _accumulator;

        /// <summary>직전 틱과 다음 틱 사이의 보간 비율 (0~1).</summary>
        public float Alpha => _accumulator / GameSimulation.DeltaTime;

        public int Advance(float frameDeltaTime)
        {
            _accumulator += frameDeltaTime;
            int ticks = 0;
            while (_accumulator >= GameSimulation.DeltaTime && ticks < MaxTicksPerFrame)
            {
                _accumulator -= GameSimulation.DeltaTime;
                ticks++;
            }

            if (ticks == MaxTicksPerFrame && _accumulator >= GameSimulation.DeltaTime)
            {
                _accumulator = 0f;
            }

            return ticks;
        }
    }
}
