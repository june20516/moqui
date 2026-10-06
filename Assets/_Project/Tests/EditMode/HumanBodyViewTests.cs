using System.Collections.Generic;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>인간이 실제 팔로 친다 (spec/02, D-052): 별도 팔 오브젝트 없이 팔 그림이 Core 팔 캡슐을 따른다.</summary>
    public class HumanBodyViewTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        /// <summary>살아 있는 몸: 숨 주기에 따라 몸통이 부풀고, 졸면 눈을 감는다 (표현 전용, M14).</summary>
        [Test]
        public void Breath_SwellsTorso_AsleepClosesEyes()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var human = simulation.Human;
            _root = new GameObject("HumanViewTest");
            var view = _root.AddComponent<HumanView>();
            view.Build(human);

            human.BreathPhase = 0f;
            view.Refresh(1);
            Assert.That(view.TorsoSwell, Is.EqualTo(1f).Within(1e-4f), "breathed out");
            human.BreathPhase = 0.5f;
            view.Refresh(1);
            Assert.That(view.TorsoSwell, Is.EqualTo(1f + HumanView.BreathSwell).Within(1e-4f), "breathed in");

            human.Doze = DozeState.Sleeping;
            view.Refresh(1);
            Assert.That(view.EyesClosed, Is.True);
        }

        /// <summary>F 착지 미끄러짐: 처음 빠르고 끝에서 감속해 0.15초에 정확히 붙은 자리 (gulf §2).</summary>
        [Test]
        public void SnapGlide_EasesOutAndArrives()
        {
            var from = Vector3.zero;
            var to = new Vector3(0f, 0f, 6f);

            Vector3 half = PlayerView.Glide(from, to, PlayerView.SnapGlideSeconds * 0.5f);

            Assert.That(half.z, Is.GreaterThan(3f), "ease-out covers more than half in the first half");
            Assert.That(PlayerView.Glide(from, to, PlayerView.SnapGlideSeconds), Is.EqualTo(to));
            Assert.That(PlayerView.Glide(from, to, float.PositiveInfinity), Is.EqualTo(to));
        }

        /// <summary>모기 소리가 귀 근처에서 들리면 귀가 움찔하고, 졸면 움찔하지 않는다 (gulf §6).</summary>
        [Test]
        public void Ears_TwitchWhileInEarZone()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var human = simulation.Human;
            _root = new GameObject("HumanViewTest");
            var view = _root.AddComponent<HumanView>();
            view.Build(human);

            human.PlayerInEarZone = true;
            human.Doze = DozeState.Awake;
            view.Refresh(1);
            Assert.That(view.EarsTwitching, Is.True);

            human.PlayerInEarZone = false;
            view.Refresh(2);
            Assert.That(view.EarsTwitching, Is.False);
        }

        /// <summary>눈동자가 고개보다 먼저 목표 쪽으로 가고, 경계하면 머리 위에 표시가 뜬다 (gulf §7·§12).</summary>
        [Test]
        public void Eyes_LeadTheHead_AlertMarkerWhenSuspicious()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var human = simulation.Human;
            _root = new GameObject("HumanViewTest");
            var view = _root.AddComponent<HumanView>();
            view.Build(human);

            human.HeadTargetYaw = human.HeadYaw + 60f;
            human.HeadTargetPitch = human.HeadPitch;
            view.Refresh(1);
            Assert.That(view.EyeLead.x, Is.EqualTo(1f).Within(1e-4f), "eyes go first, fully to the side");
            Assert.That(view.AlertMarkerVisible, Is.False, "calm");

            human.HeadTargetYaw = human.HeadYaw;
            human.State = AwarenessState.Suspicious;
            human.Awareness = 50f;
            view.Refresh(2);
            Assert.That(view.EyeLead.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(view.AlertMarkerVisible, Is.True);
        }

        /// <summary>날갯짓은 빠를수록 빠르고 정밀 비행이면 느리다. 흡혈 중이면 지팡이 하트가 분홍 빛을 낸다 (gulf §12·§13).</summary>
        [Test]
        public void FlapSpeed_FollowsSpeed_WandGlowsWhileDrinking()
        {
            Assert.That(MokiAnimator.FlapSpeed(MokiPose.Move, 1f, false), Is.GreaterThan(MokiAnimator.FlapSpeed(MokiPose.Idle, 0f, false)));
            Assert.That(MokiAnimator.FlapSpeed(MokiPose.Idle, 0f, true), Is.LessThan(MokiAnimator.FlapSpeed(MokiPose.Idle, 0f, false)));
            Assert.That(MokiAnimator.FlapSpeed(MokiPose.Suck, 1f, false), Is.EqualTo(1f));

            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            simulation.HumanSystem.Reactions.ExtraMultiplier = 0f;
            simulation.HumanSystem.SuckEvents.Enabled = false;
            var shape = simulation.Human.Shapes["forearmR"];
            var surface = Moqui.Core.Collision.ShapeGeometry.Closest(shape, shape.Center + (System.Numerics.Vector3.UnitX * 50f));
            simulation.Player.Position = surface.Point + surface.Normal;
            _root = new GameObject("PlayerViewTest");
            var player = _root.AddComponent<PlayerView>();

            player.RefreshWandLight(simulation, 0f);
            Assert.That(player.WandLightOn, Is.False);

            simulation.Step(new PlayerCommand { AttachPressed = true });
            for (int i = 0; i < 3; i++)
            {
                simulation.Step(new PlayerCommand { SuckHeld = true });
            }

            player.RefreshWandLight(simulation, 0f);
            Assert.That(player.WandLightOn, Is.True);
        }

        [Test]
        public void HoverBob_LargestWhenStill_ZeroAtFullSpeed()
        {
            float quarter = 0.25f / PlayerView.HoverBobFrequency;
            Assert.That(PlayerView.HoverBob(quarter, 0f), Is.EqualTo(PlayerView.HoverBobAmplitude).Within(1e-4f));
            Assert.That(PlayerView.HoverBob(quarter, 1f), Is.EqualTo(0f).Within(1e-4f));
        }

        /// <summary>함께 있는 인간도 그려지고(인간별 뷰), 전기 모기채는 채 끝(판정 중심)을 따라간다 (spec/02 §7·§10, M14).</summary>
        [Test]
        public void Companion_HasItsOwnView_SwatterFollowsToolTip()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var primary = level.Human;
            var friend = new HumanDefinition("friend", primary.Position + new System.Numerics.Vector3(200f, 0f, 0f), primary.FacingYaw, primary.Parts, primary.HeadPartId, primary.ShoulderLocals,
                primary.IdleLookYaws, primary.Actions, primary.Traits, primary.FacingPitch, primary.RestPitch, primary.MaxPosture, null, HumanTool.Swatter);
            var setup = new SimulationSetup(new Moqui.Core.Collision.CollisionWorld(), level.PlayerSpawn, primary, level.Seed, companions: new[] { friend });
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), setup);
            _root = new GameObject("HumanViewTest");
            var view = _root.AddComponent<HumanView>();
            view.Build(simulation.Human);
            var friendView = view.AddCompanion(simulation.Humans[1]);
            view.Refresh(simulation.Tick);

            Assert.That(view.Companions, Has.Count.EqualTo(1));
            Assert.That(view.SwatterHead, Is.Null, "the primary human has no tool");
            var friendHuman = simulation.Humans[1];
            Assert.That(friendView.SwatterHead, Is.Not.Null);
            Assert.That(Vector3.Distance(friendView.SwatterHead.position, friendHuman.Palm(friendHuman.ToolArm).ToUnity()), Is.LessThan(1e-3f));
        }

        /// <summary>공격 예고 표시가 예고 진행률에 따라 변하고(좁혀 오는 고리·차오름), 판정 순간 번쩍임으로 바뀐다 (spec/02 §7, M12).</summary>
        [Test]
        public void Telegraph_IndicatorChangesWithProgress_ThenFlashesOnStrike()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var human = simulation.Human;
            _root = new GameObject("HumanViewTest");
            var view = _root.AddComponent<HumanView>();
            view.Build(human);
            int arm = human.Rig.Arms[0].Side > 0 ? 0 : 1;
            var target = (human.Shoulder(arm).ToUnity() + new Vector3(0f, -20f, 0f) + (human.HeadForward.ToUnity() * 35f)).ToCore();
            Assert.That(simulation.HumanSystem.Attacks.Start(human, AttackKind.Slap, target, 12f, 0.6f, 1f, 500f, -1, simulation.Tick, new List<SimulationEvent>()), Is.True);

            var block = new MaterialPropertyBlock();
            float ProgressShown()
            {
                view.TelegraphIndicator.GetPropertyBlock(block);
                return block.GetFloat("_Progress");
            }

            simulation.Step(PlayerCommand.None);
            view.Refresh(simulation.Tick);
            float early = ProgressShown();
            Assert.That(view.TelegraphIndicator.gameObject.activeSelf, Is.True);
            Assert.That(view.TelegraphIndicator.sharedMaterial.shader.name, Is.EqualTo("Moqui/TelegraphRing"));
            Assert.That(_root.transform.Find("AttackApproach").gameObject.activeSelf, Is.True, "the hand's approach is shown");

            int middle = (human.Attack.TelegraphEndTick - simulation.Tick) / 2;
            for (int i = 0; i < middle; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            view.Refresh(simulation.Tick);
            float later = ProgressShown();
            Assert.That(later, Is.GreaterThan(early + 0.2f), "the ring closes in as the telegraph runs");
            view.TelegraphIndicator.GetPropertyBlock(block);
            Assert.That(block.GetFloat("_Strike"), Is.EqualTo(0f));

            while (human.Attack.Phase != AttackPhase.Active)
            {
                simulation.Step(PlayerCommand.None);
            }

            view.Refresh(simulation.Tick);
            view.TelegraphIndicator.GetPropertyBlock(block);
            Assert.That(block.GetFloat("_Strike"), Is.EqualTo(1f), "flashes when the hand strikes");
        }

        [Test]
        public void Attack_ArmVisualsFollowCoreArmCapsules_NoExtraArm()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var human = simulation.Human;
            _root = new GameObject("HumanViewTest");
            var view = _root.AddComponent<HumanView>();
            view.Build(human);

            // 오른쪽 어깨 앞 아래를 치는 손바닥 공격을 타격 중간까지 진행한다.
            int arm = human.Rig.Arms[0].Side > 0 ? 0 : 1;
            Vector3 shoulder = human.Shoulder(arm).ToUnity();
            var target = (shoulder + new Vector3(0f, -20f, 0f) + (human.HeadForward.ToUnity() * 35f)).ToCore();
            Assert.That(simulation.HumanSystem.Attacks.Start(human, AttackKind.Slap, target, 12f, 0.4f, 1f, 500f, -1, simulation.Tick, new List<SimulationEvent>()), Is.True);
            while (human.Attack.Phase != AttackPhase.Active)
            {
                simulation.Step(PlayerCommand.None);
            }

            simulation.Step(PlayerCommand.None);
            view.Refresh(simulation.Tick);

            Assert.That(_root.transform.Find("AttackArm"), Is.Null, "no separate arm pops out");
            Assert.That(_root.transform.Find("AttackFist"), Is.Null);
            var rig = human.Rig.Arms[human.Attack.ArmA];
            foreach (string partId in new[] { rig.UpperArmId, rig.ForearmId })
            {
                var shape = human.Shapes[partId];
                Transform visual = _root.transform.Find(shape.Id);
                Assert.That(visual, Is.Not.Null, partId);
                Assert.That(Vector3.Distance(visual.position, shape.Center.ToUnity()), Is.LessThan(0.01f), $"{partId} visual follows the Core capsule");
            }

            Assert.That(Vector3.Distance(human.Palm(human.Attack.ArmA).ToUnity(), shoulder), Is.GreaterThan(10f), "the real arm is swinging, away from rest");
        }
    }
}
