using System;
using System.Linq;
using Moqui.Core.Simulation;
using Moqui.Unity.Editor;
using Moqui.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>플레이어 캐릭터 모키의 애니메이션 7종 (spec/10).</summary>
    public class MokiTests
    {
        private const float FlightSpeed = 60f;

        private static Moqui.Core.Simulation.Player CreatePlayer(PlayerState state, System.Numerics.Vector3 velocity = default)
        {
            return new Moqui.Core.Simulation.Player(System.Numerics.Vector3.Zero, 0.5f, 100f) { State = state, Velocity = velocity };
        }

        [Test]
        public void Pose_HoveringInPlace_IsIdle()
        {
            Assert.That(MokiPoses.From(CreatePlayer(PlayerState.Flying), false, FlightSpeed), Is.EqualTo(MokiPose.Idle));
        }

        [Test]
        public void Pose_FlyingAboveThreshold_IsMove()
        {
            var player = CreatePlayer(PlayerState.Flying, new System.Numerics.Vector3(0f, 0f, FlightSpeed * 0.5f));
            Assert.That(MokiPoses.From(player, false, FlightSpeed), Is.EqualTo(MokiPose.Move));
        }

        [TestCase(PlayerState.Dashing, MokiPose.Dash)]
        [TestCase(PlayerState.Attached, MokiPose.Attach)]
        [TestCase(PlayerState.Trapped, MokiPose.Trapped)]
        [TestCase(PlayerState.Webbed, MokiPose.Trapped)]
        [TestCase(PlayerState.Dead, MokiPose.Death)]
        public void Pose_FollowsPlayerState(PlayerState state, MokiPose expected)
        {
            Assert.That(MokiPoses.From(CreatePlayer(state), false, FlightSpeed), Is.EqualTo(expected));
        }

        [Test]
        public void Pose_AttachedWithSessionAndSuckHeld_IsSuck_ReleasedIsAttach()
        {
            var player = CreatePlayer(PlayerState.Attached);
            player.SuckSession = new SuckSession(null, 0);

            Assert.That(MokiPoses.From(player, true, FlightSpeed), Is.EqualTo(MokiPose.Suck));
            Assert.That(MokiPoses.From(player, false, FlightSpeed), Is.EqualTo(MokiPose.Attach));
        }

        [Test]
        public void Lean_ForwardAndRight_TiltsTowardMotion()
        {
            Vector2 lean = PlayerView.Lean(new Vector3(FlightSpeed, 0f, FlightSpeed), FlightSpeed);

            Assert.That(lean.x, Is.EqualTo(PlayerView.MaxLeanDegrees).Within(1e-4f), "pitch forward");
            Assert.That(lean.y, Is.EqualTo(-PlayerView.MaxLeanDegrees).Within(1e-4f), "roll right");
        }

        /// <summary>벽·천장에 붙으면 모키의 up이 표면 법선과 5° 이내다 (spec/03, M12).</summary>
        [TestCase(0f, -1f, 0f, 30f)]
        [TestCase(-1f, 0f, 0f, 90f)]
        [TestCase(0f, 0f, -1f, 0f)]
        [TestCase(0.6f, 0.8f, 0f, 200f)]
        public void AttachedRotation_UpMatchesSurfaceNormal(float x, float y, float z, float yaw)
        {
            var normal = new Vector3(x, y, z).normalized;
            Quaternion rotation = PlayerView.AttachedRotation(normal, yaw);

            Assert.That(Vector3.Angle(rotation * Vector3.up, normal), Is.LessThan(5f));
        }

        [Test]
        public void Controller_HasSevenAnimatedStates_SelectedByStateParameter()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MokiBuilder.ControllerPath);
            Assert.That(controller, Is.Not.Null, "run Moqui/Rebuild All Sandboxes");
            Assert.That(controller.parameters.Select(p => p.name), Is.EqualTo(new[] { MokiAnimator.StateParameter }));

            var machine = controller.layers[0].stateMachine;
            var expected = Enum.GetNames(typeof(MokiPose));
            Assert.That(machine.states.Select(s => s.state.name), Is.EquivalentTo(expected));
            Assert.That(machine.defaultState.name, Is.EqualTo(nameof(MokiPose.Idle)));

            foreach (var child in machine.states)
            {
                var clip = child.state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null, child.state.name);
                Assert.That(AnimationUtility.GetCurveBindings(clip), Is.Not.Empty, child.state.name);
                int value = (int)Enum.Parse(typeof(MokiPose), child.state.name);
                Assert.That(
                    machine.anyStateTransitions.Any(t => t.destinationState == child.state && t.conditions.Any(c => c.parameter == MokiAnimator.StateParameter && (int)c.threshold == value)),
                    Is.True,
                    child.state.name);
            }
        }

        [Test]
        public void Controller_ClipBindings_ResolveOnSceneModel()
        {
            EditorSceneManager.OpenScene(SandboxSceneBuilder.StageScenePath, OpenSceneMode.Single);
            var player = GameObject.Find("Player");
            var animator = player.GetComponent<Animator>();
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(player.GetComponent<MokiAnimator>().Animator, Is.SameAs(animator));

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    Assert.That(player.transform.Find(binding.path), Is.Not.Null, $"{clip.name}: {binding.path}");
                }
            }
        }
    }
}
