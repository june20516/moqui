using System;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>팔 운동학: 2관절 IK와 관절 한계 (knowledge/human-arm-motion.md, AAOS 가동 범위).</summary>
    public static class BodyKinematics
    {
        private const float DegreesToRadians = MathF.PI / 180f;

        /// <summary>
        /// 어깨에서 손바닥 목표로 팔을 뻗는다. 팔은 늘어나지 않는다: 목표가 팔 길이 밖이면 손이 닿는 데까지만 간다.
        /// 팔꿈치는 elbowFlexMax보다 더 굽지 않고(과신전도 없음), pole 쪽으로 꺾인다.
        /// </summary>
        public static void SolveArm(Vector3 shoulder, Vector3 target, float upperLength, float forearmLength, float handExtra, float elbowFlexMax, Vector3 pole,
            out Vector3 elbow, out Vector3 wrist, out Vector3 palm)
        {
            float a = upperLength;
            float b = forearmLength + handExtra;
            float interiorMin = (180f - elbowFlexMax) * DegreesToRadians;
            float minDistance = MathF.Sqrt(MathF.Max(0f, (a * a) + (b * b) - (2f * a * b * MathF.Cos(interiorMin))));
            Vector3 toTarget = target - shoulder;
            float length = toTarget.Length();
            Vector3 direction = length > 1e-4f ? toTarget / length : Vector3.UnitZ;
            float distance = Math.Clamp(length, minDistance, (a + b) * 0.9999f);

            float along = ((a * a) - (b * b) + (distance * distance)) / (2f * distance);
            float height = MathF.Sqrt(MathF.Max(0f, (a * a) - (along * along)));
            Vector3 bend = pole - shoulder;
            bend -= direction * Vector3.Dot(bend, direction);
            if (bend.LengthSquared() < 1e-6f)
            {
                bend = Vector3.Cross(direction, Math.Abs(direction.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX);
            }

            elbow = shoulder + (direction * along) + (Vector3.Normalize(bend) * height);
            palm = shoulder + (direction * distance);
            wrist = elbow + (Vector3.Normalize(palm - elbow) * forearmLength);
        }

        /// <summary>팔꿈치 굽힘 각도 (0 = 쭉 폄).</summary>
        public static float ElbowFlexion(Vector3 shoulder, Vector3 elbow, Vector3 palm)
        {
            Vector3 upper = Vector3.Normalize(shoulder - elbow);
            Vector3 lower = Vector3.Normalize(palm - elbow);
            float interior = MathF.Acos(Math.Clamp(Vector3.Dot(upper, lower), -1f, 1f)) / DegreesToRadians;
            return 180f - interior;
        }

        /// <summary>
        /// 어깨 가동 범위로 그 방향에 손을 뻗을 수 있는가 (상체 로컬 방향: X 바깥, Y 위, Z 앞).
        /// 앞쪽 반구는 굽힘·벌림(0~180°)으로 닿고, 뒤쪽은 늘어뜨린 팔에서 폄 한계(shoulderExtensionMax) 안만 닿는다.
        /// </summary>
        public static bool ShoulderCanPoint(Vector3 directionInUpperBody, float shoulderExtensionMax)
        {
            Vector3 d = Vector3.Normalize(directionInUpperBody);
            return d.Z >= 0f || d.Y <= -MathF.Cos(shoulderExtensionMax * DegreesToRadians);
        }

        /// <summary>팔꿈치를 최대로 굽혔을 때 어깨~손바닥 거리 (이보다 가까운 곳에는 손이 못 온다).</summary>
        public static float MinReach(float upperLength, float forearmLength, float handExtra, float elbowFlexMax)
        {
            float b = forearmLength + handExtra;
            float interiorMin = (180f - elbowFlexMax) * DegreesToRadians;
            return MathF.Sqrt(MathF.Max(0f, (upperLength * upperLength) + (b * b) - (2f * upperLength * b * MathF.Cos(interiorMin))));
        }

        /// <summary>
        /// 어깨를 중심으로 도는 손 경로 (팔은 어깨에서 회전한다): 방향은 구면 보간, 어깨와의 거리는 선형 보간.
        /// from·to는 어깨 기준 상대 위치. minRadius보다 어깨에 가까워지지 않는다.
        /// </summary>
        public static Vector3 ArcPoint(Vector3 shoulder, Vector3 from, Vector3 to, float t, float minRadius, Vector3 fallbackAxis)
        {
            float r0 = MathF.Max(from.Length(), minRadius);
            float r1 = MathF.Max(to.Length(), minRadius);
            Vector3 u0 = from.LengthSquared() > 1e-8f ? Vector3.Normalize(from) : Vector3.UnitZ;
            Vector3 u1 = to.LengthSquared() > 1e-8f ? Vector3.Normalize(to) : u0;
            return shoulder + (Slerp(u0, u1, t, fallbackAxis) * (r0 + ((r1 - r0) * t)));
        }

        /// <summary>ArcPoint 경로를 smoothstep으로 갈 때 최고 속도가 peakSpeed 이하가 되는 가장 짧은 시간 (s).</summary>
        public static float ArcDuration(Vector3 from, Vector3 to, float minRadius, float peakSpeed)
        {
            float r0 = MathF.Max(from.Length(), minRadius);
            float r1 = MathF.Max(to.Length(), minRadius);
            Vector3 u0 = from.LengthSquared() > 1e-8f ? Vector3.Normalize(from) : Vector3.UnitZ;
            Vector3 u1 = to.LengthSquared() > 1e-8f ? Vector3.Normalize(to) : u0;
            float angle = MathF.Acos(Math.Clamp(Vector3.Dot(u0, u1), -1f, 1f));
            float pathRate = MathF.Sqrt(MathF.Pow(MathF.Max(r0, r1) * angle, 2f) + MathF.Pow(r1 - r0, 2f));
            return SmoothStepPeak * pathRate / peakSpeed;
        }

        /// <summary>smoothstep의 최고 기울기 (평균의 1.5배).</summary>
        public const float SmoothStepPeak = 1.5f;

        private static Vector3 Slerp(Vector3 a, Vector3 b, float t, Vector3 fallbackAxis)
        {
            float dot = Math.Clamp(Vector3.Dot(a, b), -1f, 1f);
            float angle = MathF.Acos(dot);
            if (angle < 1e-4f)
            {
                return a;
            }

            Vector3 axis = Vector3.Cross(a, b);
            if (axis.LengthSquared() < 1e-8f)
            {
                axis = Vector3.Cross(a, fallbackAxis);
                if (axis.LengthSquared() < 1e-8f)
                {
                    axis = Vector3.Cross(a, Vector3.UnitX);
                }
            }

            var rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle * t);
            return Vector3.Transform(a, rotation);
        }

        /// <summary>0→1 매끄러운 진행 (속도 0에서 시작해 0으로 끝남, 최고 속도 = 평균의 1.5배).</summary>
        public static float SmoothStep(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - (2f * t));
        }

        /// <summary>2차 베지어 곡선의 점.</summary>
        public static Vector3 Bezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float u = 1f - t;
            return (start * (u * u)) + (control * (2f * u * t)) + (end * (t * t));
        }

        /// <summary>2차 베지어 곡선 길이 근사 (16구간).</summary>
        public static float BezierLength(Vector3 start, Vector3 control, Vector3 end)
        {
            const int Segments = 16;
            float length = 0f;
            Vector3 previous = start;
            for (int i = 1; i <= Segments; i++)
            {
                Vector3 point = Bezier(start, control, end, (float)i / Segments);
                length += Vector3.Distance(previous, point);
                previous = point;
            }

            return length;
        }

        /// <summary>점에서 선분까지의 거리.</summary>
        public static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lengthSquared = ab.LengthSquared();
            float t = lengthSquared > 1e-8f ? Math.Clamp(Vector3.Dot(point - a, ab) / lengthSquared, 0f, 1f) : 0f;
            return Vector3.Distance(point, a + (ab * t));
        }
    }
}
