using System;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 볼텍스 대시 방향 (spec/01). 좌우(카메라 로컬)와 상하(월드) 입력 중 절댓값이 큰 축의 방향.
    /// 둘 다 없으면 위쪽. 전후 입력은 무시한다. 크기가 같으면 좌우를 우선한다 (D-026).
    /// allowDiagonal(스킬 와류 제어 3레벨)이면 좌우와 상하를 조합한 대각선도 허용한다.
    /// </summary>
    public static class DashDirectionResolver
    {
        public static Vector3 Resolve(in PlayerCommand command, bool allowDiagonal)
        {
            float horizontal = Math.Sign(command.Move.X);
            float vertical = Math.Sign(command.Vertical);
            if (horizontal == 0f && vertical == 0f)
            {
                return Vector3.UnitY;
            }

            Vector3 right = CameraBasis.FromYaw(command.LookYaw).Right;
            if (allowDiagonal && horizontal != 0f && vertical != 0f)
            {
                return Vector3.Normalize((right * horizontal) + (Vector3.UnitY * vertical));
            }

            bool horizontalWins = MathF.Abs(command.Move.X) >= MathF.Abs(command.Vertical);
            return horizontalWins ? right * horizontal : Vector3.UnitY * vertical;
        }
    }
}
