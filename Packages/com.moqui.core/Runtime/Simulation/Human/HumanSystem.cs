using System.Collections.Generic;
using Moqui.Core.Collision;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 인간 한 틱 (tech/architecture.md §4.4 순서): Sensing(Vision/Hearing) → Awareness → HumanBrain(상태·머리) → Attacks.
    /// </summary>
    public sealed class HumanSystem
    {
        private readonly VisionSensor _vision;
        private readonly HearingSensor _hearing;
        private readonly HumanBrain _brain;
        private readonly HumanAttackSystem _attacks;

        public HumanSystem(GameSettings settings, CollisionWorld world, ulong seed)
        {
            _vision = new VisionSensor(settings.Vision, world);
            _hearing = new HearingSensor(settings.Noise, settings.Hearing);
            Awareness = new AwarenessSystem(settings.Awareness);
            _brain = new HumanBrain(settings.Awareness, settings.Frenzy, settings.Head);
            _attacks = new HumanAttackSystem(settings.Attack, settings.Frenzy, SeedStreams.Create(seed, SeedStreams.BlindSwat));
            Reactions = new ReactionSystem(settings, _attacks, SeedStreams.Create(seed, SeedStreams.Reactions));
        }

        public AwarenessSystem Awareness { get; }

        public HumanAttackSystem Attacks => _attacks;

        public ReactionSystem Reactions { get; }

        public HumanPerception LastPerception { get; private set; }

        public void Step(Human human, Player player, int tick, float deltaTime, List<SimulationEvent> events)
        {
            var perception = default(HumanPerception);
            _vision.Sense(human, player, tick, ref perception);
            _hearing.Sense(human, player, events, ref perception);
            LastPerception = perception;

            Awareness.Apply(human, perception, player.IsHidden, tick, deltaTime);
            _brain.UpdateState(human, perception, player, tick, events);
            _brain.UpdateHead(human, player, tick, deltaTime);

            _attacks.Advance(human, player, tick, events);
            _attacks.Decide(human, player, perception, tick, events);
            Reactions.Step(human, player, perception, tick, deltaTime, events);
        }
    }
}
