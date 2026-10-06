using System;

namespace Moqui.Core.Simulation
{
    /// <summary>경계가 오른 이유 (gulf §10 "왜 들켰나", D-067).</summary>
    public enum AwarenessCause
    {
        /// <summary>기록된 원인 없음.</summary>
        None,

        /// <summary>눈: 시야(Yellow/Red Zone)에 보임.</summary>
        Sight,

        /// <summary>귀: 비행·대시·모기장 소음, 귀 근접 구역.</summary>
        Hearing,

        /// <summary>가려움: 흡혈을 마친 자리의 물린 자국을 알아챔.</summary>
        Itch,

        /// <summary>시선: 흡혈 중 이벤트에서 꿈틀거리는 모기를 봄.</summary>
        Glance,

        /// <summary>함께 있는 사람이 광분해서 알려 줌.</summary>
        Alarm,
    }

    /// <summary>
    /// 원인별 경계 기여의 짧은 기억: 기여량을 더하고 시간이 지나면 지수적으로 잊는다(시정수 awareness.causeMemory).
    /// 광분에 들어가는 순간 가장 큰 원인이 "왜 들켰나"다.
    /// </summary>
    public sealed class AwarenessCauseMemory
    {
        private readonly float[] _weights = new float[Enum.GetValues(typeof(AwarenessCause)).Length];

        public float Weight(AwarenessCause cause) => _weights[(int)cause];

        public void Add(AwarenessCause cause, float amount)
        {
            if (amount > 0f && cause != AwarenessCause.None)
            {
                _weights[(int)cause] += amount;
            }
        }

        public void Decay(float deltaTime, float memorySeconds)
        {
            float keep = memorySeconds > 0f ? MathF.Exp(-deltaTime / memorySeconds) : 0f;
            for (int i = 0; i < _weights.Length; i++)
            {
                _weights[i] *= keep;
            }
        }

        /// <summary>지금 가장 큰 원인. 모두 0이면 None.</summary>
        public AwarenessCause Dominant
        {
            get
            {
                var best = AwarenessCause.None;
                float bestWeight = 0f;
                for (int i = 1; i < _weights.Length; i++)
                {
                    if (_weights[i] > bestWeight)
                    {
                        bestWeight = _weights[i];
                        best = (AwarenessCause)i;
                    }
                }

                return best;
            }
        }
    }
}
