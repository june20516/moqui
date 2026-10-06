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

        /// <summary>날갯짓 박자 (표현 전용, gulf §12): 정지 비행 0.85배 → 최고 속도 1.35배, 정밀 비행이면 0.7배 더.</summary>
        public const float FlapSpeedIdle = 0.85f;
        public const float FlapSpeedRange = 0.5f;
        public const float PrecisionFlapMul = 0.7f;

        /// <summary>속도 비율(0~1)과 정밀 비행 여부 → 애니메이터 재생 속도. 비행 자세가 아니면 1.</summary>
        public static float FlapSpeed(MokiPose pose, float speedRatio, bool precise)
        {
            bool flying = pose == MokiPose.Idle || pose == MokiPose.Move;
            if (!flying)
            {
                return 1f;
            }

            return (FlapSpeedIdle + (FlapSpeedRange * Mathf.Clamp01(speedRatio))) * (precise ? PrecisionFlapMul : 1f);
        }

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
            var player = simulation.Player;
            _animator.speed = FlapSpeed(CurrentPose, player.Velocity.Length() / simulation.Settings.Flight.Speed, player.PrecisionHeld);
        }
    }
}
