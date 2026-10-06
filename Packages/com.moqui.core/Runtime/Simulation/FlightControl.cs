using System;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>비행 조작 방식 (gulf §5, D-066).</summary>
    public enum FlightControlMode
    {
        /// <summary>호버(기본): W/S는 수평 앞뒤, 상승·하강은 Space/C.</summary>
        Hover,

        /// <summary>자유 비행: W/S는 보는 방향(위아래 포함)으로. Space/C는 그대로 더해진다.</summary>
        Free,
    }

    /// <summary>입력 장치 값 → Core 이동 명령 (Move 수평 + Vertical). 조작 방식만 바꾸고 규칙(속도·관성)은 그대로다.</summary>
    public static class FlightControl
    {
        public static void Map(FlightControlMode mode, Vector2 move, float vertical, float lookPitchDegrees, out Vector2 mappedMove, out float mappedVertical)
        {
            if (mode == FlightControlMode.Hover || move.Y == 0f)
            {
                mappedMove = move;
                mappedVertical = Math.Clamp(vertical, -1f, 1f);
                return;
            }

            // 앞뒤 입력을 시점 pitch로 나눠 수평 앞뒤와 상하로 보낸다.
            float pitch = lookPitchDegrees * (MathF.PI / 180f);
            mappedMove = new Vector2(move.X, move.Y * MathF.Cos(pitch));
            mappedVertical = Math.Clamp(vertical + (move.Y * MathF.Sin(pitch)), -1f, 1f);
        }
    }
}
