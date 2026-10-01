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
