using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>시뮬레이션 플레이어 상태를 모키 Animator의 State 파라미터로 전달한다 (spec/10).</summary>
    public sealed class MokiAnimator : MonoBehaviour
    {
        public const string StateParameter = "State";
        private static readonly int StateHash = Animator.StringToHash(StateParameter);

        [SerializeField]
        private SimulationRunner _runner;

        [SerializeField]
        private Animator _animator;

        public MokiPose CurrentPose { get; private set; }

        public Animator Animator => _animator;

        private void Update()
        {
            if (!_runner.IsRunning)
            {
                return;
            }

            var simulation = _runner.Driver.Simulation;
            CurrentPose = MokiPoses.From(simulation.Player, simulation.LastCommand.SuckHeld, simulation.Settings.Flight.Speed);
            _animator.SetInteger(StateHash, (int)CurrentPose);
        }
    }
}
