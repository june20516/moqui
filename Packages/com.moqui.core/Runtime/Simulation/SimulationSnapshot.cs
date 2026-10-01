using System.Collections.Generic;
using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 렌더링·HUD용 읽기 전용 상태 (tech/architecture.md §4.3). 게이지·상태를 새로 만들면 반드시 여기에 추가한다:
    /// 재시도 초기화 테스트(RetryTests)가 스냅샷 전체를 비교해 초기화 누락을 잡는다.
    /// </summary>
    public sealed class SimulationSnapshot
    {
        public SimulationSnapshot(int tick, StageOutcome outcome, PlayerSnapshot player, HumanSnapshot human, IReadOnlyList<Vector3> drops, float trappedHeightRemaining, IReadOnlyList<ZoneSnapshot> shadowZones, IReadOnlyList<ZoneSnapshot> windZones, DecoySnapshot decoy = null)
        {
            Decoy = decoy;
            ShadowZones = shadowZones;
            WindZones = windZones;
            Tick = tick;
            Outcome = outcome;
            Drops = drops;
            TrappedHeightRemaining = trappedHeightRemaining;
            Player = player;
            Human = human;
        }

        public int Tick { get; }

        /// <summary>장착한 미끼 마법 상태. 장착하지 않았으면 null.</summary>
        public DecoySnapshot Decoy { get; }

        public StageOutcome Outcome { get; }

        /// <summary>낙하 중인 물방울 위치.</summary>
        public IReadOnlyList<Vector3> Drops { get; }

        /// <summary>Trapped 중 바닥까지 남은 거리 (QTE 높이 게이지).</summary>
        public float TrappedHeightRemaining { get; }

        /// <summary>은신처(Shadow Zone) 목록 (spec/11 §4 은신처 표시).</summary>
        public IReadOnlyList<ZoneSnapshot> ShadowZones { get; }

        /// <summary>바람 영역 (선풍기 원뿔, spec/06·11). M9 전까지 비어 있다.</summary>
        public IReadOnlyList<ZoneSnapshot> WindZones { get; }

        public PlayerSnapshot Player { get; }

        /// <summary>인간이 없는 레벨(샌드박스)이면 null.</summary>
        public HumanSnapshot Human { get; }

        public bool PlayerVisibleToHuman => Human != null && Human.PlayerVisibleToHuman;
    }

    public sealed class PlayerSnapshot
    {
        public PlayerSnapshot(Player player)
        {
            Position = player.Position;
            Velocity = player.Velocity;
            State = player.State;
            Yaw = player.Yaw;
            Stamina = player.Stamina;
            IsExhausted = player.IsExhausted;
            IsHidden = player.IsHidden;
            BloodGauge = player.BloodGauge;
            HasSuckSession = player.SuckSession != null;
            SessionAmount = player.SuckSession?.Amount ?? 0f;
            Humidity = player.Humidity;
            WetRemaining = player.WetRemaining;
            InSteam = player.InSteam;
            EscapePresses = player.EscapePresses;
        }

        public Vector3 Position { get; }

        public Vector3 Velocity { get; }

        public PlayerState State { get; }

        public float Yaw { get; }

        public float Stamina { get; }

        public bool IsExhausted { get; }

        public bool IsHidden { get; }

        /// <summary>흡혈 게이지 (0~100%).</summary>
        public float BloodGauge { get; }

        public bool HasSuckSession { get; }

        public float SessionAmount { get; }

        public float Humidity { get; }

        public float WetRemaining { get; }

        public bool InSteam { get; }

        public int EscapePresses { get; }
    }

    public sealed class HumanSnapshot
    {
        public HumanSnapshot(Human human)
        {
            Id = human.Id;
            Awareness = human.Awareness;
            State = human.State;
            HeadYaw = human.HeadYaw;
            HeadPitch = human.HeadPitch;
            HeadCenter = human.HeadCenter;
            HeadForward = human.HeadForward;
            PlayerVisibleToHuman = human.PlayerVisible;
            PlayerOccluded = human.PlayerOccluded;
            FrenzyMinRemaining = human.FrenzyMinRemaining;
            CalmProgress = human.CalmProgress;
            AttackPhase = human.Attack.Phase;
            AttackKind = human.Attack.Kind;
            AttackTarget = human.Attack.Target;
            AttackRadius = human.Attack.Radius;
            FrenzyCount = human.FrenzyCount;
            BiteMarkCount = human.BiteMarkCount;
            Doze = human.Doze;
            BreathPhase = human.BreathPhase;
            IsExhaling = human.IsExhaling;
            ExhalePosition = human.ExhalePosition;
            ExhaleStrength = human.ExhaleStrength;
            var sites = new List<SkinSiteSnapshot>();
            foreach (var site in human.SkinSites)
            {
                sites.Add(new SkinSiteSnapshot(site));
            }

            SkinSites = sites;
        }

        public string Id { get; }

        public float Awareness { get; }

        public AwarenessState State { get; }

        public float HeadYaw { get; }

        public float HeadPitch { get; }

        public Vector3 HeadCenter { get; }

        public Vector3 HeadForward { get; }

        public bool PlayerVisibleToHuman { get; }

        /// <summary>Yellow Zone 안이지만 장애물에 가려짐 (HUD "가려짐").</summary>
        public bool PlayerOccluded { get; }

        /// <summary>광분 최소 유지 시간 중 남은 초.</summary>
        public float FrenzyMinRemaining { get; }

        /// <summary>진정 진행 0~1.</summary>
        public float CalmProgress { get; }

        public AttackPhase AttackPhase { get; }

        public AttackKind AttackKind { get; }

        public Vector3 AttackTarget { get; }

        public float AttackRadius { get; }

        public int FrenzyCount { get; }

        public int BiteMarkCount { get; }

        public DozeState Doze { get; }

        /// <summary>호흡 주기 위치 (0~1, spec/11 §2).</summary>
        public float BreathPhase { get; }

        public bool IsExhaling { get; }

        public Vector3 ExhalePosition { get; }

        public float ExhaleStrength { get; }

        public IReadOnlyList<SkinSiteSnapshot> SkinSites { get; }
    }

    public sealed class SkinSiteSnapshot
    {
        public SkinSiteSnapshot(SkinSiteState site)
        {
            PartId = site.PartId;
            Type = site.Type;
            Itch = site.Itch;
            HasBiteMark = site.HasBiteMark;
            Position = site.Shape.Center;
            Radius = site.Shape.Radius;
        }

        public string PartId { get; }

        public SkinSiteType Type { get; }

        public float Itch { get; }

        public bool HasBiteMark { get; }

        /// <summary>부위 캡슐 중심 (체온 표시 위치, spec/11 §3).</summary>
        public Vector3 Position { get; }

        public float Radius { get; }
    }
}

namespace Moqui.Core.Simulation
{
    /// <summary>볼륨 영역 하나 (은신처·바람). 박스는 중심·반크기·회전.</summary>
    public sealed class ZoneSnapshot
    {
        public ZoneSnapshot(Moqui.Core.Collision.CollisionShape shape)
        {
            Id = shape.Id;
            Center = shape.Center;
            HalfExtents = shape.HalfExtents;
            Rotation = shape.Rotation;
        }

        public string Id { get; }

        public System.Numerics.Vector3 Center { get; }

        public System.Numerics.Vector3 HalfExtents { get; }

        public System.Numerics.Quaternion Rotation { get; }
    }
}

namespace Moqui.Core.Simulation
{
    /// <summary>미끼 마법 상태 (HUD 쿨타임, 미끼 표현).</summary>
    public sealed class DecoySnapshot
    {
        public DecoySnapshot(bool active, Vector3 position, float cooldownRemaining)
        {
            Active = active;
            Position = position;
            CooldownRemaining = cooldownRemaining;
        }

        public bool Active { get; }

        public Vector3 Position { get; }

        public float CooldownRemaining { get; }
    }
}
