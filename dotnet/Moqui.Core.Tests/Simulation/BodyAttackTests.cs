using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;
using static Moqui.Core.Tests.Support.TestSimulations;

namespace Moqui.Core.Tests.Simulation
{
    /// <summary>사람 몸으로 치는 공격 (spec/02 §7, D-052·D-053): 닿는 거리, 몸동작 단계, 관절 한계, 손 속도, 반대 손, 손 경로 판정.</summary>
    public class BodyAttackTests
    {
        private static readonly Vector3 RightShoulder = new Vector3(TestHumans.ShoulderHalfWidth, TestHumans.ShoulderHeight, -5f);

        private static GameSimulation Far() => TestHumans.Simulation(new Vector3(0f, 300f, -400f));

        [Test]
        public void Planner_UsesOnlyAsMuchBodyAsNeeded()
        {
            var human = Far().Human;

            Assert.That(Plan(human, RightShoulder + new Vector3(0f, -10f, 40f)).Level, Is.EqualTo(PostureLevel.Arm), "within arm reach");
            Assert.That(Plan(human, new Vector3(20f, 70f, 65f)).Level, Is.EqualTo(PostureLevel.Lean), "just beyond the arm in front");
            Assert.That(Plan(human, new Vector3(0f, 140f, 60f)).Level, Is.EqualTo(PostureLevel.Rise), "high and far: stand up");
        }

        [Test]
        public void Planner_TargetBehindNeedsTurn_NotJustArm()
        {
            var human = Far().Human;
            var target = new Vector3(55f, 100f, -45f);

            var plan = Plan(human, target);

            Assert.That(plan.Level, Is.GreaterThanOrEqualTo(PostureLevel.Turn), "a shoulder cannot reach behind and up without turning");
            Assert.That(AttackPlanner.CanReach(human, plan.Arm, PostureState.Rest, target), Is.False);
            Assert.That(AttackPlanner.CanReach(human, plan.Arm, plan.Posture, target), Is.True);
        }

        [Test]
        public void Planner_TooFar_NoPlan_AndMaxPostureLimitsBody()
        {
            var human = Far().Human;
            Assert.That(AttackPlanner.TryPlan(human, new Vector3(0f, 100f, 200f), -1, -1, out _), Is.False, "beyond any posture");

            var armOnly = WithMaxPosture(PostureLevel.Arm);
            Assert.That(AttackPlanner.TryPlan(armOnly.Human, new Vector3(20f, 70f, 65f), -1, -1, out _), Is.False, "maxPosture arm cannot lean");
            var leanOnly = WithMaxPosture(PostureLevel.Lean);
            Assert.That(AttackPlanner.TryPlan(leanOnly.Human, new Vector3(0f, 140f, 60f), -1, -1, out _), Is.False, "maxPosture lean cannot stand up");
        }

        [Test]
        public void Frenzy_PlayerSeenBeyondReach_NoSlap()
        {
            var simulation = TestHumans.Simulation(new Vector3(0f, TestHumans.HeadHeight, 180f));
            TestHumans.Provoke(simulation, 100f);
            var starts = new List<AttackTelegraphStarted>();
            for (int i = 0; i < SecondsToTicks(3f); i++)
            {
                simulation.Step(PlayerCommand.None);
                starts.AddRange(simulation.Events.OfType<AttackTelegraphStarted>());
            }

            Assert.That(simulation.Human.State, Is.EqualTo(AwarenessState.Frenzy));
            Assert.That(starts.Where(s => s.Kind == AttackKind.Slap), Is.Empty, "the arm does not stretch to 180u");
        }

        [Test]
        public void Rise_TakesHumanTime_AndHeadAndVisionFollowTheBody()
        {
            var simulation = Far();
            var human = simulation.Human;
            Vector3 headBefore = human.HeadCenter;
            var target = new Vector3(0f, 140f, 60f);

            Assert.That(simulation.HumanSystem.Attacks.Start(human, AttackKind.Slap, target, Settings.Attack.SlapRadius, 0f, 1f, Settings.Attack.HandPeakSpeedFrenzy, -1, simulation.Tick, new List<SimulationEvent>()), Is.True);
            var attack = human.Attack;
            Assert.That((attack.TelegraphEndTick - attack.StartTick) * GameSimulation.DeltaTime, Is.GreaterThanOrEqualTo(Settings.Body.RiseTime), "standing up takes posture.riseTime");

            while (attack.Phase == AttackPhase.Telegraph)
            {
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(human.Pose.Posture.Rise, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(human.HeadCenter.Y - headBefore.Y, Is.EqualTo(Settings.Body.RiseLift).Within(1f), "head rises with the body");
        }

        [Test]
        public void Lean_HeadForwardFollowsUpperBody()
        {
            var human = Far().Human;
            Vector3 forwardBefore = human.HeadForward;

            human.Pose.Posture = new PostureState { LeanDirection = Vector3.UnitZ, LeanAngle = 30f };
            human.UpdatePose();

            float angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(forwardBefore), Vector3.Normalize(human.HeadForward)), -1f, 1f)) * 180f / MathF.PI;
            Assert.That(angle, Is.EqualTo(30f).Within(1f), "the view cone tilts with the torso");
        }

