using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>몸동작 단계 (spec/02, D-053). 레벨 데이터 `human.maxPosture`가 허용 상한을 정한다. 걷기(step)는 이후 확장.</summary>
    public enum PostureLevel
    {
        Arm,
        Lean,
        Turn,
        Rise,
    }

    /// <summary>상체 기울기·비틀기·일어섬 정도. 모두 몸 로컬 기준.</summary>
    public struct PostureState
    {
        /// <summary>상체를 기울이는 수평 방향 (몸 로컬, 단위 벡터).</summary>
        public Vector3 LeanDirection;

        /// <summary>상체 기울기 (도).</summary>
        public float LeanAngle;

        /// <summary>상체 비틀기 (도, 몸 로컬 위 축 기준, + = 오른쪽).</summary>
        public float Twist;

        /// <summary>일어섬 정도 0~1.</summary>
        public float Rise;

        public static PostureState Rest => new PostureState { LeanDirection = Vector3.UnitZ };
    }

    /// <summary>현재 몸 포즈: 자세와 팔별 손 목표.</summary>
    public sealed class BodyPose
    {
        public BodyPose(int armCount)
        {
            HandTargets = new Vector3?[armCount];
        }

        public PostureState Posture = PostureState.Rest;

        /// <summary>팔별 손바닥 목표 (월드). null이면 휴식 자세(데이터·동작 그대로).</summary>
        public Vector3?[] HandTargets { get; }
    }
}
