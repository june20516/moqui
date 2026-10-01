using System;
using System.Numerics;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 볼텍스 대시 방향 (spec/01, D-051): 진행 방향 = 이동 입력(카메라 yaw 기준 전후좌우 + 월드 상하)을 합친 벡터의 방향.
    /// 이동 입력이 없으면 난수 스트림으로 고른 구면 균등 무작위 방향.
    /// </summary>
    public static class DashDirectionResolver
    {
        private const float MinInputLength = 1e-4f;

        public static Vector3 Resolve(in PlayerCommand command, IRandom random)
        {
            Vector3 input = CameraBasis.FromYaw(command.LookYaw).ToWorld(command.Move) + (Vector3.UnitY * command.Vertical);
            return input.Length() > MinInputLength ? Vector3.Normalize(input) : RandomDirection(random);
        }

        /// <summary>구면 균등 분포의 단위 벡터 (높이 균등 + 방위각 균등).</summary>
        public static Vector3 RandomDirection(IRandom random)
        {
            float y = (float)((random.NextDouble() * 2.0) - 1.0);
            float angle = (float)(random.NextDouble() * 2.0 * Math.PI);
            float ring = MathF.Sqrt(MathF.Max(0f, 1f - (y * y)));
            return new Vector3(ring * MathF.Cos(angle), y, ring * MathF.Sin(angle));
        }
    }
}
