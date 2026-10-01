using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>tuning 값을 Core 시스템이 쓰는 명시적 DTO로 옮긴다. 키 이름은 spec/tuning.md와 같다.</summary>
    public sealed class GameSettings
    {
        private GameSettings(Tuning tuning)
        {
            World = new WorldSettings(tuning);
            Player = new PlayerSettings(tuning);
            Flight = new FlightSettings(tuning);
            Dash = new DashSettings(tuning);
            Stamina = new StaminaSettings(tuning);
            Hiding = new HidingSettings(tuning);
            Noise = new NoiseSettings(tuning);
            Hearing = new HearingSettings(tuning);
            Vision = new VisionSettings(tuning);
            Awareness = new AwarenessSettings(tuning);
            Head = new HeadSettings(tuning);
            Frenzy = new FrenzySettings(tuning);
            Attack = new AttackSettings(tuning);
            Sites = new SiteSettings(tuning);
            Reaction = new ReactionSettings(tuning);
            HumanMotion = new HumanMotionSettings(tuning);
            Attach = new AttachSettings(tuning);
            BiteMark = new BiteMarkSettings(tuning);
            Suck = new SuckSettings(tuning);
            Water = new WaterSettings(tuning);
            Humid = new HumidSettings(tuning);
            Doze = new DozeSettings(tuning);
            Breath = new BreathSettings(tuning);
            Decoy = new DecoySettings(tuning);
            Fan = new FanSettings(tuning);
            Toxin = new ToxinSettings(tuning);
            Drunk = new DrunkSettings(tuning);
            WebStruggleTime = tuning.GetFloat("web.struggleTime");
        }

        public FanSettings Fan { get; }

        public ToxinSettings Toxin { get; }

        public DrunkSettings Drunk { get; }

        /// <summary>거미줄에 걸린 뒤 사망까지 (spec/06).</summary>
        public float WebStruggleTime { get; }

        /// <summary>액티브 스킬 미끼 마법 (spec/09).</summary>
        public DecoySettings Decoy { get; }

        public DozeSettings Doze { get; }

        public BreathSettings Breath { get; }

        public WaterSettings Water { get; }

        public HumidSettings Humid { get; }

        public SuckSettings Suck { get; }

        public SiteSettings Sites { get; }

        public ReactionSettings Reaction { get; }

        public HumanMotionSettings HumanMotion { get; }

        public AttachSettings Attach { get; }

        public BiteMarkSettings BiteMark { get; }

        public NoiseSettings Noise { get; }

        public HearingSettings Hearing { get; }

        public VisionSettings Vision { get; }

        public AwarenessSettings Awareness { get; }

        public HeadSettings Head { get; }

        public FrenzySettings Frenzy { get; }

        public AttackSettings Attack { get; }

        public WorldSettings World { get; }

        public PlayerSettings Player { get; }

        public FlightSettings Flight { get; }

        public DashSettings Dash { get; }

        public StaminaSettings Stamina { get; }

        public HidingSettings Hiding { get; }

        public static GameSettings FromTuning(Tuning tuning)
        {
            return new GameSettings(tuning);
        }
    }

    public sealed class WorldSettings
    {
        public WorldSettings(Tuning tuning)
        {
            Gravity = tuning.GetFloat("world.gravity");
        }

        /// <summary>물체용 중력 가속도 (u/s²).</summary>
        public float Gravity { get; }
    }

    public sealed class PlayerSettings
    {
        public PlayerSettings(Tuning tuning)
        {
            CollisionRadius = tuning.GetFloat("player.collisionRadius");
        }

        public float CollisionRadius { get; }
    }

    public sealed class FlightSettings
    {
        public FlightSettings(Tuning tuning)
        {
            Speed = tuning.GetFloat("flight.speed");
            VerticalSpeed = tuning.GetFloat("flight.verticalSpeed");
            PrecisionSpeedMul = tuning.GetFloat("flight.precisionSpeedMul");
            AccelTime = tuning.GetFloat("flight.accelTime");
            DecelTime = tuning.GetFloat("flight.decelTime");
        }

        public float Speed { get; }

        public float VerticalSpeed { get; }

        public float PrecisionSpeedMul { get; }

        public float AccelTime { get; }

        public float DecelTime { get; }
    }

    public sealed class DashSettings
    {
        public DashSettings(Tuning tuning)
        {
            Distance = tuning.GetFloat("dash.distance");
            Duration = tuning.GetFloat("dash.duration");
            StaminaCost = tuning.GetFloat("dash.staminaCost");
            Cooldown = tuning.GetFloat("dash.cooldown");
            NoiseRadius = tuning.GetFloat("dash.noiseRadius");
            NoiseAwareness = tuning.GetFloat("dash.noiseAwareness");
            ChainWindow = tuning.GetFloat("dash.chainWindow");
        }

        public float Distance { get; }

        public float Duration { get; }

        public float StaminaCost { get; }

        public float Cooldown { get; }

        public float NoiseRadius { get; }

        public float NoiseAwareness { get; }

        public float ChainWindow { get; }
    }

    public sealed class StaminaSettings
    {
        public StaminaSettings(Tuning tuning)
        {
            Max = tuning.GetFloat("stamina.max");
            RegenRate = tuning.GetFloat("stamina.regenRate");
            RegenDelay = tuning.GetFloat("stamina.regenDelay");
            ExhaustedDuration = tuning.GetFloat("stamina.exhaustedDuration");
            ExhaustedSpeedMul = tuning.GetFloat("stamina.exhaustedSpeedMul");
        }

        public float Max { get; }

        public float RegenRate { get; }

        public float RegenDelay { get; }

        public float ExhaustedDuration { get; }

        public float ExhaustedSpeedMul { get; }
    }

    public sealed class HidingSettings
    {
        public HidingSettings(Tuning tuning)
        {
            DebuffRecoveryMul = tuning.GetFloat("hiding.debuffRecoveryMul");
            CueRange = tuning.GetFloat("hiding.cueRange");
        }

        /// <summary>은신처 표시 거리이자 "도망칠 곳 보장" 검사 반경 (spec/07, spec/11).</summary>
        public float CueRange { get; }

        /// <summary>숨은 상태에서 디버프(중독·젖은 날개·습기·탈진)가 줄어드는 속도 배율 (D-018).</summary>
        public float DebuffRecoveryMul { get; }
    }
}
