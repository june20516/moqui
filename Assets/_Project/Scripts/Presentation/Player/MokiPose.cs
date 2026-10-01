using Moqui.Core.Simulation;

namespace Moqui.Unity.Presentation
{
    /// <summary>모키의 최소 애니메이션 7종 (spec/10). 값은 Animator의 State 정수 파라미터와 같다.</summary>
    public enum MokiPose
    {
        Idle = 0,
        Move = 1,
        Dash = 2,
        Attach = 3,
        Suck = 4,
        Trapped = 5,
        Death = 6,
    }

    /// <summary>Core 플레이어 상태를 애니메이션 상태로 옮긴다 (표현 전용, 규칙 판정 없음).</summary>
    public static class MokiPoses
    {
        /// <summary>비행 속도에 대한 이 비율 이상으로 움직이면 이동 자세 (호버링과 구분하는 표현용 경계).</summary>
        public const float MoveSpeedFraction = 0.1f;

        /// <param name="suckHeld">이번 틱 Suck 입력. 세션은 손을 떼도 부착 중에는 남으므로 입력으로 흡혈 중인지 가린다.</param>
        public static MokiPose From(Moqui.Core.Simulation.Player player, bool suckHeld, float flightSpeed)
        {
            switch (player.State)
            {
                case PlayerState.Dead:
                    return MokiPose.Death;
                case PlayerState.Trapped:
                case PlayerState.Webbed:
                    return MokiPose.Trapped;
                case PlayerState.Attached:
                    return player.SuckSession != null && suckHeld ? MokiPose.Suck : MokiPose.Attach;
                case PlayerState.Dashing:
                    return MokiPose.Dash;
                default:
                    return player.Velocity.Length() >= flightSpeed * MoveSpeedFraction ? MokiPose.Move : MokiPose.Idle;
            }
        }
    }
}
