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
        }

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
        }

        /// <summary>플레이어 중심에서 표면까지 이 거리 이내면 부착할 수 있다 (spec/03).</summary>
        public float AttachRange { get; }

        public float DetachOffset { get; }

        /// <summary>가려움이 이 값에 도달하면 확률과 무관하게 즉시 반응한다 (spec/02 §5).</summary>
        public float ItchThreshold { get; }
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
    }
}
