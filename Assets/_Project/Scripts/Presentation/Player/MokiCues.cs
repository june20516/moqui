using System.Collections.Generic;
using Moqui.Core.Simulation;

namespace Moqui.Unity.Presentation
{
    /// <summary>모키 표현 큐 ID (spec/12-moki-cues.md). 표시기는 이 ID로 동작·글자를 고른다.</summary>
    public static class MokiCueIds
    {
        public const string LandReady = "land.ready";
        public const string Land = "land";
        public const string LandMiss = "land.miss";
        public const string Detach = "detach";
        public const string WandIn = "wand.in";
        public const string WandDrink = "wand.drink";
        public const string WandOut = "wand.out";
        public const string WandTug = "wand.tug";
        public const string WandGrip = "wand.grip";
        public const string WandYank = "wand.yank";
        public const string WandCloth = "wand.cloth";
        public const string Freeze = "freeze";
        public const string Dislodged = "dislodged";
        public const string Heard = "heard";
        public const string Precise = "precise";
        public const string Full = "full";
        public const string Trapped = "trapped";
    }

    /// <summary>큐 하나의 표시 정의: 지속 여부, 같은 큐를 다시 낼 수 있는 최소 간격(s), 글자 표시기의 글자.</summary>
    public sealed class MokiCueDefinition
    {
        public MokiCueDefinition(string id, bool loop, float minInterval, string text)
        {
            Id = id;
            Loop = loop;
            MinInterval = minInterval;
            Text = text;
        }

        public string Id { get; }

        public bool Loop { get; }

        public float MinInterval { get; }

        /// <summary>글자 표시기(`TextCuePresenter`)가 띄우는 글자. 모델·VFX 표시기는 쓰지 않는다.</summary>
        public string Text { get; }
    }

    /// <summary>큐 목록 (spec/12의 표와 같다).</summary>
    public static class MokiCueCatalog
    {
        public static readonly IReadOnlyDictionary<string, MokiCueDefinition> All = Build(
            new MokiCueDefinition(MokiCueIds.LandReady, true, 0f, "사뿐?"),
            new MokiCueDefinition(MokiCueIds.Land, false, 0.2f, "사뿐"),
            new MokiCueDefinition(MokiCueIds.LandMiss, false, 0.5f, "허공…"),
            new MokiCueDefinition(MokiCueIds.Detach, false, 0.2f, "폴짝"),
            new MokiCueDefinition(MokiCueIds.WandIn, false, 0.2f, "콕!"),
            new MokiCueDefinition(MokiCueIds.WandDrink, true, 0f, "쪽…"),
            new MokiCueDefinition(MokiCueIds.WandOut, false, 0.3f, "쏙"),
            new MokiCueDefinition(MokiCueIds.WandTug, false, 0.6f, "끙!"),
            new MokiCueDefinition(MokiCueIds.WandGrip, true, 0f, "버텨!"),
            new MokiCueDefinition(MokiCueIds.WandYank, false, 0.3f, "확!"),
            new MokiCueDefinition(MokiCueIds.WandCloth, false, 1f, "천…"),
            new MokiCueDefinition(MokiCueIds.Freeze, true, 0f, "쉿…"),
            new MokiCueDefinition(MokiCueIds.Dislodged, false, 0.3f, "앗!"),
            new MokiCueDefinition(MokiCueIds.Heard, false, 2f, "윙…?"),
            new MokiCueDefinition(MokiCueIds.Precise, true, 0f, "살금"),
            new MokiCueDefinition(MokiCueIds.Full, true, 0f, "묵직"),
            new MokiCueDefinition(MokiCueIds.Trapped, true, 0f, "꿀렁"));

        private static IReadOnlyDictionary<string, MokiCueDefinition> Build(params MokiCueDefinition[] definitions)
        {
            var map = new Dictionary<string, MokiCueDefinition>();
            foreach (var definition in definitions)
            {
                map.Add(definition.Id, definition);
            }

            return map;
        }
    }

    /// <summary>
    /// 큐를 보여 주는 쪽 (spec/12). 지금은 글자 표시기, 모델·VFX가 준비되면 애니메이션 표시기로 바꾼다.
    /// 큐를 내는 <see cref="MokiCueSystem"/>은 표시기를 몰라도 된다.
    /// </summary>
    public interface IMokiCuePresenter
    {
        /// <summary>한 번 큐를 재생한다.</summary>
        void Play(MokiCueDefinition cue);

        /// <summary>지속 큐를 켜거나 끈다.</summary>
        void SetLoop(MokiCueDefinition cue, bool active);
    }

    /// <summary>
    /// 시뮬레이션 틱마다 Core 상태·이벤트를 읽어 모키 표현 큐를 낸다 (표현 전용, 규칙 판정 없음, spec/12).
    /// </summary>
    public sealed class MokiCueSystem
    {
        private readonly float _satietyHighlightMul;
        private readonly Dictionary<string, int> _lastPlayedTick = new Dictionary<string, int>();
        private readonly HashSet<string> _activeLoops = new HashSet<string>();
        private readonly Dictionary<Human, bool> _wasInEarZone = new Dictionary<Human, bool>();
        private bool _wasDrinking;
        private bool _hadSession;

