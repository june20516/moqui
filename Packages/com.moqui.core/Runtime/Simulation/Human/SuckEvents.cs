using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>흡혈 중 이벤트 수치 (spec/04 §8, D-056). 키 이름은 spec/tuning.md suckEvent.*와 같다.</summary>
    public sealed class SuckEventSettings
    {
        public SuckEventSettings(Tuning tuning)
        {
            TwitchItchStart = tuning.GetFloat("suckEvent.twitchItchStart");
            TwitchItchStep = tuning.GetFloat("suckEvent.twitchItchStep");
            TwitchReachStart = tuning.GetFloat("suckEvent.twitchReachStart");
            TwitchReachStep = tuning.GetFloat("suckEvent.twitchReachStep");
            TwitchDuration = tuning.GetFloat("suckEvent.twitchDuration");
            ShiftRate = tuning.GetFloat("suckEvent.shiftRate");
            ShiftTelegraph = tuning.GetFloat("suckEvent.shiftTelegraph");
            ShiftDuration = tuning.GetFloat("suckEvent.shiftDuration");
            ShiftDistance = tuning.GetFloat("suckEvent.shiftDistance");
            GripMul = tuning.GetFloat("suckEvent.gripMul");
            ShiftRateMul = tuning.GetFloat("suckEvent.shiftRateMul");
            GlanceRate = tuning.GetFloat("suckEvent.glanceRate");
            GlanceTurnTime = tuning.GetFloat("suckEvent.glanceTurnTime");
            GlanceTurnSpeed = tuning.GetFloat("suckEvent.glanceTurnSpeed");
            GlanceHold = tuning.GetFloat("suckEvent.glanceHold");
            GlanceNoticeAwareness = tuning.GetFloat("suckEvent.glanceNoticeAwareness");
        }

        /// <summary>긁으러 오는 손: 첫 움찔 가려움과 단계 간격.</summary>
        public float TwitchItchStart { get; }

        public float TwitchItchStep { get; }

        /// <summary>움찔할 때 손이 문 자리까지 가는 비율: 첫 단계와 단계마다 더하는 값.</summary>
        public float TwitchReachStart { get; }

        public float TwitchReachStep { get; }

        public float TwitchDuration { get; }

        /// <summary>부위가 움직임: 위험률 (/s), 예고, 움직이는 시간, 이동량 (u).</summary>
        public float ShiftRate { get; }

        public float ShiftTelegraph { get; }

        public float ShiftDuration { get; }

        public float ShiftDistance { get; }

        /// <summary>움직이는 동안 Suck을 누르고 버티면 튕김 기준 속도에 곱하는 값.</summary>
        public float GripMul { get; }

        /// <summary>버티는 동안 흡혈 속도 배율.</summary>
        public float ShiftRateMul { get; }

        /// <summary>시선: 위험률 (/s, × (0.5 + 가려움/100)), 머리를 돌리는 예고 시간, 회전 속도 (°/s), 응시 시간, 들켰을 때 경계 증가.</summary>
        public float GlanceRate { get; }

        public float GlanceTurnTime { get; }

        public float GlanceTurnSpeed { get; }

        public float GlanceHold { get; }

        public float GlanceNoticeAwareness { get; }

        /// <summary>움찔 단계 수: 가려움 100(즉시 반응) 전까지의 단계 (40·60·80이면 3).</summary>
        public int TwitchLevels => TwitchItchStep > 0f ? Math.Max(0, (int)Math.Ceiling((ReactionSystem.GaugeMax - TwitchItchStart) / TwitchItchStep)) : 0;

        /// <summary>가려움이 지난 움찔 단계 (0 = 아직 없음).</summary>
        public int TwitchLevel(float itch)
        {
            if (itch < TwitchItchStart || TwitchItchStep <= 0f)
            {
                return 0;
            }

            return Math.Min(TwitchLevels, 1 + (int)((itch - TwitchItchStart) / TwitchItchStep));
        }

        public float TwitchReach(int level) => TwitchReachStart + ((level - 1) * TwitchReachStep);
    }

    public enum SuckEventKind
    {
        None,

        /// <summary>긁으러 오는 손: 반대쪽 손이 문 자리 쪽으로 움찔 (경고, 판정 없음).</summary>
        Twitch,

        /// <summary>부위가 움직임: 예고 뒤 부위가 옆으로 크게 움직인다. Suck을 누르고 버티면 붙어 있는다.</summary>
        Shift,

        /// <summary>시선: 머리가 문 자리를 본다. 그동안 흡혈하고 있으면 들킨다.</summary>
        Glance,
    }

    public enum SuckEventPhase
    {
        Telegraph,
        Active,

        /// <summary>시선: 원래 보던 쪽으로 머리를 되돌린다.</summary>
        Return,
    }

    /// <summary>진행 중인 흡혈 이벤트 (인간마다 하나, spec/04 §8).</summary>
    public sealed class SuckEventState
    {
        public SuckEventKind Kind { get; set; } = SuckEventKind.None;

        public SuckEventPhase Phase { get; set; }

        public int PhaseStartTick { get; set; }

        public int PhaseEndTick { get; set; }

        /// <summary>문 자리 (월드). 부위가 움직이면 틱마다 갱신한다.</summary>
        public Vector3 Target { get; set; }

        /// <summary>움찔하는 팔과 그 팔의 시작 손바닥 위치, 문 자리까지 가는 비율.</summary>
        public int Arm { get; set; } = -1;

        public Vector3 HandRest { get; set; }

        public float Reach { get; set; }

        /// <summary>시선에 이미 들켰는가 (한 번만).</summary>
        public bool Noticed { get; set; }

        /// <summary>움찔 단계를 센 세션 (세션을 시작한 부착 틱).</summary>
        public int SessionTick { get; set; } = Player.NeverTick;

        /// <summary>이 세션에서 이미 한 움찔 단계.</summary>
        public int TwitchLevel { get; set; }

        /// <summary>시선 전에 보던 머리 각도 (되돌릴 곳).</summary>
        public float ReturnYaw { get; set; }

        public float ReturnPitch { get; set; }

        public bool IsActive => Kind != SuckEventKind.None;

        public bool Is(SuckEventKind kind, SuckEventPhase phase) => Kind == kind && Phase == phase;

        /// <summary>지금 단계의 진행률 0~1.</summary>
        public float Progress(int tick)
        {
            int length = Math.Max(1, PhaseEndTick - PhaseStartTick);
            return Math.Clamp((float)(tick - PhaseStartTick) / length, 0f, 1f);
        }

        public void Begin(SuckEventKind kind, SuckEventPhase phase, int tick, float seconds)
        {
            Kind = kind;
            BeginPhase(phase, tick, seconds);
            Noticed = false;
        }

        public void BeginPhase(SuckEventPhase phase, int tick, float seconds)
        {
            Phase = phase;
            PhaseStartTick = tick;
            PhaseEndTick = tick + Math.Max(1, SimulationTime.ToTicks(seconds));
        }

        public void Clear()
        {
            Kind = SuckEventKind.None;
            Arm = -1;
            Noticed = false;
        }
    }

    /// <summary>
    /// 흡혈 중 이벤트 (spec/04 §8, D-056): 긁으러 오는 손, 부위가 움직임, 시선.
    /// "흡혈 중" = 인간의 피부에 붙어 Suck을 누른 채 세션이 진행 중. 이벤트는 한 번에 하나이다.
    /// 시선의 머리 회전은 HumanBrain이, 부위 움직임은 HumanMotionSystem이, 버티기(튕김 기준)는 GameSimulation이,
    /// 버티는 동안 흡혈 배율은 SuckSystem이 이 상태를 읽어 처리한다.
    /// </summary>
    public sealed class SuckEventSystem
    {
        private const float RadiansToDegrees = 180f / MathF.PI;

        private readonly SuckEventSettings _settings;
        private readonly VisionSettings _vision;
        private readonly CollisionWorld _world;
        private readonly HumanMotionSystem _motion;
        private readonly IRandom _random;

        public SuckEventSystem(SuckEventSettings settings, VisionSettings vision, CollisionWorld world, HumanMotionSystem motion, IRandom random)
        {
            _settings = settings;
            _vision = vision;
            _world = world;
            _motion = motion;
            _random = random;
        }

        /// <summary>
        /// 시선이 진행되는 동안(돌림·응시·되돌림) 흡혈을 멈추고 붙은 채 얼어 있는 모기는 시야로 알아채지 못한다:
        /// 가려운 자리를 살필 뿐이다 (D-056). 흡혈하고 있으면 평소 시야 + 응시 중 들킴.
        /// </summary>
        public static bool HiddenByFreezing(Human human, Player player)
        {
            return human.SuckEvent.Kind == SuckEventKind.Glance && player.State == PlayerState.Attached && !player.SuckHeld;
        }

        /// <summary>인간의 피부에 붙어 Suck을 누른 채 세션이 진행 중인가.</summary>
        public static bool IsSucking(Human human, Player player)
        {
            return player.State == PlayerState.Attached
                && player.SuckHeld
                && player.SuckSession != null
                && human.Owns(player.SuckSession.Site.Shape);
        }

        /// <summary>흡혈 중 이벤트를 켜고 끈다 (이벤트와 무관한 규칙 테스트용). 기본 켜짐.</summary>
        public bool Enabled { get; set; } = true;

        public void Step(Human human, Player player, int tick, float deltaTime, List<SimulationEvent> events)
        {
            if (!Enabled)
            {
                return;
            }

            var state = human.SuckEvent;
            var session = player.SuckSession;
            if (session != null && state.SessionTick != session.AttachedTick)
            {
                state.SessionTick = session.AttachedTick;
                state.TwitchLevel = 0;
            }

            if (session?.BiteSpot != null && human.Owns(session.Site.Shape))
            {
                session.BiteSpot.Resolve(out Vector3 spot, out _);
                state.Target = spot;
            }

            bool sucking = IsSucking(human, player);
            switch (state.Kind)
            {
                case SuckEventKind.Twitch:
                    StepTwitch(human, state, tick);
                    return;
                case SuckEventKind.Shift:
                    StepShift(human, player, state, tick, events);
                    return;
                case SuckEventKind.Glance:
                    StepGlance(human, player, state, sucking, tick, events);
                    return;
            }

            if (sucking)
            {
                TryStart(human, player, state, tick, deltaTime, events);
            }
        }

        /// <summary>그 팔이 아닌 팔 중 문 자리에 가까운 팔 (모기가 앉은 팔로는 그 자리를 못 긁는다).</summary>
        private static int TwitchArm(Human human, string partId, Vector3 spot)
        {
            int excluded = human.Rig.ArmOwning(partId)?.Index ?? -1;
            return Enumerable.Range(0, human.Rig.Arms.Count)
                .Where(arm => arm != excluded)
                .OrderBy(arm => Vector3.Distance(human.Shoulder(arm), spot))
                .DefaultIfEmpty(-1)
                .First();
        }

        /// <summary>지정한 이벤트를 바로 시작한다 (테스트, 연출). 흡혈 중이 아니면 아무 일도 없다.</summary>
        public void Begin(Human human, Player player, SuckEventKind kind, int tick, List<SimulationEvent> events)
        {
            var state = human.SuckEvent;
            if (state.IsActive || !IsSucking(human, player))
            {
                return;
            }

            switch (kind)
            {
                case SuckEventKind.Twitch:
                    StartTwitch(human, player.SuckSession.Site, Math.Max(1, state.TwitchLevel), tick, events);
                    break;
                case SuckEventKind.Shift:
                    StartShift(human, tick, events);
                    break;
                case SuckEventKind.Glance:
                    StartGlance(human, tick, events);
                    break;
            }
        }

        private void TryStart(Human human, Player player, SuckEventState state, int tick, float deltaTime, List<SimulationEvent> events)
        {
            if (human.Attack.IsBusy || human.IsAsleep)
            {
                return;
            }

            var site = player.SuckSession.Site;
            int level = _settings.TwitchLevel(site.Itch);
            if (level > state.TwitchLevel)
            {
                state.TwitchLevel = level;
                if (StartTwitch(human, site, level, tick, events))
                {
                    return;
                }
            }

            if (human.State == AwarenessState.Frenzy)
            {
                return;
            }

            if (human.CurrentAction == null && _random.Chance(ReactionSystem.TickProbability(_settings.ShiftRate, deltaTime)))
            {
                StartShift(human, tick, events);
                return;
            }

            float glanceRate = _settings.GlanceRate * (0.5f + (site.Itch / ReactionSystem.GaugeMax));
            if (_random.Chance(ReactionSystem.TickProbability(glanceRate, deltaTime)))
            {
                StartGlance(human, tick, events);
            }
        }

        private bool StartTwitch(Human human, SkinSiteState site, int level, int tick, List<SimulationEvent> events)
        {
            var state = human.SuckEvent;
            int arm = TwitchArm(human, site.PartId, state.Target);
            if (arm < 0)
            {
                return false;
            }

            state.Begin(SuckEventKind.Twitch, SuckEventPhase.Active, tick, _settings.TwitchDuration);
            state.Arm = arm;
            state.HandRest = human.Palm(arm);
            state.Reach = _settings.TwitchReach(level);
            events.Add(new SuckEventStarted(tick, human.Id, SuckEventKind.Twitch));
            return true;
        }

        private void StartShift(Human human, int tick, List<SimulationEvent> events)
        {
            human.SuckEvent.Begin(SuckEventKind.Shift, SuckEventPhase.Telegraph, tick, _settings.ShiftTelegraph);
            events.Add(new SuckEventStarted(tick, human.Id, SuckEventKind.Shift));
        }

        private void StartGlance(Human human, int tick, List<SimulationEvent> events)
        {
            var state = human.SuckEvent;
            state.Begin(SuckEventKind.Glance, SuckEventPhase.Telegraph, tick, _settings.GlanceTurnTime);
            state.ReturnYaw = human.HeadYaw;
            state.ReturnPitch = human.HeadPitch;
            events.Add(new SuckEventStarted(tick, human.Id, SuckEventKind.Glance));
        }

        private static void StepTwitch(Human human, SuckEventState state, int tick)
        {
            // 진짜 공격(반응 때리기 등)이 시작되면 그 공격이 손을 가져간다.
            if (human.Attack.IsBusy)
            {
                state.Clear();
                return;
            }

            float t = state.Progress(tick);
            if (t >= 1f)
            {
                human.Pose.HandTargets[state.Arm] = null;
                human.UpdatePose();
                state.Clear();
                return;
            }

            float reach = state.Reach * MathF.Sin(MathF.PI * t);
            human.Pose.HandTargets[state.Arm] = Vector3.Lerp(state.HandRest, state.Target, reach);
            human.UpdatePose();
        }

        private void StepShift(Human human, Player player, SuckEventState state, int tick, List<SimulationEvent> events)
        {
            if (tick < state.PhaseEndTick)
            {
                return;
            }

            if (state.Phase == SuckEventPhase.Active)
            {
                state.Clear();
                return;
            }

            // 예고가 끝났다: 그 부위를 옆으로 움직인다. 이미 다른 동작·공격 중이거나 모기가 떠났으면 그만둔다.
            var session = player.SuckSession;
            if (human.CurrentAction != null || human.Attack.IsBusy || session == null || !human.Owns(session.Site.Shape))
            {
                state.Clear();
                return;
            }

            var shape = session.Site.Shape;
            Vector3 axis = shape.PointB - shape.PointA;
            Vector3 side = Vector3.Cross(axis, Vector3.UnitY);
            if (side.LengthSquared() < 1e-6f)
            {
                side = Vector3.Cross(axis, Vector3.UnitX);
            }

            side = Vector3.Normalize(side) * (_random.Chance(0.5) ? 1f : -1f);
            Vector3 offset = Vector3.Transform(side * _settings.ShiftDistance, Quaternion.Conjugate(human.BodyRotation));
            var motion = new PartMotionDefinition(session.Site.PartId, offset, offset);
            var action = new HumanActionDefinition(ShiftActionName, 1f, _settings.ShiftDuration, new[] { motion });
            _motion.Start(human, action, tick);
            state.BeginPhase(SuckEventPhase.Active, tick, _settings.ShiftDuration);
            events.Add(new SuckEventStarted(tick, human.Id, SuckEventKind.Shift));
        }

        private void StepGlance(Human human, Player player, SuckEventState state, bool sucking, int tick, List<SimulationEvent> events)
        {
            if (human.State == AwarenessState.Frenzy || human.IsAsleep || human.Attack.IsBusy)
            {
                state.Clear();
                return;
            }

            if (state.Phase == SuckEventPhase.Telegraph)
            {
                if (tick >= state.PhaseEndTick)
                {
                    state.BeginPhase(SuckEventPhase.Active, tick, _settings.GlanceHold);
                }

                return;
            }

            if (state.Phase == SuckEventPhase.Return)
            {
                if (tick >= state.PhaseEndTick)
                {
                    state.Clear();
                }

                return;
            }

            if (!state.Noticed && sucking && CanSee(human, player))
            {
                // 흡혈하며 꿈틀거리는 모기를 보았다 (D-056).
                state.Noticed = true;
                human.Causes.Add(AwarenessCause.Glance, _settings.GlanceNoticeAwareness);
                human.Awareness = Math.Min(ReactionSystem.GaugeMax, human.Awareness + _settings.GlanceNoticeAwareness);
                human.HasStimulus = true;
                human.LastStimulusPosition = state.Target;
                human.LastStimulusTick = tick;
                events.Add(new SuckGlanceNoticed(tick, human.Id, state.Target));
            }

            if (tick >= state.PhaseEndTick)
            {
                state.BeginPhase(SuckEventPhase.Return, tick, _settings.GlanceTurnTime);
            }
        }

        /// <summary>머리 정면 Yellow 원뿔 각 안이고 가림이 없는가 (몸 캡슐은 가림으로 보지 않는다, spec/02 §1).</summary>
        private bool CanSee(Human human, Player player)
        {
            Vector3 eye = human.HeadCenter;
            Vector3 toPlayer = player.Position - eye;
            float distance = toPlayer.Length();
            if (distance <= player.CollisionRadius)
            {
                return true;
            }

            float cos = Vector3.Dot(Vector3.Normalize(human.HeadForward), toPlayer / distance);
            float angle = MathF.Acos(Math.Clamp(cos, -1f, 1f)) * RadiansToDegrees;
            if (angle > _vision.YellowHalfAngle || distance > _vision.YellowRange)
            {
                return false;
            }

            return !_world.Raycast(eye, toPlayer, distance - player.CollisionRadius, ShapeFlags.Obstacle, out _, ShapeFlags.Body);
        }

        /// <summary>부위가 움직임 이벤트가 만드는 동작 이름 (튕김 기준·표현에서 구분).</summary>
        public const string ShiftActionName = "suckShift";
    }
}
