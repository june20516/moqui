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
        public SimulationSnapshot(int tick, PlayerSnapshot player, HumanSnapshot human)
        {
            Tick = tick;
            Player = player;
            Human = human;
        }

        public int Tick { get; }

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
        }

        public Vector3 Position { get; }

        public Vector3 Velocity { get; }

        public PlayerState State { get; }

        public float Yaw { get; }

        public float Stamina { get; }

        public bool IsExhausted { get; }

        public bool IsHidden { get; }
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
            AttackPhase = human.Attack.Phase;
            AttackKind = human.Attack.Kind;
            AttackTarget = human.Attack.Target;
            AttackRadius = human.Attack.Radius;
            FrenzyCount = human.FrenzyCount;
            BiteMarkCount = human.BiteMarkCount;
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

        public AttackPhase AttackPhase { get; }

        public AttackKind AttackKind { get; }

        public Vector3 AttackTarget { get; }

        public float AttackRadius { get; }

        public int FrenzyCount { get; }

        public int BiteMarkCount { get; }

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
        }

        public string PartId { get; }

        public SkinSiteType Type { get; }

        public float Itch { get; }

        public bool HasBiteMark { get; }
    }
}
