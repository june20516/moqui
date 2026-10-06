using System;
using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>부위 유형별 민감도와 혈액량 (spec/04 §1).</summary>
    public sealed class SiteSettings
    {
        private readonly float[] _sensitivity;
        private readonly float[] _bloodAmount;

        public SiteSettings(Tuning tuning)
        {
            var types = (SkinSiteType[])Enum.GetValues(typeof(SkinSiteType));
            _sensitivity = new float[types.Length];
            _bloodAmount = new float[types.Length];
            foreach (var type in types)
            {
                string key = $"site.{KeyName(type)}";
                _sensitivity[(int)type] = tuning.GetFloat($"{key}.sensitivity");
                _bloodAmount[(int)type] = tuning.GetFloat($"{key}.bloodAmount");
            }
        }

        public float Sensitivity(SkinSiteType type)
        {
            return _sensitivity[(int)type];
        }

        public float BloodAmount(SkinSiteType type)
        {
            return _bloodAmount[(int)type];
        }

        /// <summary>tuning 키 이름: 첫 글자만 소문자 (FootTop → footTop).</summary>
        public static string KeyName(SkinSiteType type)
        {
            string name = type.ToString();
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }

    public sealed class ReactionSettings
    {
        public ReactionSettings(Tuning tuning)
        {
            LandChance = tuning.GetFloat("reaction.landChance");
            BaseRate = tuning.GetFloat("reaction.baseRate");
            ItchRate = tuning.GetFloat("reaction.itchRate");
            EarRate = tuning.GetFloat("reaction.earRate");
        }

        public float LandChance { get; }

        public float BaseRate { get; }

        public float ItchRate { get; }

        public float EarRate { get; }
    }

    public sealed class HumanMotionSettings
    {
        public HumanMotionSettings(Tuning tuning)
        {
            ActionInterval = tuning.GetRange("human.actionInterval");
            DislodgeSpeed = tuning.GetFloat("human.dislodgeSpeed");
            DislodgePush = tuning.GetFloat("human.dislodgePush");
            DislodgeStun = tuning.GetFloat("human.dislodgeStun");
            AlarmShare = tuning.GetFloat("human.alarmShare");
        }

        /// <summary>한 사람이 광분하면 다른 사람의 경계를 이 값까지 올린다 (광분 전염, M14).</summary>
        public float AlarmShare { get; }

        public FloatRange ActionInterval { get; }

        public float DislodgeSpeed { get; }

        public float DislodgePush { get; }

        public float DislodgeStun { get; }
    }

    public sealed class AttachSettings
    {
        public AttachSettings(Tuning tuning)
        {
            AttachRange = tuning.GetFloat("suck.attachRange");
            DetachOffset = tuning.GetFloat("attach.detachOffset");
            ItchThreshold = tuning.GetFloat("suck.itchThreshold");
            SnapRange = tuning.GetFloat("attach.snapRange");
            AutoLandAlign = tuning.GetFloat("attach.autoLandAlign");
        }

        /// <summary>표면에 닿았다고 보는 거리: 플레이어 중심에서 표면까지 이 거리 이내 (spec/03, 정밀 비행 자동 착지).</summary>
        public float AttachRange { get; }

        /// <summary>F 착지가 닿는 거리: 이 안의 가장 가까운 표면으로 미끄러져 붙는다 (gulf §2, D-066).</summary>
        public float SnapRange { get; }

        /// <summary>정밀 비행 자동 착지: 진행 방향이 표면 쪽(−법선)과 이루는 코사인이 이 값 이상이어야 한다.</summary>
        public float AutoLandAlign { get; }

        public float DetachOffset { get; }

        /// <summary>가려움이 이 값에 도달하면 확률과 무관하게 즉시 반응한다 (spec/02 §5).</summary>
        public float ItchThreshold { get; }
    }

    public sealed class SuckSettings
    {
        public SuckSettings(Tuning tuning)
        {
            RateStart = tuning.GetFloat("suck.rateStart");
            RateMax = tuning.GetFloat("suck.rateMax");
            RampTime = tuning.GetFloat("suck.rampTime");
            ItchRate = tuning.GetFloat("suck.itchRate");
            ItchDecay = tuning.GetFloat("suck.itchDecay");
            ItchThreshold = tuning.GetFloat("suck.itchThreshold");
            YankItch = tuning.GetFloat("suck.yankItch");
            SatietyMinSpeedMul = tuning.GetFloat("satiety.minSpeedMul");
            SatietyMinDashMul = tuning.GetFloat("satiety.minDashMul");
        }

        /// <summary>세션 시작 흡혈 속도 (%/s).</summary>
        public float RateStart { get; }

        public float RateMax { get; }

        /// <summary>시작 → 최대 속도까지 걸리는 세션 흡혈 시간 (s).</summary>
        public float RampTime { get; }

        public float ItchRate { get; }

        public float ItchDecay { get; }

        public float ItchThreshold { get; }

        /// <summary>흡혈 중 대시로 지팡이를 억지로 뽑을 때 그 부위 가려움에 더하는 양 (gulf §1, D-066).</summary>
        public float YankItch { get; }

        public float SatietyMinSpeedMul { get; }

        public float SatietyMinDashMul { get; }
    }

    public sealed class BiteMarkSettings
    {
        public BiteMarkSettings(Tuning tuning)
        {
            MinAmount = tuning.GetFloat("biteMark.minAmount");
            AwarenessBump = tuning.GetFloat("biteMark.awarenessBump");
            GainMulPerBite = tuning.GetFloat("biteMark.gainMulPerBite");
            DecayDivPerBite = tuning.GetFloat("biteMark.decayDivPerBite");
            FloorPerBite = tuning.GetFloat("biteMark.floorPerBite");
            FloorMax = tuning.GetFloat("biteMark.floorMax");
            ReactionMulPerBite = tuning.GetFloat("biteMark.reactionMulPerBite");
        }

        public float MinAmount { get; }

        public float AwarenessBump { get; }

        public float GainMulPerBite { get; }

        public float DecayDivPerBite { get; }

        public float FloorPerBite { get; }

        public float FloorMax { get; }

        public float ReactionMulPerBite { get; }

        /// <summary>반응 확률 배율 1 + reactionMulPerBite × n.</summary>
        public float ReactionMultiplier(int biteCount)
        {
            return 1f + (ReactionMulPerBite * biteCount);
        }

        /// <summary>경계 증가 배율 1 + gainMulPerBite × n.</summary>
        public float GainMultiplier(int biteCount)
        {
            return 1f + (GainMulPerBite * biteCount);
        }

        /// <summary>경계 감소를 나누는 값 1 + decayDivPerBite × n (감소 배율은 그 역수).</summary>
        public float DecayDivisor(int biteCount)
        {
            return 1f + (DecayDivPerBite * biteCount);
        }

        /// <summary>경계 하한 min(floorPerBite × n, floorMax).</summary>
        public float Floor(int biteCount)
        {
            return Math.Min(FloorPerBite * biteCount, FloorMax);
        }
    }
}
