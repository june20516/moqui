using Moqui.Core.Simulation;
using Moqui.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>인간 공격 팔 3단계 자세 (spec/10): 예고 → 타격 → 회복.</summary>
    public class HumanArmPoseTests
    {
        private static readonly Vector3 Shoulder = new Vector3(0f, 50f, 0f);
        private static readonly Vector3 Target = new Vector3(0f, 40f, 40f);
        private const float HeadRadius = 10f;

        [Test]
        public void WindUp_IsAboveShoulder_AndAwayFromTarget()
        {
            Vector3 windUp = HumanArmPose.WindUp(Shoulder, Target, Vector3.up, HeadRadius);

            Assert.That(windUp.y, Is.EqualTo(Shoulder.y + (HumanArmPose.WindUpRaise * HeadRadius)).Within(1e-3f));
            Assert.That(Vector3.Distance(windUp, Target), Is.GreaterThan(Vector3.Distance(Shoulder, Target)));
        }

        [Test]
        public void Hand_MovesShoulderToWindUpToTargetAndBack()
        {
            Vector3 windUp = HumanArmPose.WindUp(Shoulder, Target, Vector3.up, HeadRadius);

            AssertNear(HumanArmPose.Hand(AttackPhase.Telegraph, 0f, Shoulder, windUp, Target), Shoulder);
            AssertNear(HumanArmPose.Hand(AttackPhase.Telegraph, 1f, Shoulder, windUp, Target), windUp);
            AssertNear(HumanArmPose.Hand(AttackPhase.Active, 0f, Shoulder, windUp, Target), windUp);
            AssertNear(HumanArmPose.Hand(AttackPhase.Active, 1f, Shoulder, windUp, Target), Target);
            AssertNear(HumanArmPose.Hand(AttackPhase.Recovery, 0f, Shoulder, windUp, Target), Target);
            AssertNear(HumanArmPose.Hand(AttackPhase.Recovery, 1f, Shoulder, windUp, Target), Shoulder);
            AssertNear(HumanArmPose.Hand(AttackPhase.Idle, 0.5f, Shoulder, windUp, Target), Shoulder);
        }

        [TestCase(10, 10, 20, 0f)]
        [TestCase(15, 10, 20, 0.5f)]
        [TestCase(25, 10, 20, 1f)]
        [TestCase(10, 10, 10, 1f)]
        public void Progress_IsFractionOfPhase(int tick, int start, int end, float expected)
        {
            Assert.That(HumanArmPose.Progress(tick, start, end), Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void PhaseEndTick_MatchesCurrentPhase()
        {
            var attack = new HumanAttack { TelegraphEndTick = 10, ActiveEndTick = 20, RecoveryEndTick = 30 };
            attack.Phase = AttackPhase.Telegraph;
            Assert.That(HumanArmPose.PhaseEndTick(attack), Is.EqualTo(10));
            attack.Phase = AttackPhase.Active;
            Assert.That(HumanArmPose.PhaseEndTick(attack), Is.EqualTo(20));
            attack.Phase = AttackPhase.Recovery;
            Assert.That(HumanArmPose.PhaseEndTick(attack), Is.EqualTo(30));
        }

        private static void AssertNear(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(1e-3f), $"{actual} vs {expected}");
        }
    }
}
