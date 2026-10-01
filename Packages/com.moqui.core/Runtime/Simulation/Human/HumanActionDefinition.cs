using System;
using System.Collections.Generic;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>동작 중 몸 캡슐 하나의 움직임: 최고점에서 끝점 A·B의 이동량 (인간 루트 기준 로컬, cm).</summary>
    public sealed class PartMotionDefinition
    {
        public PartMotionDefinition(string partId, Vector3 offsetA, Vector3 offsetB)
        {
            PartId = partId ?? throw new ArgumentNullException(nameof(partId));
            OffsetA = offsetA;
            OffsetB = offsetB;
        }

        public string PartId { get; }

        public Vector3 OffsetA { get; }

        public Vector3 OffsetB { get; }
    }

    /// <summary>
    /// 무작위 동작 하나 (spec/02 §6, 레벨 데이터 human.actions[]).
    /// 움직임 곡선은 sin(π·t/Duration): 최고점까지 갔다가 제자리로 돌아온다. 최고 속도 = 이동량 × π / Duration.
    /// </summary>
    public sealed class HumanActionDefinition
    {
        public HumanActionDefinition(string name, float weight, float duration, IReadOnlyList<PartMotionDefinition> motions)
        {
            if (weight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Weight = weight;
            Duration = duration;
            Motions = motions ?? throw new ArgumentNullException(nameof(motions));
        }

        public string Name { get; }

        public float Weight { get; }

        public float Duration { get; }

        public IReadOnlyList<PartMotionDefinition> Motions { get; }

        /// <summary>시작 후 elapsed초에서의 이동 비율 (0~1~0).</summary>
        public float Profile(float elapsed)
        {
            float t = Math.Clamp(elapsed / Duration, 0f, 1f);
            return MathF.Sin(MathF.PI * t);
        }
    }
}
