using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 인간 공격 (spec/02 §7, D-052·D-053). 사람 몸으로 친다:
    /// 예고 = 필요한 자세(기울이기·돌기·일어서기)를 취하며 손을 치켜듦, 타격 = 손이 호를 그리며 목표로 감(사람 손 최고 속도 이하),
    /// 회복 = 손과 자세가 제자리로. 판정은 움직이는 손바닥 구가 지나간 경로다. 목표는 예고 시작 시점에 고정된다.
    /// 팔이 닿지 않는 목표는 치지 않는다.
    /// </summary>
    public sealed class HumanAttackSystem
    {
        // 손 치켜듦·타격 호의 모양 (팔 길이 배수, 표현과 판정 경로를 함께 정하는 몸 동작 비율).
        private const float WindUpRaise = 0.45f;
        private const float WindUpPullBack = 0.25f;
        private const float ClapSpread = 0.45f;
        private const float ClapForward = 0.25f;
        private const float SprayAim = 0.7f;

        /// <summary>예고(치켜듦)·회복 때 손 속도 상한 = 손 최고 속도 × 이 값 (자세 전환으로 어깨가 움직이는 몫을 남긴다).</summary>
        private const float PrepareSpeedRatio = 0.7f;

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

        public void Initialize(Human human, int tick)
        {
            if (human.IsDrunk)
            {
                ScheduleDrunkSwat(human, tick);
            }
        }

        /// <summary>몸을 움직여서라도 손이 닿는 목표인가 (레벨 maxPosture까지).</summary>
        public static bool CanReach(Human human, Vector3 target) => AttackPlanner.TryPlan(human, target, -1, -1, out _);

        public void Advance(Human human, Player player, int tick, List<SimulationEvent> events)
        {
            var attack = human.Attack;
            if (attack.Phase == AttackPhase.Telegraph && tick >= attack.TelegraphEndTick)
            {
                BeginStrike(human, attack, tick, events);
            }

            if (attack.Phase == AttackPhase.Active)
            {
                if (tick >= attack.ActiveEndTick)
                {
                    BeginRecovery(human, attack, tick);
                }
                else
                {
                    StepStrike(human, attack, player, tick, events);
                }
            }

            if (attack.Phase == AttackPhase.Telegraph)
            {
                StepTelegraph(human, attack, tick);
            }
            else if (attack.Phase == AttackPhase.Recovery)
            {
                if (tick >= attack.RecoveryEndTick)
                {
                    Finish(human, attack);
                }
                else
                {
                    StepRecovery(human, attack, tick);
                }
            }
        }

        public void Decide(Human human, Player player, in HumanPerception perception, int tick, List<SimulationEvent> events)
        {
            if (human.Attack.IsBusy || player.State == PlayerState.Dead)
            {
                return;
            }

            if (perception.RedZoneTriggered)
            {
                Vector3 facePoint = human.HeadCenter + (human.HeadForward * _attack.ClapOffset);
                StartClap(human, facePoint, tick, events);
                return;
            }

            // 취한 타겟은 상태와 무관하게 가슴 주변 무작위 지점을 친다 (spec/06). 팔이 닿지 않으면 가까운 어깨 쪽으로 팔 길이 안까지 당긴다.
            if (human.IsDrunk && tick >= human.NextDrunkSwatTick)
            {
                Vector3 swatTarget = ReachableToward(human, human.ChestCenter + _drunkRandom.InsideSphere(_drunk.RandomSwatRadius));
                Start(human, AttackKind.DrunkSwat, swatTarget, _attack.SlapRadius, _drunk.SlapTelegraph, _frenzy.SlapInterval, _attack.HandPeakSpeedDrunk, -1, tick, events);
                ScheduleDrunkSwat(human, tick);
                return;
            }

            if (human.State != AwarenessState.Frenzy)
            {
                return;
            }

            // 모기약 (spec/02 §4, spec/06): 광분 + canSpray + 보이는 플레이어가 손이 닿지 않고 spray.useRange 안 + 쿨타임 (D-046).
            bool reachable = perception.PlayerSeen && CanReach(human, player.Position);
            if (human.Definition.Traits.CanSpray
                && perception.PlayerSeen
                && tick >= human.NextSprayTick
                && !reachable
                && human.DistanceToNearestShoulder(player.Position) <= _spray.UseRange)
            {
                StartSpray(human, player.Position, tick, events);
                human.NextSprayTick = tick + SimulationTime.ToTicks(_spray.Cooldown);
                return;
            }

            if (reachable)
            {
                Start(human, AttackKind.Slap, player.Position, _attack.SlapRadius, _frenzy.SlapTelegraph, _frenzy.SlapInterval, _attack.HandPeakSpeedFrenzy, -1, tick, events);
                return;
            }

            if (!perception.PlayerSeen && human.HasSeenPlayer && tick >= human.NextBlindSwatTick)
            {
                Vector3 target = human.LastSeenPosition + _blindSwatRandom.InsideSphere(_frenzy.BlindSwatRadius);
                float interval = _blindSwatRandom.Range(_frenzy.BlindSwatInterval.Min, _frenzy.BlindSwatInterval.Max);
                human.NextBlindSwatTick = tick + SimulationTime.ToTicks(interval);
                Start(human, AttackKind.BlindSwat, target, _attack.SlapRadius, _frenzy.SlapTelegraph, _frenzy.SlapInterval, _attack.HandPeakSpeedFrenzy, -1, tick, events);
            }
        }

        /// <summary>
        /// 한 손으로 치는 공격을 시작한다. 닿는 자세가 없으면 시작하지 않고 false.
        /// </summary>
        /// <param name="telegraph">예고 기본 시간. 자세 전환 + 최소 예고보다 짧으면 늘린다.</param>
        /// <param name="excludedArm">쓸 수 없는 팔 (모기가 앉은 팔). −1이면 없음.</param>
        public bool Start(Human human, AttackKind kind, Vector3 target, float radius, float telegraph, float recovery, float peakSpeed, int excludedArm, int tick, List<SimulationEvent> events)
        {
            if (!AttackPlanner.TryPlan(human, target, -1, excludedArm, out var plan))
            {
                return false;
            }

            float windUpTime = WindUpDuration(human, kind, target, plan.Arm, peakSpeed);
            Begin(human, kind, target, radius, Math.Max(telegraph, Math.Max(plan.PostureTime + _attack.MinTelegraph, windUpTime)), recovery, peakSpeed, plan.Posture, plan.PostureTime, tick, events);
            human.Attack.ArmA = plan.Arm;
            human.Attack.HandStart[0] = human.Palm(plan.Arm) - human.Shoulder(plan.Arm);
            return true;
        }

        private void StartClap(Human human, Vector3 facePoint, int tick, List<SimulationEvent> events)
        {
            float windUpTime = 0f;
            for (int arm = 0; arm < Math.Min(2, human.Rig.Arms.Count); arm++)
            {
                windUpTime = Math.Max(windUpTime, WindUpDuration(human, AttackKind.Clap, facePoint, arm, _attack.HandPeakSpeedFrenzy));
            }

            Begin(human, AttackKind.Clap, facePoint, _attack.ClapRadius, Math.Max(_attack.ClapTelegraph, Math.Max(_attack.MinTelegraph, windUpTime)), _attack.ClapRecovery,
                _attack.HandPeakSpeedFrenzy, human.Pose.Posture, 0f, tick, events);
            var attack = human.Attack;
            attack.ArmA = human.Rig.Arms.Count > 0 ? 0 : -1;
            attack.ArmB = human.Rig.Arms.Count > 1 ? 1 : -1;
            for (int slot = 0; slot < 2; slot++)
            {
                int arm = attack.ArmAt(slot);
                if (arm >= 0)
                {
                    attack.HandStart[slot] = human.Palm(arm) - human.Shoulder(arm);
                }
            }
        }

        private void StartSpray(Human human, Vector3 playerPosition, int tick, List<SimulationEvent> events)
        {
            int arm = human.NearestArm(playerPosition);
            Vector3 shoulder = human.NearestShoulder(playerPosition);
            Vector3 toward = Vector3.Normalize(playerPosition - shoulder);
            Begin(human, AttackKind.Spray, shoulder + (toward * _spray.Travel), 0f, Math.Max(_spray.Telegraph, _attack.MinTelegraph), _frenzy.SlapInterval,
                _attack.HandPeakSpeedFrenzy, human.Pose.Posture, 0f, tick, events);
            human.Attack.ArmA = arm;
            if (arm >= 0)
            {
                human.Attack.HandStart[0] = human.Palm(arm) - human.Shoulder(arm);
            }
        }

        private static void Begin(Human human, AttackKind kind, Vector3 target, float radius, float telegraph, float recovery, float peakSpeed,
            PostureState posture, float postureTime, int tick, List<SimulationEvent> events)
        {
            var attack = human.Attack;
            int telegraphTicks = Math.Max(1, SimulationTime.ToTicks(telegraph));
            attack.Kind = kind;
            attack.Target = target;
            attack.Radius = radius;
            attack.Phase = AttackPhase.Telegraph;
            attack.StartTick = tick;
            attack.TelegraphEndTick = tick + telegraphTicks;
            attack.ActiveEndTick = attack.TelegraphEndTick + 1;
            attack.RecoveryEndTick = attack.ActiveEndTick + SimulationTime.ToTicks(recovery);
            attack.RecoveryTicks = SimulationTime.ToTicks(recovery);
            attack.HandPeakSpeed = peakSpeed;
            attack.PostureStart = human.Pose.Posture;
            attack.PostureTarget = posture;
            attack.PostureTime = postureTime;
            attack.ArmA = -1;
            attack.ArmB = -1;
            events.Add(new AttackTelegraphStarted(tick, human.Id, kind, target, radius, telegraphTicks));
        }

        private void StepTelegraph(Human human, HumanAttack attack, int tick)
        {
            float elapsed = (tick - attack.StartTick) * GameSimulation.DeltaTime;
            float postureProgress = attack.PostureTime > 0f ? BodyKinematics.SmoothStep(elapsed / attack.PostureTime) : 1f;
            human.Pose.Posture = Lerp(attack.PostureStart, attack.PostureTarget, postureProgress);
            float handProgress = BodyKinematics.SmoothStep((float)(tick - attack.StartTick) / Math.Max(1, attack.TelegraphEndTick - attack.StartTick));
            human.UpdatePose();
            for (int slot = 0; slot < 2; slot++)
            {
                int arm = attack.ArmAt(slot);
                if (arm >= 0)
                {
                    Vector3 shoulder = human.Shoulder(arm);
                    human.Pose.HandTargets[arm] = BodyKinematics.ArcPoint(shoulder, attack.HandStart[slot], WindUp(human, attack.Kind, attack.Target, arm) - shoulder, handProgress, MinRadius(human, arm), SideOut(human, arm));
                }
            }

            human.UpdatePose();
        }

        private void BeginStrike(Human human, HumanAttack attack, int tick, List<SimulationEvent> events)
        {
            attack.Phase = AttackPhase.Active;
            if (attack.Kind == AttackKind.Spray)
            {
                // 분사는 판정 대신 연무를 만든다 (spec/06). 연무는 ToxinSystem이 가진다.
                events.Add(new SprayReleased(tick, attack.Target));
                attack.ActiveTicks = 1;
                attack.ActiveEndTick = tick + 1;
                return;
            }

            // 손은 어깨를 중심으로 돌며 목표로 간다. 타격 시간 = 그 호의 최고 속도가 사람 손 최고 속도를 넘지 않는 가장 짧은 시간.
            float seconds = 0f;
            for (int slot = 0; slot < 2; slot++)
            {
                int arm = attack.ArmAt(slot);
                if (arm < 0)
                {
                    continue;
                }

                Vector3 shoulder = human.Shoulder(arm);
                Vector3 end = attack.Kind == AttackKind.Clap ? attack.Target + (SideOut(human, arm) * (attack.Radius * 0.3f)) : attack.Target;
                attack.StrikeStart[slot] = human.Palm(arm) - shoulder;
                attack.StrikeEnd[slot] = end - shoulder;
                attack.PreviousHand[slot] = human.Palm(arm);
                seconds = Math.Max(seconds, BodyKinematics.ArcDuration(attack.StrikeStart[slot], attack.StrikeEnd[slot], MinRadius(human, arm), attack.HandPeakSpeed));
            }

            attack.ActiveTicks = Math.Max(1, (int)Math.Ceiling(seconds / GameSimulation.DeltaTime));
            attack.ActiveEndTick = tick + attack.ActiveTicks;
            attack.RecoveryEndTick = attack.ActiveEndTick + attack.RecoveryTicks;
        }

        private static void StepStrike(Human human, HumanAttack attack, Player player, int tick, List<SimulationEvent> events)
        {
            if (attack.Kind == AttackKind.Spray)
            {
                return;
            }

            float progress = BodyKinematics.SmoothStep((float)(tick - attack.TelegraphEndTick + 1) / attack.ActiveTicks);
            for (int slot = 0; slot < 2; slot++)
            {
                int arm = attack.ArmAt(slot);
                if (arm >= 0)
                {
                    human.Pose.HandTargets[arm] = StrikePoint(human, attack, slot, progress);
                }
            }

            human.UpdatePose();
            for (int slot = 0; slot < 2; slot++)
            {
                int arm = attack.ArmAt(slot);
                if (arm < 0)
                {
                    continue;
                }

                Vector3 palm = human.Palm(arm);
                if (player.State != PlayerState.Dead
                    && BodyKinematics.DistanceToSegment(player.Position, attack.PreviousHand[slot], palm) <= attack.Radius + player.CollisionRadius)
                {
                    Kill(player, tick, events);
                }

                attack.PreviousHand[slot] = palm;
            }
        }

        private static void BeginRecovery(Human human, HumanAttack attack, int tick)
        {
            attack.Phase = AttackPhase.Recovery;
            attack.RecoveryStartTick = tick;
            attack.RecoveryPostureStart = human.Pose.Posture;
            float seconds = attack.RecoveryTicks * GameSimulation.DeltaTime;
            for (int slot = 0; slot < 2; slot++)
            {
                int arm = attack.ArmAt(slot);
                if (arm >= 0)
                {
                    attack.StrikeEnd[slot] = human.Palm(arm) - human.Shoulder(arm);
                    seconds = Math.Max(seconds, BodyKinematics.ArcDuration(attack.StrikeEnd[slot], attack.HandStart[slot], MinRadius(human, arm), attack.HandPeakSpeed * PrepareSpeedRatio));
                }
            }

            seconds = Math.Max(seconds, AttackPlanner.TransitionTime(human.Pose.Posture, PostureState.Rest, human.Body));
            attack.RecoveryEndTick = tick + Math.Max(1, (int)Math.Ceiling(seconds / GameSimulation.DeltaTime));
        }

        private static void StepRecovery(Human human, HumanAttack attack, int tick)
        {
            float progress = BodyKinematics.SmoothStep((float)(tick - attack.RecoveryStartTick) / Math.Max(1, attack.RecoveryEndTick - attack.RecoveryStartTick));
            human.Pose.Posture = Lerp(attack.RecoveryPostureStart, PostureState.Rest, progress);
            for (int slot = 0; slot < 2; slot++)
            {
                int arm = attack.ArmAt(slot);
                if (arm >= 0)
                {
                    human.Pose.HandTargets[arm] = BodyKinematics.ArcPoint(human.Shoulder(arm), attack.StrikeEnd[slot], attack.HandStart[slot], progress, MinRadius(human, arm), SideOut(human, arm));
                }
            }

            human.UpdatePose();
        }

        private static void Finish(Human human, HumanAttack attack)
        {
            attack.Phase = AttackPhase.Idle;
            human.Pose.Posture = PostureState.Rest;
            for (int i = 0; i < human.Pose.HandTargets.Length; i++)
            {
                human.Pose.HandTargets[i] = null;
            }

            human.UpdatePose();
        }

        /// <summary>타격 중 손 위치 (어깨 중심 호). 테스트·표현에서 같은 경로를 쓴다.</summary>
        public static Vector3 StrikePoint(Human human, HumanAttack attack, int slot, float progress)
        {
            int arm = attack.ArmAt(slot);
            return BodyKinematics.ArcPoint(human.Shoulder(arm), attack.StrikeStart[slot], attack.StrikeEnd[slot], progress, MinRadius(human, arm), SideOut(human, arm));
        }

        private static float MinRadius(Human human, int arm)
        {
            var rig = human.Rig.Arms[arm];
            return BodyKinematics.MinReach(rig.UpperLength, rig.ForearmLength, human.Body.HandReachExtra, human.Body.ElbowFlexMax) + 1f;
        }

        /// <summary>손을 치켜드는 데 필요한 시간 (손 속도 상한 × PrepareSpeedRatio 이하).</summary>
        private float WindUpDuration(Human human, AttackKind kind, Vector3 target, int arm, float peakSpeed)
        {
            Vector3 shoulder = human.Shoulder(arm);
            return BodyKinematics.ArcDuration(human.Palm(arm) - shoulder, WindUp(human, kind, target, arm) - shoulder, MinRadius(human, arm), peakSpeed * PrepareSpeedRatio);
        }

        /// <summary>치켜든 손 위치: 상체 위쪽으로 들고 목표 반대쪽으로 당김 (박수는 두 손을 얼굴 앞 양옆으로 벌림, 분사는 캔을 목표로 겨눔).</summary>
        private Vector3 WindUp(Human human, AttackKind kind, Vector3 target, int arm)
        {
            Vector3 shoulder = human.Shoulder(arm);
            float reach = human.ArmReach(arm);
            Quaternion upper = human.UpperBodyWorldRotation;
            Vector3 up = Vector3.Transform(Vector3.UnitY, upper);
            Vector3 forward = Vector3.Transform(Vector3.UnitZ, upper);
            switch (kind)
            {
                case AttackKind.Clap:
                    return shoulder + (SideOut(human, arm) * (ClapSpread * reach)) + (forward * (ClapForward * reach));
                case AttackKind.Spray:
                    return shoulder + (Vector3.Normalize(target - shoulder) * (SprayAim * reach));
                default:
                    Vector3 away = shoulder - target;
                    away -= up * Vector3.Dot(away, up);
                    Vector3 pullBack = away.LengthSquared() > 1e-6f ? Vector3.Normalize(away) * (WindUpPullBack * reach) : Vector3.Zero;
                    return shoulder + (up * (WindUpRaise * reach)) + pullBack;
            }
        }

        /// <summary>팔이 닿지 않는 점이면 가장 가까운 어깨에서 팔 길이 안쪽으로 당긴 점.</summary>
        private static Vector3 ReachableToward(Human human, Vector3 point)
        {
            if (CanReach(human, point))
            {
                return point;
            }

            int arm = human.NearestArm(point);
            if (arm < 0)
            {
                return point;
            }

            Vector3 shoulder = human.Shoulder(arm);
            Vector3 offset = point - shoulder;
            float limit = human.ArmReach(arm) * 0.9f;
            return offset.Length() > limit ? shoulder + (Vector3.Normalize(offset) * limit) : point;
        }

        private static Vector3 SideOut(Human human, int arm)
        {
            return Vector3.Transform(Vector3.UnitX * human.Rig.Arms[arm].Side, human.UpperBodyWorldRotation);
        }

        private static PostureState Lerp(PostureState from, PostureState to, float t)
        {
            var direction = Vector3.Lerp(from.LeanDirection, to.LeanDirection, t);
            return new PostureState
            {
                LeanDirection = direction.LengthSquared() > 1e-6f ? Vector3.Normalize(direction) : to.LeanDirection,
                LeanAngle = from.LeanAngle + ((to.LeanAngle - from.LeanAngle) * t),
                Twist = from.Twist + ((to.Twist - from.Twist) * t),
                Rise = from.Rise + ((to.Rise - from.Rise) * t),
            };
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