        [TestCase(AttackKind.Slap, 0f, 75f, 50f)]
        [TestCase(AttackKind.Slap, 40f, 60f, 30f)]
        [TestCase(AttackKind.Slap, 0f, 140f, 60f)]
        [TestCase(AttackKind.Slap, 55f, 100f, -45f)]
        [TestCase(AttackKind.ReactSlap, -10f, 40f, 30f)]
        public void Strike_RespectsJointLimitsAndHumanHandSpeed(AttackKind kind, float x, float y, float z)
        {
            var simulation = Far();
            var human = simulation.Human;
            float peak = kind == AttackKind.ReactSlap ? Settings.Attack.HandPeakSpeedReaction : Settings.Attack.HandPeakSpeedFrenzy;
            Assert.That(simulation.HumanSystem.Attacks.Start(human, kind, new Vector3(x, y, z), Settings.Attack.SlapRadius, 0.4f, 1f, peak, -1, simulation.Tick, new List<SimulationEvent>()), Is.True);
            int arm = human.Attack.ArmA;
            Vector3 previous = human.Palm(arm);
            float fastest = 0f;
            float maxFlex = 0f;

            float palmToTargetAtStrikeEnd = float.MaxValue;
            while (human.Attack.IsBusy)
            {
                var phaseBefore = human.Attack.Phase;
                simulation.Step(PlayerCommand.None);
                Vector3 palm = human.Palm(arm);
                if (phaseBefore == AttackPhase.Active && human.Attack.Phase == AttackPhase.Recovery)
                {
                    palmToTargetAtStrikeEnd = Vector3.Distance(previous, new Vector3(x, y, z));
                }

                fastest = Math.Max(fastest, Vector3.Distance(previous, palm) / GameSimulation.DeltaTime);
                previous = palm;
                var upper = human.Shapes[human.Rig.Arms[arm].UpperArmId];
                maxFlex = Math.Max(maxFlex, BodyKinematics.ElbowFlexion(upper.PointA, upper.PointB, palm));
                Assert.That(Vector3.Distance(upper.PointA, upper.PointB), Is.EqualTo(human.Rig.Arms[arm].UpperLength).Within(0.1f), "the upper arm never stretches");
            }

            Assert.That(fastest, Is.LessThanOrEqualTo(peak * 1.05f), "hand speed stays within a human hand");
            Assert.That(fastest, Is.GreaterThan(peak * 0.5f), "and is still a fast swat");
            Assert.That(maxFlex, Is.LessThanOrEqualTo(Settings.Body.ElbowFlexMax + 0.5f));
            Assert.That(palmToTargetAtStrikeEnd, Is.LessThanOrEqualTo(Settings.Attack.SlapRadius), "the palm arrives inside the target sphere");
        }

