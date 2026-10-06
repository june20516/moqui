using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    public sealed class NoiseSettings
    {
        public NoiseSettings(Tuning tuning)
        {
            FlightRadius = tuning.GetFloat("noise.flightRadius");
            FlightAwarenessRate = tuning.GetFloat("noise.flightAwarenessRate");
            PrecisionRadiusMul = tuning.GetFloat("noise.precisionRadiusMul");
            AttachedRadius = tuning.GetFloat("noise.attachedRadius");
        }

        public float FlightRadius { get; }

        public float FlightAwarenessRate { get; }

        public float PrecisionRadiusMul { get; }

        public float AttachedRadius { get; }
    }

    public sealed class HearingSettings
    {
        public HearingSettings(Tuning tuning)
        {
            NearMul = tuning.GetFloat("hearing.nearMul");
            FarMul = tuning.GetFloat("hearing.farMul");
            EarZoneRadius = tuning.GetFloat("hearing.earZoneRadius");
            EarZoneRate = tuning.GetFloat("hearing.earZoneRate");
        }

        public float NearMul { get; }

        public float FarMul { get; }

        public float EarZoneRadius { get; }

        public float EarZoneRate { get; }
    }

    public sealed class VisionSettings
    {
        public VisionSettings(Tuning tuning)
        {
            YellowHalfAngle = tuning.GetFloat("vision.yellow.halfAngle");
            YellowRange = tuning.GetFloat("vision.yellow.range");
            YellowRateNear = tuning.GetFloat("vision.yellow.rateNear");
            YellowRateFar = tuning.GetFloat("vision.yellow.rateFar");
            RedHalfAngle = tuning.GetFloat("vision.red.halfAngle");
            RedRange = tuning.GetFloat("vision.red.range");
            AttachedMul = tuning.GetFloat("vision.attachedMul");
            ShadowMul = tuning.GetFloat("vision.shadowMul");
            LosCheckInterval = tuning.GetFloat("vision.losCheckInterval");
        }

        public float YellowHalfAngle { get; }

        public float YellowRange { get; }

        public float YellowRateNear { get; }

        public float YellowRateFar { get; }

        public float RedHalfAngle { get; }

        public float RedRange { get; }

        public float AttachedMul { get; }

        public float ShadowMul { get; }

        public float LosCheckInterval { get; }
    }

    public sealed class AwarenessSettings
    {
        public AwarenessSettings(Tuning tuning)
        {
            SuspiciousEnter = tuning.GetFloat("awareness.suspiciousEnter");
            SuspiciousExit = tuning.GetFloat("awareness.suspiciousExit");
            FrenzyEnter = tuning.GetFloat("awareness.frenzyEnter");
            DecayDelay = tuning.GetFloat("awareness.decayDelay");
            DecayRate = tuning.GetFloat("awareness.decayRate");
            ShadowDecayRate = tuning.GetFloat("awareness.shadowDecayRate");
        }

        public float SuspiciousEnter { get; }

        public float SuspiciousExit { get; }

        /// <summary>경계 상한이자 광분 진입선.</summary>
        public float FrenzyEnter { get; }

        public float DecayDelay { get; }

        public float DecayRate { get; }

        public float ShadowDecayRate { get; }
    }

    public sealed class HeadSettings
    {
        public HeadSettings(Tuning tuning)
        {
            IdleTurnSpeed = tuning.GetFloat("head.idleTurnSpeed");
            SuspiciousTurnSpeed = tuning.GetFloat("head.suspiciousTurnSpeed");
            FrenzyTurnSpeed = tuning.GetFloat("head.frenzyTurnSpeed");
            YawLimit = tuning.GetFloat("head.yawLimit");
            PitchLimit = tuning.GetFloat("head.pitchLimit");
            SuspiciousStareTime = tuning.GetFloat("head.suspiciousStareTime");
            SearchAngle = tuning.GetFloat("head.searchAngle");
        }

        public float IdleTurnSpeed { get; }

        public float SuspiciousTurnSpeed { get; }

        public float FrenzyTurnSpeed { get; }

        public float YawLimit { get; }

        public float PitchLimit { get; }

        public float SuspiciousStareTime { get; }

        public float SearchAngle { get; }
    }

    public sealed class FrenzySettings
    {
        public FrenzySettings(Tuning tuning)
        {
            MinDuration = tuning.GetFloat("frenzy.minDuration");
            CalmTime = tuning.GetFloat("frenzy.calmTime");
            ExitValue = tuning.GetFloat("frenzy.exitValue");
            SlapInterval = tuning.GetFloat("frenzy.slapInterval");
            SlapTelegraph = tuning.GetFloat("frenzy.slapTelegraph");
            BlindSwatRadius = tuning.GetFloat("frenzy.blindSwatRadius");
            BlindSwatInterval = tuning.GetRange("frenzy.blindSwatInterval");
            ReactionMul = tuning.GetFloat("frenzy.reactionMul");
        }

        public float MinDuration { get; }

        public float CalmTime { get; }

        public float ExitValue { get; }

        public float SlapInterval { get; }

        public float SlapTelegraph { get; }

        public float BlindSwatRadius { get; }

        public FloatRange BlindSwatInterval { get; }

        public float ReactionMul { get; }
    }

    public sealed class AttackSettings
    {
        public AttackSettings(Tuning tuning)
        {
            ClapTelegraph = tuning.GetFloat("attack.clap.telegraph");
            ClapRadius = tuning.GetFloat("attack.clap.radius");
            ClapOffset = tuning.GetFloat("attack.clap.offset");
            ClapRecovery = tuning.GetFloat("attack.clap.recovery");
            SlapRadius = tuning.GetFloat("attack.slap.radius");
            SlapRecovery = tuning.GetFloat("attack.slap.recovery");
            SelfSlapTelegraph = tuning.GetFloat("attack.selfSlap.telegraph");
            SelfSlapRadius = tuning.GetFloat("attack.selfSlap.radius");
            SelfSlapRecovery = tuning.GetFloat("attack.selfSlap.recovery");
            SwatterLength = tuning.GetFloat("attack.swatter.length");
            SwatterRadius = tuning.GetFloat("attack.swatter.radius");
            HandPeakSpeedFrenzy = tuning.GetFloat("attack.handPeakSpeed.frenzy");
            HandPeakSpeedReaction = tuning.GetFloat("attack.handPeakSpeed.reaction");
            HandPeakSpeedDrunk = tuning.GetFloat("attack.handPeakSpeed.drunk");
            MinTelegraph = tuning.GetFloat("attack.minTelegraph");
        }

        /// <summary>광분 공격(손바닥·맹목 휘두르기·박수)의 손 최고 속도 (u/s).</summary>
        public float HandPeakSpeedFrenzy { get; }

        /// <summary>반사적으로 자기 몸을 칠 때의 손 최고 속도 (u/s).</summary>
        public float HandPeakSpeedReaction { get; }

        /// <summary>취한 사람의 무작위 휘두르기 손 최고 속도 (u/s).</summary>
        public float HandPeakSpeedDrunk { get; }

        /// <summary>예고(치켜듦) 최소 시간 = 사람 반응 시간 하한 (s).</summary>
        public float MinTelegraph { get; }

        public float ClapTelegraph { get; }

        public float ClapRadius { get; }

        public float ClapOffset { get; }

        public float ClapRecovery { get; }

        public float SlapRadius { get; }

        public float SlapRecovery { get; }

        public float SelfSlapTelegraph { get; }

        public float SelfSlapRadius { get; }

        public float SelfSlapRecovery { get; }

        /// <summary>전기 모기채 (M14): 손목 앞 채 길이와 판정 반경.</summary>
        public float SwatterLength { get; }

        public float SwatterRadius { get; }
    }

    /// <summary>인간 몸 (knowledge/human-arm-motion.md): 손 길이, 관절 한계, 자세 전환 범위·시간.</summary>
    public sealed class BodySettings
    {
        public BodySettings(Tuning tuning)
        {
            HandReachExtra = tuning.GetFloat("human.handReachExtra");
            ElbowFlexMax = tuning.GetFloat("human.elbowFlexMax");
            ShoulderExtensionMax = tuning.GetFloat("human.shoulderExtensionMax");
            MaxLeanAngle = tuning.GetFloat("posture.maxLeanAngle");
            MaxTwist = tuning.GetFloat("posture.maxTwist");
            RiseLift = tuning.GetFloat("posture.riseLift");
            RiseForward = tuning.GetFloat("posture.riseForward");
            LeanTime = tuning.GetFloat("posture.leanTime");
            TurnTime = tuning.GetFloat("posture.turnTime");
            RiseTime = tuning.GetFloat("posture.riseTime");
        }

        /// <summary>손목 → 손바닥 중심 거리 (u).</summary>
        public float HandReachExtra { get; }

        /// <summary>팔꿈치 최대 굽힘 (도).</summary>
        public float ElbowFlexMax { get; }

        /// <summary>어깨 폄(뒤로) 한계 (도).</summary>
        public float ShoulderExtensionMax { get; }

        /// <summary>상체 최대 기울기 (도).</summary>
        public float MaxLeanAngle { get; }

        /// <summary>상체 최대 비틀기 (도).</summary>
        public float MaxTwist { get; }

        /// <summary>완전히 일어설 때 골반이 오르는 높이 (u).</summary>
        public float RiseLift { get; }

        /// <summary>완전히 일어설 때 골반이 앞으로 나오는 거리 (u).</summary>
        public float RiseForward { get; }

        /// <summary>최대 기울기까지 걸리는 시간 (s).</summary>
        public float LeanTime { get; }

        /// <summary>최대 비틀기까지 걸리는 시간 (s).</summary>
        public float TurnTime { get; }

        /// <summary>완전히 일어서는 시간 (s).</summary>
        public float RiseTime { get; }
    }
}
