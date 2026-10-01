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
        private readonly ToxinSettings _spray;
        private readonly DrunkSettings _drunk;
        private readonly IRandom _drunkRandom;

        public HumanAttackSystem(AttackSettings attack, FrenzySettings frenzy, IRandom blindSwatRandom, ToxinSettings spray, DrunkSettings drunk, IRandom drunkRandom)
        {
            _attack = attack;
            _frenzy = frenzy;
            _blindSwatRandom = blindSwatRandom;
            _spray = spray;
            _drunk = drunk;
            _drunkRandom = drunkRandom;
        }

        /// <summary>취한 타겟의 첫 무작위 휘두르기를 예약한다.</summary>
        public void Initialize(Human human, int tick)
        {
            if (human.IsDrunk)
            {
                ScheduleDrunkSwat(human, tick);
            }
        }

        public float FrenzyReach => _attack.Reach + _frenzy.ReachBonus;

        /// <summary>진행 중인 공격의 단계를 넘기고, 판정 중이면 플레이어 사망을 처리한다.</summary>
        public void Advance(Human human, Player player, int tick, List<SimulationEvent> events)
        {
            var attack = human.Attack;
            if (attack.Phase == AttackPhase.Telegraph && tick >= attack.TelegraphEndTick)
            {
                attack.Phase = AttackPhase.Active;
                if (attack.Kind == AttackKind.Spray)
                {
                    // 분사는 판정 대신 연무를 만든다 (spec/06). 연무는 ToxinSystem이 가진다.
                    events.Add(new SprayReleased(tick, attack.Target));
                }
            }

            if (attack.Phase == AttackPhase.Active && attack.Kind == AttackKind.Spray)
            {
                if (tick >= attack.ActiveEndTick)
                {
                    attack.Phase = AttackPhase.Recovery;
                }
            }
            else if (attack.Phase == AttackPhase.Active)
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

            // 취한 타겟은 상태와 무관하게 몸 주변 무작위 지점을 친다 (spec/06).
            if (human.IsDrunk && tick >= human.NextDrunkSwatTick)
            {
                Vector3 swatTarget = human.Definition.Position + _drunkRandom.InsideSphere(_drunk.RandomSwatRadius);
                Start(human, AttackKind.DrunkSwat, swatTarget, _attack.SlapRadius, _drunk.SlapTelegraph, _attack.SlapActiveTime, _frenzy.SlapInterval, tick, events);
                ScheduleDrunkSwat(human, tick);
                return;
            }

            if (human.State != AwarenessState.Frenzy)
            {
                return;
            }

            // 모기약 (spec/02 §4, spec/06): 광분 + canSpray + 보이는 플레이어가 손 사거리 밖 spray.useRange 안 + 쿨타임 (D-046).
            float shoulderDistance = human.DistanceToNearestShoulder(player.Position);
            if (human.Definition.Traits.CanSpray
                && perception.PlayerSeen
                && tick >= human.NextSprayTick
                && shoulderDistance > FrenzyReach
                && shoulderDistance <= _spray.UseRange)
            {
                Vector3 hand = human.NearestShoulder(player.Position);
                Vector3 toward = Vector3.Normalize(player.Position - hand);
                Start(human, AttackKind.Spray, hand + (toward * _spray.Travel), 0f, _spray.Telegraph, GameSimulation.DeltaTime, _frenzy.SlapInterval, tick, events);
                human.NextSprayTick = tick + SimulationTime.ToTicks(_spray.Cooldown);
                return;
            }

            if (perception.PlayerSeen && shoulderDistance <= FrenzyReach)
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

        private void ScheduleDrunkSwat(Human human, int tick)
        {
            float interval = _drunkRandom.Range(_drunk.RandomSwatInterval.Min, _drunk.RandomSwatInterval.Max);
            human.NextDrunkSwatTick = tick + SimulationTime.ToTicks(interval);
        }

        private static void Kill(Player player, int tick, List<SimulationEvent> events)
        {
            player.State = PlayerState.Dead;
            player.Velocity = Vector3.Zero;
            events.Add(new PlayerDied(tick, DeathCause.Attack, player.Position));
        }
    }
}
