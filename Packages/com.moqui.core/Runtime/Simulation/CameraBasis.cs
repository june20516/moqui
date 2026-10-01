using System;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 카메라 yaw 기준 수평 이동 축. 좌표계는 Y-up, +Z 전방, 왼손 좌표계(spec/00)이므로
    /// yaw 0°는 +Z, yaw 90°는 +X를 향한다 (Unity의 Quaternion.Euler(0, yaw, 0)과 같다).
    /// </summary>
    public readonly struct CameraBasis
    {
        private const float DegreesToRadians = MathF.PI / 180f;

        private CameraBasis(Vector3 forward, Vector3 right)
        {
            Forward = forward;
            Right = right;
        }

        public Vector3 Forward { get; }

        public Vector3 Right { get; }

        public static CameraBasis FromYaw(float yawDegrees)
        {
            float radians = yawDegrees * DegreesToRadians;
            float sin = MathF.Sin(radians);
            float cos = MathF.Cos(radians);
            return new CameraBasis(new Vector3(sin, 0f, cos), new Vector3(cos, 0f, -sin));
        }

        /// <summary>Move 입력(X 오른쪽, Y 앞)을 월드 수평 벡터로 바꾼다.</summary>
        public Vector3 ToWorld(Vector2 move)
        {
            return (Right * move.X) + (Forward * move.Y);
        }
    }
}
