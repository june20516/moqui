using System.Collections.Generic;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 어그로 상태머신과 머리 행동 (spec/02 §3~4).
    /// Safe ↔ Suspicious는 진입 40 / 이탈 20의 히스테리시스, 100이면 Frenzy.
    /// Frenzy는 최소 유지 시간이 지난 뒤 calmTime 동안 연속으로 보지 못하면 exitValue로 진정해 Suspicious가 된다.
    /// </summary>
    public sealed class HumanBrain
    {
        private readonly AwarenessSettings _awareness;
        private readonly FrenzySettings _frenzy;
        private readonly HeadSettings _head;
        private readonly HeadController _headController;

        public HumanBrain(AwarenessSettings awareness, FrenzySettings frenzy, HeadSettings head)
        {
            _awareness = awareness;
            _frenzy = frenzy;
            _head = head;
            _headController = new HeadController(head);
        }

        public void UpdateState(Human human, in HumanPerception perception, Player player, int tick, List<SimulationEvent> events)
        {
            if (perception.PlayerSeen)
            {
                human.HasSeenPlayer = true;
                human.LastSeenPosition = player.Position;
            }

            human.PlayerVisible = perception.PlayerSeen;

            switch (human.State)
            {
                case AwarenessState.Safe:
                case AwarenessState.Suspicious:
                    if (human.Awareness >= _awareness.FrenzyEnter)
                    {
                        EnterFrenzy(human, tick, events);
                    }
                    else if (human.State == AwarenessState.Safe && human.Awareness >= _awareness.SuspiciousEnter)
                    {
                        ChangeState(human, AwarenessState.Suspicious, tick, events);
                    }
                    else if (human.State == AwarenessState.Suspicious && human.Awareness < _awareness.SuspiciousExit)
                    {
                        ChangeState(human, AwarenessState.Safe, tick, events);
                    }

                    break;
                case AwarenessState.Frenzy:
                    human.UnseenTicks = perception.PlayerSeen ? 0 : human.UnseenTicks + 1;
                    bool minimumElapsed = SimulationTime.HasElapsed(human.FrenzyEnteredTick, tick, _frenzy.MinDuration);
                    if (minimumElapsed && human.UnseenTicks >= SimulationTime.ToTicks(_frenzy.CalmTime))
                    {
                        human.Awareness = _frenzy.ExitValue;
                        human.LastStimulusPosition = human.LastSeenPosition;
                        human.LastStimulusTick = tick;
                        ChangeState(human, AwarenessState.Suspicious, tick, events);
                    }

                    break;
            }
        }

        public void UpdateHead(Human human, Player player, int tick, float deltaTime)
        {
            switch (human.State)
            {
                case AwarenessState.Safe:
                    float idleYaw = human.Definition.IdleLookYaws[human.IdleLookIndex % human.Definition.IdleLookYaws.Count];
                    if (_headController.TurnToward(human, idleYaw, 0f, _head.IdleTurnSpeed, deltaTime))
                    {
                        human.IdleLookIndex = (human.IdleLookIndex + 1) % human.Definition.IdleLookYaws.Count;
                    }

                    break;
                case AwarenessState.Suspicious:
                    int stareStart = System.Math.Max(human.StateEnteredTick, human.LastStimulusTick);
                    if (!SimulationTime.HasElapsed(stareStart, tick, _head.SuspiciousStareTime))
                    {
                        _headController.TurnTowardPoint(human, human.LastStimulusPosition, _head.SuspiciousTurnSpeed, deltaTime);
                        break;
                    }

                    human.AnglesToward(human.LastStimulusPosition, out float stimulusYaw, out float stimulusPitch);
                    float searchYaw = stimulusYaw + (human.SearchSign * _head.SearchAngle);
                    if (_headController.TurnToward(human, searchYaw, stimulusPitch, _head.SuspiciousTurnSpeed, deltaTime)
                        || _headController.ClampYaw(searchYaw) == human.HeadYaw)
                    {
                        human.SearchSign = -human.SearchSign;
                    }

                    break;
                case AwarenessState.Frenzy:
                    if (human.PlayerVisible)
                    {
                        _headController.TurnTowardPoint(human, player.Position, _head.FrenzyTurnSpeed, deltaTime);
                    }
                    else if (human.HasSeenPlayer)
                    {
                        _headController.TurnTowardPoint(human, human.LastSeenPosition, _head.FrenzyTurnSpeed, deltaTime);
                    }
                    else if (human.HasStimulus)
                    {
                        _headController.TurnTowardPoint(human, human.LastStimulusPosition, _head.FrenzyTurnSpeed, deltaTime);
                    }
                    else
                    {
                        _headController.TurnToward(human, 0f, 0f, _head.FrenzyTurnSpeed, deltaTime);
                    }

                    break;
            }
        }

        private void EnterFrenzy(Human human, int tick, List<SimulationEvent> events)
        {
            human.Awareness = _awareness.FrenzyEnter;
            human.FrenzyEnteredTick = tick;
            human.UnseenTicks = 0;
            human.FrenzyCount++;
            human.NextBlindSwatTick = tick;
            ChangeState(human, AwarenessState.Frenzy, tick, events);
        }

        private static void ChangeState(Human human, AwarenessState next, int tick, List<SimulationEvent> events)
        {
            var previous = human.State;
            human.State = next;
            human.StateEnteredTick = tick;
            events.Add(new AwarenessStateChanged(tick, human.Id, previous, next));
        }
    }
}
