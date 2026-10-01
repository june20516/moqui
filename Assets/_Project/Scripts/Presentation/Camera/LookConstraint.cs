using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>1인칭 부착 상태의 시선 제한: 표면 법선 기준 limit 각도 원뿔 안으로 시선을 당긴다 (spec/00).</summary>
    public static class LookConstraint
    {
        public static Vector3 Direction(float yaw, float pitch)
        {
            return CameraPoseSolver.LookRotation(yaw, pitch) * Vector3.forward;
        }

        public static void ClampToCone(ref float yaw, ref float pitch, Vector3 normal, float limitDegrees)
        {
            Vector3 direction = Direction(yaw, pitch);
            if (Vector3.Angle(normal, direction) <= limitDegrees)
            {
                return;
            }

            Vector3 clamped = Vector3.RotateTowards(normal.normalized, direction, limitDegrees * Mathf.Deg2Rad, 0f);
            yaw = Mathf.Atan2(clamped.x, clamped.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(clamped.y, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }
}
