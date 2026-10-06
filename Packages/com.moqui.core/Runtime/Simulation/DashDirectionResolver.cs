using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 볼텍스 대시 방향 (spec/01, D-051, D-066): 진행 방향 = 이동 입력(카메라 yaw 기준 전후좌우 + 월드 상하)을 합친 벡터의 방향.
    /// 이동 입력이 없으면 보는 방향(yaw + pitch)으로 간다 — 조준점 쪽으로 튀어 나가므로 예측할 수 있다 (gulf §5).
    /// </summary>
    public static class DashDirectionResolver
    {
        private const float MinInputLength = 1e-4f;

        public static Vector3 Resolve(in PlayerCommand command)
        {
            Vector3 input = CameraBasis.FromYaw(command.LookYaw).ToWorld(command.Move) + (Vector3.UnitY * command.Vertical);
            return input.Length() > MinInputLength ? Vector3.Normalize(input) : CameraBasis.Aim(command.LookYaw, command.LookPitch);
        }
    }
}
