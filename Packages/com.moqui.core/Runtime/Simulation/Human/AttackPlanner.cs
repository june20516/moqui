using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>목표를 칠 팔과 그 팔이 닿도록 취할 자세.</summary>
    public readonly struct AttackPlan
    {
        public AttackPlan(int arm, PostureState posture, PostureLevel level, float postureTime)
        {
            Arm = arm;
            Posture = posture;
            Level = level;
            PostureTime = postureTime;
        }

        public int Arm { get; }

        public PostureState Posture { get; }

        public PostureLevel Level { get; }

        /// <summary>현재 자세에서 이 자세까지 걸리는 시간 (s).</summary>
        public float PostureTime { get; }
    }

    /// <summary>
    /// 사람 몸으로 목표에 닿는 방법을 고른다 (spec/02, D-052·D-053): 팔만 → 상체 기울이기 → 몸 돌리기 → 일어서기 순서로,
    /// 레벨의 maxPosture까지 필요한 만큼만 몸을 움직인다. 팔은 늘어나지 않고 어깨 가동 범위 밖으로는 뻗지 않는다.
    /// </summary>
    public static class AttackPlanner
    {
        private const float LeanStep = 5f;

        /// <summary>손이 닿는다고 보는 팔 길이 비율 (팔을 끝까지 펴서 치지는 않는다).</summary>
        private const float ReachMargin = 0.97f;

        private const float DegreesToRadians = MathF.PI / 180f;

        /// <param name="preferredArm">먼저 시도할 팔 (−1이면 가까운 팔부터).</param>
        /// <param name="excludedArm">쓸 수 없는 팔 (모기가 그 팔에 앉아 있으면 그 손으로는 못 친다). −1이면 없음.</param>
        public static bool TryPlan(Human human, Vector3 target, int preferredArm, int excludedArm, out AttackPlan plan)
        {
            var arms = Enumerable.Range(0, human.Rig.Arms.Count)
                .Where(arm => arm != excludedArm)
                .OrderBy(arm => arm == preferredArm ? 0 : 1)
                .ThenBy(arm => Vector3.Distance(human.Shoulder(arm), target))
                .ToList();

            for (var level = PostureLevel.Arm; level <= human.Definition.MaxPosture; level++)
            {
                foreach (int arm in arms)
                {
                    if (TryLevel(human, arm, target, level, out var posture))
                    {
                        plan = new AttackPlan(arm, posture, level, TransitionTime(human.Pose.Posture, posture, human.Body));
                        return true;
                    }
                }
            }

            plan = default;
            return false;
        }

        /// <summary>그 자세에서 그 팔로 목표에 손이 닿는가 (거리 + 어깨 가동 범위).</summary>
        public static bool CanReach(Human human, int arm, PostureState posture, Vector3 target)
        {
            Vector3 shoulder = human.ShoulderFor(arm, posture);
            Vector3 toTarget = target - shoulder;
            if (toTarget.Length() > human.ArmReach(arm) * ReachMargin)
            {
                return false;
            }

            var upperWorld = Quaternion.Concatenate(Human.UpperRotation(posture), human.BodyRotation);
            Vector3 local = Vector3.Transform(toTarget, Quaternion.Conjugate(upperWorld));
            local.X *= human.Rig.Arms[arm].Side;
            return toTarget.LengthSquared() < 1e-4f || BodyKinematics.ShoulderCanPoint(local, human.Body.ShoulderExtensionMax);
        }

        /// <summary>현재 자세에서 목표 자세까지 걸리는 시간: 기울기·비틀기·일어섬 중 가장 오래 걸리는 것.</summary>
        public static float TransitionTime(PostureState from, PostureState to, BodySettings body)
        {
            float lean = Math.Abs(to.LeanAngle - from.LeanAngle) / body.MaxLeanAngle * body.LeanTime;
            float twist = Math.Abs(to.Twist - from.Twist) / body.MaxTwist * body.TurnTime;
            float rise = Math.Abs(to.Rise - from.Rise) * body.RiseTime;
            return Math.Max(lean, Math.Max(twist, rise));
        }

        private static bool TryLevel(Human human, int arm, Vector3 target, PostureLevel level, out PostureState posture)
        {
            var body = human.Body;
            Vector3 hipToTarget = human.ToLocal(target) - human.Rig.HipLocal;
            var horizontal = new Vector3(hipToTarget.X, 0f, hipToTarget.Z);
            Vector3 leanDirection = horizontal.LengthSquared() > 1e-6f ? Vector3.Normalize(horizontal) : Vector3.UnitZ;
            float yaw = MathF.Atan2(horizontal.X, horizontal.Z) / DegreesToRadians;

            posture = PostureState.Rest;
            posture.LeanDirection = leanDirection;
            posture.Twist = level >= PostureLevel.Turn ? Math.Clamp(yaw, -body.MaxTwist, body.MaxTwist) : 0f;
            posture.Rise = level >= PostureLevel.Rise ? 1f : 0f;
            if (level == PostureLevel.Arm)
            {
                return CanReach(human, arm, posture, target);
            }

            // 기울기는 닿는 데 필요한 만큼만 (사람은 필요 이상으로 몸을 던지지 않는다).
            for (float angle = level == PostureLevel.Lean ? LeanStep : 0f; angle <= body.MaxLeanAngle + 1e-3f; angle += LeanStep)
            {
                posture.LeanAngle = Math.Min(angle, body.MaxLeanAngle);
                if (CanReach(human, arm, posture, target))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
