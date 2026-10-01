using System.Linq;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Simulation;
using UnityEngine;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>하단 중앙 상호작용 프롬프트 종류 (spec/08).</summary>
    public enum HudPrompt
    {
        None,
        Attach,
        Suck,
        Detach,
        Escape,
    }

    /// <summary>HUD가 그릴 값 한 프레임분. 시뮬레이션과 카메라에서 계산하며 판정에는 쓰지 않는다 (spec/08 HUD 표).</summary>
    public sealed class HudState
    {
        /// <summary>가장자리 표시가 화면 끝에서 떨어지는 뷰포트 비율. 세로는 상단(게이지·눈)·하단(스태미나·프롬프트·안내) 띠를 피한다.</summary>
        public static readonly Vector2 EdgeMargin = new Vector2(0.05f, 0.25f);

        public float BloodFraction { get; private set; }

        public bool SatietyHighlighted { get; private set; }

        public int BiteMarks { get; private set; }

        public bool HasHuman { get; private set; }

        public AwarenessState Awareness { get; private set; }

        /// <summary>경계값 / 광분 진입값 (눈 아이콘 채움).</summary>
        public float AwarenessFill { get; private set; }

        public float FrenzyMinRemaining { get; private set; }

        public float CalmProgress { get; private set; }

        public bool Occluded { get; private set; }

        public bool Hidden { get; private set; }

        public float StaminaFraction { get; private set; }

        /// <summary>대시 비용 위치의 눈금 (스태미나 최대 대비).</summary>
        public float DashCostFraction { get; private set; }

        public bool Exhausted { get; private set; }

        public bool Wet { get; private set; }

        public float WetRemaining { get; private set; }

        /// <summary>흡혈 중에만 보이는 가려움 링.</summary>
        public bool ItchVisible { get; private set; }

        public float ItchFraction { get; private set; }

        public bool HumidityVisible { get; private set; }

        public float HumidityFraction { get; private set; }

        /// <summary>중독 게이지는 중독 > 0일 때만 (spec/08).</summary>
        public bool ToxinVisible { get; private set; }

        public float ToxinFraction { get; private set; }

        /// <summary>모기향 하한 위치 (0이면 없음).</summary>
        public float ToxinFloorFraction { get; private set; }

        /// <summary>디버프 단계: 0 없음, 1 끊김, 2 반전, 3 랜덤 (spec/06).</summary>
        public int ToxinTier { get; private set; }

        public bool StutterActive { get; private set; }

        public bool RandomActive { get; private set; }

        public HudPrompt Prompt { get; private set; }

        public int EscapePressesRemaining { get; private set; }

        public bool CrosshairVisible { get; private set; }

        /// <summary>장착한 액티브 스킬이 있는가 (우하단 칸).</summary>
        public bool ActiveSkillVisible { get; private set; }

        public float ActiveSkillCooldown { get; private set; }

        public bool ActiveSkillInUse { get; private set; }

        /// <summary>인간 머리가 화면 밖일 때만.</summary>
        public EdgeMarker HeadArrow { get; private set; }

        /// <summary>공격 예고 판정 위치가 화면 밖일 때만.</summary>
        public EdgeMarker AttackWarning { get; private set; }

        public bool AttackTelegraphing { get; private set; }

        /// <summary>광분 중에만, 가장 가까운 Shadow Zone 방향.</summary>
        public EdgeMarker HidingDirection { get; private set; }

        public static HudState Compute(GameSimulation simulation, Camera camera, bool firstPerson, Tuning tuning)
        {
            var settings = simulation.Settings;
            var player = simulation.Player;
            var state = new HudState
            {
                BloodFraction = player.BloodGauge / SuckSystem.GaugeMax,
                SatietyHighlighted = simulation.Suck.SpeedMultiplier(player.BloodGauge) < tuning.GetFloat("hud.satietyHighlightMul"),
                Hidden = player.IsHidden,
                StaminaFraction = player.Stamina / settings.Stamina.Max,
                DashCostFraction = Mathf.Clamp01(simulation.DashCost / settings.Stamina.Max),
                Exhausted = player.IsExhausted,
                Wet = player.IsWet,
                WetRemaining = player.WetRemaining,
                HumidityVisible = player.Humidity > 0f,
                HumidityFraction = player.Humidity / HumiditySystem.GaugeMax,
                ItchVisible = player.SuckSession != null,
                ItchFraction = player.SuckSession != null ? Mathf.Clamp01(player.SuckSession.Site.Itch / settings.Attach.ItchThreshold) : 0f,
                CrosshairVisible = firstPerson || player.SuckSession != null,
            };

            state.Prompt = PromptFor(simulation);
            var toxin = settings.Toxin;
            state.ToxinVisible = player.Toxin > 0f;
            state.ToxinFraction = player.Toxin / ToxinSystem.GaugeMax;
            state.ToxinFloorFraction = player.ToxinFloor / ToxinSystem.GaugeMax;
            state.ToxinTier = player.Toxin >= toxin.Tier3 ? 3 : player.Toxin >= toxin.Tier2 ? 2 : player.Toxin >= toxin.Tier1 ? 1 : 0;
            state.StutterActive = simulation.Toxin.StutterActive(simulation.Tick);
            state.RandomActive = simulation.Toxin.RandomActive(simulation.Tick);
            state.ActiveSkillVisible = simulation.Decoy.IsAvailable;
            state.ActiveSkillCooldown = simulation.Decoy.CooldownRemaining(simulation.Tick);
            state.ActiveSkillInUse = simulation.Decoy.IsActive(simulation.Tick);
            state.EscapePressesRemaining = Mathf.Max(0, settings.Water.EscapePresses - player.EscapePresses);

            var human = simulation.Human;
            if (human == null)
            {
                return state;
            }

            state.HasHuman = true;
            state.BiteMarks = human.BiteMarkCount;
            state.Awareness = human.State;
            state.AwarenessFill = Mathf.Clamp01(human.Awareness / settings.Awareness.FrenzyEnter);
            state.FrenzyMinRemaining = human.FrenzyMinRemaining;
            state.CalmProgress = human.CalmProgress;
            state.Occluded = human.PlayerOccluded;
            state.HeadArrow = ScreenEdge.OffscreenMarker(camera, human.HeadCenter.ToUnity(), EdgeMargin);
            state.AttackTelegraphing = human.Attack.Phase == AttackPhase.Telegraph;
            state.AttackWarning = state.AttackTelegraphing ? ScreenEdge.OffscreenMarker(camera, human.Attack.Target.ToUnity(), EdgeMargin) : EdgeMarker.Hidden;
            state.HidingDirection = human.State == AwarenessState.Frenzy && !player.IsHidden
                ? NearestShadowZoneMarker(simulation, camera)
                : EdgeMarker.Hidden;
            return state;
        }

        private static HudPrompt PromptFor(GameSimulation simulation)
        {
            var player = simulation.Player;
            switch (player.State)
            {
                case PlayerState.Trapped:
                    return HudPrompt.Escape;
                case PlayerState.Attached:
                    bool onSkin = simulation.Human != null && simulation.Human.TryGetSite(player.Anchor.Shape, out _);
                    return onSkin && player.SuckSession == null ? HudPrompt.Suck : HudPrompt.Detach;
                case PlayerState.Flying:
                    return simulation.CanAttach ? HudPrompt.Attach : HudPrompt.None;
                default:
                    return HudPrompt.None;
            }
        }

        private static EdgeMarker NearestShadowZoneMarker(GameSimulation simulation, Camera camera)
        {
            var player = simulation.Player.Position;
            var zones = simulation.World.Shapes.Where(shape => shape.Matches(ShapeFlags.ShadowZone)).ToList();
            if (zones.Count == 0)
            {
                return EdgeMarker.Hidden;
            }

            var nearest = zones
                .Select(zone => ShapeGeometry.Closest(zone, player).Point)
                .OrderBy(point => System.Numerics.Vector3.DistanceSquared(point, player))
                .First();
            return ScreenEdge.Marker(camera, nearest.ToUnity(), EdgeMargin);
        }
    }
}
