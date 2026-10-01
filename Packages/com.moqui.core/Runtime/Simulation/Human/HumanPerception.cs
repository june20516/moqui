using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>한 틱 동안 인간이 받은 자극의 합 (spec/02 §1~2).</summary>
    public struct HumanPerception
    {
        /// <summary>Yellow Zone 시각 증가율 (/s).</summary>
        public float VisionRate;

        /// <summary>비행 소음 + 귀 근접 구역 증가율 (/s).</summary>
        public float HearingRate;

        /// <summary>대시 소음 같은 즉시 증가량.</summary>
        public float InstantGain;

        /// <summary>D-029의 "보임".</summary>
        public bool PlayerSeen;

        /// <summary>시야 확보된 Red Zone 진입: 박수 공격과 광분.</summary>
        public bool RedZoneTriggered;

        public bool InEarZone;

        public bool HasStimulus;

        public Vector3 StimulusPosition;

        public void AddStimulus(Vector3 position)
        {
            HasStimulus = true;
            StimulusPosition = position;
        }
    }
}
