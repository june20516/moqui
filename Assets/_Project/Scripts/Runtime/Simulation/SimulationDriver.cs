using System;
using Moqui.Core.Simulation;
using Moqui.Unity.Input;
using UnityEngine;

namespace Moqui.Unity.Simulation
{
    /// <summary>
    /// 프레임마다 입력을 샘플링하고 필요한 만큼 틱을 돌린다. 렌더링은 직전·현재 틱 위치를 보간한 값을 쓴다.
    /// MonoBehaviour가 아니므로 EditMode 테스트에서 프레임을 직접 넣어 검증할 수 있다.
    /// </summary>
    public sealed class SimulationDriver
    {
        private readonly SimulationClock _clock = new SimulationClock();
        private Vector3 _previousPlayerPosition;
        private Vector3 _currentPlayerPosition;

        public SimulationDriver(GameSimulation simulation, CommandCollector collector)
        {
            Simulation = simulation;
            Collector = collector;
            _previousPlayerPosition = simulation.Player.Position.ToUnity();
            _currentPlayerPosition = _previousPlayerPosition;
        }

        /// <summary>틱 하나가 끝날 때마다 호출된다 (이벤트 처리용).</summary>
        public event Action<GameSimulation> TickCompleted;

        public GameSimulation Simulation { get; }

        public CommandCollector Collector { get; }

        public Vector3 InterpolatedPlayerPosition => Vector3.Lerp(_previousPlayerPosition, _currentPlayerPosition, _clock.Alpha);

        /// <summary>한 프레임을 진행하고 실행한 틱 수를 돌려준다.</summary>
        public int Frame(float deltaTime)
        {
            Collector.Sample(deltaTime);
            int ticks = _clock.Advance(deltaTime);
            for (int i = 0; i < ticks; i++)
            {
                _previousPlayerPosition = Simulation.Player.Position.ToUnity();
                Simulation.Step(Collector.NextCommand());
                _currentPlayerPosition = Simulation.Player.Position.ToUnity();
                TickCompleted?.Invoke(Simulation);
            }

            return ticks;
        }
    }
}
