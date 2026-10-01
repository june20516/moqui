using Moqui.Core.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>
    /// 공격 팔 3단계 자세 (spec/10): 예고(어깨 뒤로 치켜듦) → 타격(목표로 내리침) → 회복(어깨로 복귀).
    /// 판정은 Core가 하고 이 계산은 그림에만 쓴다.
    /// </summary>
    public static class HumanArmPose
    {
        /// <summary>치켜든 손이 어깨에서 위로 올라가는 높이 (머리 반지름 배수).</summary>
        public const float WindUpRaise = 2.2f;

        /// <summary>치켜든 손이 목표 반대쪽으로 물러나는 거리 (머리 반지름 배수).</summary>
        public const float WindUpPullBack = 1.2f;

        public static Vector3 WindUp(Vector3 shoulder, Vector3 target, Vector3 up, float headRadius)
        {
            Vector3 away = shoulder - target;
            away -= Vector3.Project(away, up);
            Vector3 pullBack = away.sqrMagnitude > 1e-6f ? away.normalized * (WindUpPullBack * headRadius) : Vector3.zero;
            return shoulder + (up * (WindUpRaise * headRadius)) + pullBack;
        }

        /// <param name="progress">현재 단계 진행률 0~1.</param>
        public static Vector3 Hand(AttackPhase phase, float progress, Vector3 shoulder, Vector3 windUp, Vector3 target)
        {
            float t = Mathf.Clamp01(progress);
            switch (phase)
            {
                case AttackPhase.Telegraph:
                    return Vector3.Lerp(shoulder, windUp, EaseOut(t));
                case AttackPhase.Active:
                    return Vector3.Lerp(windUp, target, EaseIn(t));
                case AttackPhase.Recovery:
                    return Vector3.Lerp(target, shoulder, EaseOut(t));
                default:
                    return shoulder;
            }
        }

        /// <summary>단계 진행률. 단계에 들어온 틱(뷰가 처음 본 틱)과 Core의 단계 종료 틱으로 계산한다.</summary>
        public static float Progress(int tick, int phaseStartTick, int phaseEndTick)
        {
            int length = phaseEndTick - phaseStartTick;
            return length <= 0 ? 1f : Mathf.Clamp01((float)(tick - phaseStartTick) / length);
        }

        public static int PhaseEndTick(HumanAttack attack)
        {
            switch (attack.Phase)
            {
                case AttackPhase.Telegraph:
                    return attack.TelegraphEndTick;
                case AttackPhase.Active:
                    return attack.ActiveEndTick;
                case AttackPhase.Recovery:
                    return attack.RecoveryEndTick;
                default:
                    return 0;
            }
        }

        private static float EaseOut(float t) => 1f - ((1f - t) * (1f - t));

        private static float EaseIn(float t) => t * t;
    }
}
