using System.Collections.Generic;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    public enum DozeState
    {
        /// <summary>졸음 수정자가 없거나 완전히 깬 상태.</summary>
        Awake,

        /// <summary>졸기: 눈을 감아 시각이 없고, 청각·반응이 둔하다.</summary>
        Sleeping,

        /// <summary>깜빡 깸: 잠깐 눈을 뜨고 평소처럼 감지한다.</summary>
        Blinking,
    }

    /// <summary>
    /// 졸음 수정자 (spec/06). 졸기(doze.sleepDuration)와 깜빡 깸(doze.wakeDuration)을 반복한다.
    /// 깨기 doze.wakeTelegraph 전에 예고 이벤트가 나온다. 경계가 의심 진입선 이상이면 완전히 깨고,
    /// 의심 이탈선 미만이 doze.resleepDelay 동안 이어지면 다시 존다. 난수는 레벨 시드의 doze 스트림을 쓴다.
    /// </summary>
    public sealed class DozeSystem
    {
        private readonly DozeSettings _settings;
        private readonly AwarenessSettings _awareness;
        private readonly IRandom _random;

        public DozeSystem(DozeSettings settings, AwarenessSettings awareness, IRandom random)
        {
            _settings = settings;
            _awareness = awareness;
            _random = random;
        }

        public void Initialize(Human human, int tick)
        {
            if (human.Definition.Traits.Has(HumanModifier.Doze))
            {
                StartSleeping(human, tick);
            }
        }

        public void Step(Human human, int tick, List<SimulationEvent> events)
        {
            if (!human.Definition.Traits.Has(HumanModifier.Doze))
            {
                return;
            }

            if (human.Doze != DozeState.Awake && human.Awareness >= _awareness.SuspiciousEnter)
            {
                Change(human, DozeState.Awake, tick, events);
                human.CalmSinceTick = Player.NeverTick;
                return;
            }

            switch (human.Doze)
            {
                case DozeState.Awake:
                    if (human.Awareness >= _awareness.SuspiciousExit || human.State != AwarenessState.Safe)
                    {
                        human.CalmSinceTick = Player.NeverTick;
                    }
                    else if (human.CalmSinceTick == Player.NeverTick)
                    {
                        human.CalmSinceTick = tick;
                    }
                    else if (SimulationTime.HasElapsed(human.CalmSinceTick, tick, _settings.ResleepDelay))
                    {
                        StartSleeping(human, tick);
                        events.Add(new DozeStateChanged(tick, human.Id, DozeState.Awake, DozeState.Sleeping));
                    }

                    break;
                case DozeState.Sleeping:
                    if (!human.DozeTelegraphSent && tick >= human.DozeStateEndTick - SimulationTime.ToTicks(_settings.WakeTelegraph))
                    {
                        human.DozeTelegraphSent = true;
                        events.Add(new DozeWakeTelegraph(tick, human.Id));
                    }

                    if (tick >= human.DozeStateEndTick)
                    {
                        human.DozeStateEndTick = tick + SimulationTime.ToTicks(_random.Range(_settings.WakeDuration.Min, _settings.WakeDuration.Max));
                        Change(human, DozeState.Blinking, tick, events);
                    }

                    break;
                case DozeState.Blinking:
                    if (tick >= human.DozeStateEndTick)
                    {
                        StartSleeping(human, tick);
                        events.Add(new DozeStateChanged(tick, human.Id, DozeState.Blinking, DozeState.Sleeping));
                    }

                    break;
            }
        }

        /// <summary>청각 증가 배율: 졸기 중 doze.hearingMul.</summary>
        public float HearingMultiplier(Human human)
        {
            return human.Doze == DozeState.Sleeping ? _settings.HearingMul : 1f;
        }

        /// <summary>반응 확률 배율: 졸기 중 doze.reactionMul.</summary>
        public float ReactionMultiplier(Human human)
        {
            return human.Doze == DozeState.Sleeping ? _settings.ReactionMul : 1f;
        }

        /// <summary>광분 최소 유지 시간 배율: 졸음 수정자가 있으면 doze.frenzyDurationMul (튜토리얼 배려).</summary>
        public float FrenzyDurationMultiplier(Human human)
        {
            return human.Definition.Traits.Has(HumanModifier.Doze) ? _settings.FrenzyDurationMul : 1f;
        }

        private void StartSleeping(Human human, int tick)
        {
            human.Doze = DozeState.Sleeping;
            human.DozeTelegraphSent = false;
            human.DozeStateEndTick = tick + SimulationTime.ToTicks(_random.Range(_settings.SleepDuration.Min, _settings.SleepDuration.Max));
        }

        private static void Change(Human human, DozeState next, int tick, List<SimulationEvent> events)
        {
            var previous = human.Doze;
            human.Doze = next;
            events.Add(new DozeStateChanged(tick, human.Id, previous, next));
        }
    }
}
