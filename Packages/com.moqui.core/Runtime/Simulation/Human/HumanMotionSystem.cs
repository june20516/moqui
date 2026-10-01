using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 무작위 움직임 (spec/02 §6). human.actionInterval마다 가중치에 따라 동작 하나를 고르고 몸 캡슐을 움직인다.
    /// 어그로 상태·흡혈 여부와 관계없이 계속되지만, 광분 중이거나 공격 중이면 새 동작을 시작하지 않는다(공격 우선).
    /// 진행 중인 동작은 끝까지 이어진다. 난수는 레벨 시드의 humanActions 스트림을 쓴다.
    /// </summary>
    public sealed class HumanMotionSystem
    {
        private readonly HumanMotionSettings _settings;
        private readonly IRandom _random;

        public HumanMotionSystem(HumanMotionSettings settings, IRandom random)
        {
            _settings = settings;
            _random = random;
        }

        public void Initialize(Human human)
        {
            human.NextActionTick = NextInterval();
        }

        public void Step(Human human, int tick)
        {
            if (human.CurrentAction != null && SimulationTime.HasElapsed(human.ActionStartTick, tick, human.CurrentAction.Duration))
            {
                human.CurrentAction = null;
                human.ApplyPose(null, 0f);
            }

            var actions = human.Definition.Actions;
            if (human.CurrentAction == null
                && actions.Count > 0
                && tick >= human.NextActionTick
                && human.State != AwarenessState.Frenzy
                && !human.Attack.IsBusy)
            {
                Start(human, PickWeighted(actions), tick);
            }

            if (human.CurrentAction != null)
            {
                human.ApplyPose(human.CurrentAction, (tick - human.ActionStartTick) * GameSimulation.DeltaTime);
            }
        }

        /// <summary>지정한 동작을 바로 시작한다 (테스트, 스크립트 연출).</summary>
        public void Start(Human human, HumanActionDefinition action, int tick)
        {
            human.CurrentAction = action;
            human.ActionStartTick = tick;
            human.NextActionTick = tick + System.Math.Max(SimulationTime.ToTicks(action.Duration), NextInterval());
        }

        private int NextInterval()
        {
            return SimulationTime.ToTicks(_random.Range(_settings.ActionInterval.Min, _settings.ActionInterval.Max));
        }

        private HumanActionDefinition PickWeighted(System.Collections.Generic.IReadOnlyList<HumanActionDefinition> actions)
        {
            float total = 0f;
            foreach (var action in actions)
            {
                total += action.Weight;
            }

            float roll = _random.Range(0f, total);
            foreach (var action in actions)
            {
                roll -= action.Weight;
                if (roll < 0f)
                {
                    return action;
                }
            }

            return actions[actions.Count - 1];
        }
    }
}
