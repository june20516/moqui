using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 인간 한 틱 (tech/architecture.md §4.4 순서). StepMotion(무작위 동작)은 플레이어 이동 전에,
    /// Step(Sensing → Awareness → HumanBrain → Attacks → Reactions)은 플레이어 이동 뒤에 부른다.
    /// </summary>
    public sealed class HumanSystem
    {
        private readonly VisionSensor _vision;
        private readonly HearingSensor _hearing;
        private readonly HumanBrain _brain;
        private readonly HumanAttackSystem _attacks;
        private readonly BiteMarkSettings _biteMarks;

        public HumanSystem(GameSettings settings, CollisionWorld world, ulong seed)
        {
            _biteMarks = settings.BiteMark;
            _vision = new VisionSensor(settings.Vision, world, settings.Humid.SteamVisionMul);
            _hearing = new HearingSensor(settings.Noise, settings.Hearing);
            Awareness = new AwarenessSystem(settings.Awareness);
            _brain = new HumanBrain(settings.Awareness, settings.Frenzy, settings.Head, settings.Doze, SeedStreams.Create(seed, SeedStreams.Glance));
            Doze = new DozeSystem(settings.Doze, settings.Awareness, SeedStreams.Create(seed, SeedStreams.Doze));
            Breath = new BreathSystem(settings.Breath);
            _attacks = new HumanAttackSystem(settings.Attack, settings.Frenzy, SeedStreams.Create(seed, SeedStreams.BlindSwat));
            Reactions = new ReactionSystem(settings, _attacks, SeedStreams.Create(seed, SeedStreams.Reactions));
            Motion = new HumanMotionSystem(settings.HumanMotion, SeedStreams.Create(seed, SeedStreams.HumanActions));
        }

        public AwarenessSystem Awareness { get; }

        public HumanAttackSystem Attacks => _attacks;

        public ReactionSystem Reactions { get; }

        public HumanMotionSystem Motion { get; }

        public DozeSystem Doze { get; }

        public BreathSystem Breath { get; }

        /// <summary>시작 상태: 무작위 동작 예약, 졸음, 둘러보기 예약.</summary>
        public void Initialize(Human human, int tick)
        {
            Motion.Initialize(human);
            Doze.Initialize(human, tick);
            _brain.Initialize(human);
        }

        public HumanPerception LastPerception { get; private set; }

        public void StepMotion(Human human, int tick)
        {
            Motion.Step(human, tick);
        }

        public void Step(Human human, Player player, int tick, float deltaTime, List<SimulationEvent> events)
        {
            Doze.Step(human, tick, events);
            Breath.Step(human, tick);

            // 졸기 중에는 눈을 감아 시각이 없다(Red Zone 포함). 청각 증가는 doze.hearingMul배 (spec/06).
            var perception = default(HumanPerception);
            if (!human.IsAsleep)
            {
                _vision.Sense(human, player, tick, ref perception);
            }

            _hearing.Sense(human, player, events, ref perception);
            float hearingMul = Doze.HearingMultiplier(human);
            perception.HearingRate *= hearingMul;
            perception.InstantGain *= hearingMul;
            LastPerception = perception;
            human.PlayerOccluded = perception.PlayerOccluded;

            // 물린 자국 수 n에 따른 경계 보정 (spec/04 §4).
            Awareness.GainMultiplier = _biteMarks.GainMultiplier(human.BiteMarkCount);
            Awareness.DecayDivisor = _biteMarks.DecayDivisor(human.BiteMarkCount);
            Awareness.Floor = _biteMarks.Floor(human.BiteMarkCount);

            Awareness.Apply(human, perception, player.IsHidden, tick, deltaTime);
            _brain.UpdateState(human, perception, player, tick, events);
            _brain.UpdateHead(human, player, tick, deltaTime);

            _attacks.Advance(human, player, tick, events);
            _attacks.Decide(human, player, perception, tick, events);
            Reactions.Step(human, player, perception, tick, deltaTime, events);
        }
    }
}