        /// <param name="satietyHighlightMul">포만 강조 기준 (tuning hud.satietyHighlightMul): 속도 배율이 이보다 낮으면 묵직.</param>
        public MokiCueSystem(float satietyHighlightMul)
        {
            _satietyHighlightMul = satietyHighlightMul;
        }

        /// <summary>지금 켜져 있는 지속 큐.</summary>
        public IReadOnlyCollection<string> ActiveLoops => _activeLoops;

        /// <summary>한 틱을 읽고 이번 틱의 한 번 큐를 돌려준다. 지속 큐 변화는 presenter가 있으면 바로 알린다.</summary>
        public List<string> Step(GameSimulation simulation, IMokiCuePresenter presenter = null)
        {
            var oneShots = new List<string>();
            var player = simulation.Player;
            var command = simulation.LastCommand;
            int tick = simulation.Tick;
            bool attached = player.State == PlayerState.Attached;
            bool session = attached && player.SuckSession != null;
            bool drinking = session && command.SuckHeld;
            bool moveInput = command.Move.LengthSquared() > 0f || command.Vertical != 0f;
            bool attachedEvent = false;
            bool detached = false;

            foreach (var simulationEvent in simulation.Events)
            {
                switch (simulationEvent)
                {
                    case PlayerAttached _:
                        attachedEvent = true;
                        Emit(MokiCueIds.Land, tick, oneShots);
                        break;
                    case PlayerDetached _:
                        detached = true;
                        break;
                    case PlayerDislodged _:
                        Emit(MokiCueIds.Dislodged, tick, oneShots);
                        break;
                }
            }

            if (detached)
            {
                // 흡혈 중 대시로 뽑았으면 "확!", 아니면 그냥 떠남.
                Emit(_wasDrinking && command.DashPressed ? MokiCueIds.WandYank : MokiCueIds.Detach, tick, oneShots);
            }

            if (command.AttachPressed && !attachedEvent && !detached && player.State == PlayerState.Flying && !simulation.CanAttach)
            {
                Emit(MokiCueIds.LandMiss, tick, oneShots);
            }

            if (session && !_hadSession)
            {
                Emit(MokiCueIds.WandIn, tick, oneShots);
            }

            if (_wasDrinking && session && !command.SuckHeld)
            {
                Emit(MokiCueIds.WandOut, tick, oneShots);
            }

            if (drinking && moveInput)
            {
                Emit(MokiCueIds.WandTug, tick, oneShots);
            }

            if (attached && command.SuckHeld && player.SuckSession == null && OnClothedBody(simulation))
            {
                Emit(MokiCueIds.WandCloth, tick, oneShots);
            }

            bool glance = false;
            bool shifting = false;
            foreach (var human in simulation.Humans)
            {
                glance |= human.SuckEvent.Kind == SuckEventKind.Glance;
                shifting |= human.SuckEvent.Is(SuckEventKind.Shift, SuckEventPhase.Active);
                bool inEar = simulation.HumanSystemOf(human).LastPerception.InEarZone;
                _wasInEarZone.TryGetValue(human, out bool wasInEar);
                if (inEar && !wasInEar)
                {
                    Emit(MokiCueIds.Heard, tick, oneShots);
                }

                _wasInEarZone[human] = inEar;
            }

            SetLoop(MokiCueIds.LandReady, player.State == PlayerState.Flying && simulation.CanAttach, presenter);
            SetLoop(MokiCueIds.WandDrink, drinking && !shifting, presenter);
            SetLoop(MokiCueIds.WandGrip, drinking && shifting, presenter);
            SetLoop(MokiCueIds.Freeze, session && !command.SuckHeld && glance, presenter);
            SetLoop(MokiCueIds.Precise, player.State == PlayerState.Flying && player.PrecisionHeld, presenter);
            SetLoop(MokiCueIds.Full, simulation.Suck.SpeedMultiplier(player.BloodGauge) < _satietyHighlightMul, presenter);
            SetLoop(MokiCueIds.Trapped, player.State == PlayerState.Trapped, presenter);

            _wasDrinking = drinking;
            _hadSession = session;
            if (presenter != null)
            {
                foreach (string id in oneShots)
                {
                    presenter.Play(MokiCueCatalog.All[id]);
                }
            }

            return oneShots;
        }

        private static bool OnClothedBody(GameSimulation simulation)
        {
            var shape = simulation.Player.Anchor?.Shape;
            if (shape == null)
            {
                return false;
            }

            foreach (var human in simulation.Humans)
            {
                if (human.Owns(shape))
                {
                    return !human.TryGetSite(shape, out _);
                }
            }

            return false;
        }

        private void Emit(string id, int tick, List<string> oneShots)
        {
            var definition = MokiCueCatalog.All[id];
            if (_lastPlayedTick.TryGetValue(id, out int last) && !SimulationTime.HasElapsed(last, tick, definition.MinInterval))
            {
                return;
            }

            _lastPlayedTick[id] = tick;
            oneShots.Add(id);
        }

        private void SetLoop(string id, bool active, IMokiCuePresenter presenter)
        {
            bool changed = active ? _activeLoops.Add(id) : _activeLoops.Remove(id);
            if (changed && presenter != null)
            {
                presenter.SetLoop(MokiCueCatalog.All[id], active);
            }
        }
    }
}
