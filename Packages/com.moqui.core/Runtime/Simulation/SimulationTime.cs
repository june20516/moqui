using System;

namespace Moqui.Core.Simulation
{
    /// <summary>초 단위 tuning 값을 틱 수로 바꾼다. 쿨타임·지연처럼 경계가 중요한 시간은 틱으로 비교해 부동소수 누적 오차를 피한다.</summary>
    public static class SimulationTime
    {
        public static int ToTicks(float seconds)
        {
            return (int)MathF.Round(seconds * GameSimulation.TickRate);
        }

        public static bool HasElapsed(int sinceTick, int currentTick, float seconds)
        {
            return currentTick - sinceTick >= ToTicks(seconds);
        }
    }
}
