using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 인간의 공격 진행 (spec/02 §4, §7). 예고 → 판정 → 회복 순서이며, 목표는 예고 시작 시점에 고정된다.
    /// 판정 구에 플레이어 충돌 구가 겹치면 즉사한다. 진행 중(회복 포함)에는 새 공격을 시작하지 않는다.
    /// </summary>
    public sealed class HumanAttackSystem
    {
        private readonly AttackSettings _attack;
        private readonly FrenzySettings _frenzy;
        private readonly IRandom _blindSwatRandom;

        public HumanAttackSystem(AttackSettings attack, FrenzySettings frenzy, IRandom blindSwatRandom)
        {
            _attack = attack;
            _frenzy = frenzy;
            _blindSwatRandom = blindSwatRandom;
        }

        public float FrenzyReach => _attack.Reach + _frenzy.ReachBonus;

        /// <summary>진행 중인 공격의 단계를 넘기고, 판정 중이면 플레이어 사망을 처리한다.</summary>
        public void Advance(Human human, Player player, int tick, List<SimulationEvent> events)
        {
            var attack = human.Attack;
            if (attack.Phase == AttackPhase.Telegraph && tick >= attack.TelegraphEndTick)
            {
                attack.Phase = AttackPhase.Active;
            }

            if (attack.Phase == AttackPhase.Active)
            {
                if (tick >= attack.ActiveEndTick)
                {
                    attack.Phase = AttackPhase.Recovery;
                }
                else if (player.State != PlayerState.Dead
                    && Vector3.Distance(player.Position, attack.Target) <= attack.Radius + player.CollisionRadius)
                {
                    Kill(player, tick, events);
                }
            }

            if (attack.Phase == AttackPhase.Recovery && tick >= attack.RecoveryEndTick)
            {
                attack.Phase = AttackPhase.Idle;
            }
        }

        /// <summary>새 공격을 시작할지 정한다. 이미 진행 중이면 아무것도 하지 않는다.</summary>
        public void Decide(Human human, Player player, in HumanPerception perception, int tick, List<SimulationEvent> events)
        {
            if (human.Attack.IsBusy || player.State == PlayerState.Dead)
            {
                return;
            }

            if (perception.RedZoneTriggered)
            {
                Vector3 facePoint = human.HeadCenter + (human.HeadForward * _attack.ClapOffset);
                Start(human, AttackKind.Clap, facePoint, _attack.ClapRadius, _attack.ClapTelegraph, _attack.ClapActiveTime, _attack.ClapRecovery, tick, events);
                return;
            }

            if (human.State != AwarenessState.Frenzy)
            {
                return;
            }

            if (perception.PlayerSeen && human.DistanceToNearestShoulder(player.Position) <= FrenzyReach)
            {
                Start(human, AttackKind.Slap, player.Position, _attack.SlapRadius, _frenzy.SlapTelegraph, _attack.SlapActiveTime, _frenzy.SlapInterval, tick, events);
                return;
            }

            if (!perception.PlayerSeen
                && human.HasSeenPlayer
                && tick >= human.NextBlindSwatTick
                && human.DistanceToNearestShoulder(human.LastSeenPosition) <= FrenzyReach)
            {
                Vector3 target = human.LastSeenPosition + _blindSwatRandom.InsideSphere(_frenzy.BlindSwatRadius);
                Start(human, AttackKind.BlindSwat, target, _attack.SlapRadius, _frenzy.SlapTelegraph, _attack.SlapActiveTime, _frenzy.SlapInterval, tick, events);
                float interval = _blindSwatRandom.Range(_frenzy.BlindSwatInterval.Min, _frenzy.BlindSwatInterval.Max);
                human.NextBlindSwatTick = tick + SimulationTime.ToTicks(interval);
            }
        }

        public void Start(Human human, AttackKind kind, Vector3 target, float radius, float telegraph, float activeTime, float recovery, int tick, List<SimulationEvent> events)
        {
            var attack = human.Attack;
            int telegraphTicks = SimulationTime.ToTicks(telegraph);
            attack.Kind = kind;
            attack.Target = target;
            attack.Radius = radius;
            attack.Phase = AttackPhase.Telegraph;
            attack.TelegraphEndTick = tick + telegraphTicks;
            attack.ActiveEndTick = attack.TelegraphEndTick + Math.Max(1, SimulationTime.ToTicks(activeTime));
            attack.RecoveryEndTick = attack.ActiveEndTick + SimulationTime.ToTicks(recovery);
            events.Add(new AttackTelegraphStarted(tick, human.Id, kind, target, radius, telegraphTicks));
        }

        private static void Kill(Player player, int tick, List<SimulationEvent> events)
        {
            player.State = PlayerState.Dead;
            player.Velocity = Vector3.Zero;
            events.Add(new PlayerDied(tick, DeathCause.Attack, player.Position));
        }
    }
}