        [Test]
        public void ReactSlap_OnOwnRightForearm_UsesLeftHand()
        {
            var simulation = TestHumans.Simulation(Vector3.Zero);
            TestHumans.PlaceNearPart(simulation, "forearmR");
            simulation.Step(new PlayerCommand { AttachPressed = true });
            simulation.Human.Attack.Phase = AttackPhase.Idle;
            TestHumans.Site(simulation, "forearmR").Itch = 100f;

            for (int i = 0; i < 5 && !simulation.Human.Attack.IsBusy; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            Assert.That(simulation.Human.Attack.Kind, Is.EqualTo(AttackKind.ReactSlap));
            int arm = simulation.Human.Attack.ArmA;
            Assert.That(simulation.Human.Rig.Arms[arm].ForearmId, Is.EqualTo("forearmL"), "the bitten arm cannot slap itself");
        }

        [Test]
        public void Strike_PlayerOnTheHandPath_NotAtTarget_Dies()
        {
            var simulation = Far();
            var human = simulation.Human;
            var target = new Vector3(20f, 60f, 45f);
            Assert.That(simulation.HumanSystem.Attacks.Start(human, AttackKind.Slap, target, Settings.Attack.SlapRadius, 0.4f, 1f, Settings.Attack.HandPeakSpeedFrenzy, -1, simulation.Tick, new List<SimulationEvent>()), Is.True);
            var attack = human.Attack;
            while (attack.Phase == AttackPhase.Telegraph)
            {
                simulation.Step(PlayerCommand.None);
            }

            // 타격 경로 중간(목표에서 떨어진 곳)에 플레이어를 둔다.
            Vector3 midway = HumanAttackSystem.StrikePoint(human, attack, 0, 0.5f);
            Assert.That(Vector3.Distance(midway, target), Is.GreaterThan(attack.Radius + simulation.Player.CollisionRadius), "midway is outside the old target sphere");
            simulation.Player.Position = midway;
            PlayerDied death = null;
            while (attack.Phase == AttackPhase.Active && death == null)
            {
                simulation.Step(PlayerCommand.None);
                death = simulation.Events.OfType<PlayerDied>().SingleOrDefault();
            }

            Assert.That(death, Is.Not.Null, "the swinging hand hits whatever is on its way");
            Assert.That(death.Cause, Is.EqualTo(DeathCause.Attack));
        }

        /// <summary>전기 모기채를 든 오른팔은 채 길이만큼 더 닿는다 (spec/02 §7, M14).</summary>
        [Test]
        public void Swatter_ExtendsReachOfTheToolArm()
        {
            var plain = WithTool(HumanTool.None).Human;
            var swatter = WithTool(HumanTool.Swatter).Human;
            int right = swatter.ToolArm;
            Assert.That(swatter.Rig.Arms[right].Side, Is.GreaterThan(0f), "held in the right hand");
            Assert.That(swatter.ArmReach(right) - plain.ArmReach(right), Is.EqualTo(Settings.Attack.SwatterLength).Within(1e-3f));

            Vector3 shoulder = swatter.Shoulder(right);
            Vector3 target = shoulder + (Vector3.Normalize(new Vector3(0.3f, -0.2f, 1f)) * (plain.ArmReach(right) + 25f));
            Assert.That(AttackPlanner.CanReach(plain, right, PostureState.Rest, target), Is.False);
            Assert.That(AttackPlanner.CanReach(swatter, right, PostureState.Rest, target), Is.True);
        }

        /// <summary>모기채로 치면 판정이 넓다: 손바닥이면 비껴갈 거리에서도 맞는다.</summary>
        [TestCase(HumanTool.None, false)]
        [TestCase(HumanTool.Swatter, true)]
        public void Swatter_WiderHit(HumanTool tool, bool dies)
        {
            var simulation = WithTool(tool);
            var human = simulation.Human;
            var target = new Vector3(20f, 60f, 45f);
            Assert.That(simulation.HumanSystem.Attacks.Start(human, AttackKind.Slap, target, Settings.Attack.SlapRadius, 0.4f, 1f, Settings.Attack.HandPeakSpeedFrenzy, human.Rig.Arms.First(arm => arm.Side < 0f).Index, simulation.Tick, new List<SimulationEvent>()), Is.True);
            float offset = (Settings.Attack.SlapRadius + Settings.Attack.SwatterRadius) * 0.5f + simulation.Player.CollisionRadius;
            simulation.Player.Position = target + new Vector3(offset, 0f, 0f);

            bool died = false;
            for (int i = 0; i < SecondsToTicks(3f) && !died; i++)
            {
                simulation.Step(PlayerCommand.None);
                died = simulation.Player.State == PlayerState.Dead;
            }

            Assert.That(died, Is.EqualTo(dies));
        }

        private static GameSimulation WithTool(HumanTool tool)
        {
            var seated = TestHumans.Seated();
            var definition = new HumanDefinition(seated.Id, seated.Position, seated.FacingYaw, seated.Parts, seated.HeadPartId, seated.ShoulderLocals, seated.IdleLookYaws,
                seated.Actions, seated.Traits, seated.FacingPitch, seated.RestPitch, seated.MaxPosture, null, tool);
            var simulation = TestHumans.Simulation(new Vector3(0f, 300f, -400f), human: definition);
            TestHumans.DisableReactions(simulation);
            return simulation;
        }

        private static AttackPlan Plan(Human human, Vector3 target)
        {
            Assert.That(AttackPlanner.TryPlan(human, target, -1, -1, out var plan), Is.True, $"reachable {target}");
            return plan;
        }

        private static GameSimulation WithMaxPosture(PostureLevel level)
        {
            var seated = TestHumans.Seated();
            var limited = new HumanDefinition(seated.Id, seated.Position, seated.FacingYaw, seated.Parts, seated.HeadPartId, seated.ShoulderLocals, seated.IdleLookYaws,
                seated.Actions, seated.Traits, seated.FacingPitch, seated.RestPitch, level);
            return TestHumans.Simulation(new Vector3(0f, 300f, -400f), human: limited);
        }
    }
}
