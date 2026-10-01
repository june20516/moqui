using System;
using System.Collections.Generic;
using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>인간 수정자 (spec/06). 수치에 배율을 곱하거나 감지를 끄는 방식으로만 작동한다.</summary>
    public enum HumanModifier
    {
        Doze,
        Drunk,
    }

    /// <summary>
    /// 평온 상태의 둘러보기 (spec/07 Stage 2: "6~10초마다 좌 또는 우로 60° 2초간 둘러봄"). 레벨 데이터 human.idle.glance.
    /// </summary>
    public sealed class IdleGlance
    {
        public IdleGlance(FloatRange interval, float angle, float duration)
        {
            if (interval.Min <= 0f || interval.Max < interval.Min)
            {
                throw new ArgumentOutOfRangeException(nameof(interval));
            }

            Interval = interval;
            Angle = angle;
            Duration = duration;
        }

        public FloatRange Interval { get; }

        /// <summary>몸 정면 기준 좌우 회전 각도 (도).</summary>
        public float Angle { get; }

        public float Duration { get; }
    }

    /// <summary>레벨 데이터의 인간 특성: 수정자, 스프레이 사용 여부, 둘러보기.</summary>
    public sealed class HumanTraits
    {
        public static readonly HumanTraits None = new HumanTraits(Array.Empty<HumanModifier>(), false, null);

        public HumanTraits(IReadOnlyList<HumanModifier> modifiers, bool canSpray, IdleGlance glance)
        {
            Modifiers = modifiers ?? Array.Empty<HumanModifier>();
            CanSpray = canSpray;
            Glance = glance;
        }

        public IReadOnlyList<HumanModifier> Modifiers { get; }

        public bool CanSpray { get; }

        public IdleGlance Glance { get; }

        public bool Has(HumanModifier modifier)
        {
            foreach (var candidate in Modifiers)
            {
                if (candidate == modifier)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
