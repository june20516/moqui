using System.Linq;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation.Gimmicks;
using Moqui.Unity.Presentation.Senses;
using Moqui.Unity.Presentation.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>환경 기믹 표현 (spec/06): 선풍기 머리 방향, 모기향 연기, 모기약 연무, 거미줄 머티리얼.</summary>
    public class GimmickViewTests
    {
        private const float FrameTime = 1f / 60f;

        private GameObject _root;
        private GameSimulation _simulation;
        private GimmickView _view;
        private LevelDefinition _level;

        [SetUp]
        public void SetUp()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            _level = new LevelLoader(new UnityDataSource()).Load("stage05");
            _simulation = new GameSimulation(GameSettings.FromTuning(tuning), _level.CreateSetup());
            _root = new GameObject("GimmickTest");
            _view = new GameObject("GimmickView").AddComponent<GimmickView>();
            _view.transform.SetParent(_root.transform);
            _view.Bind(_simulation, new SensesSettings(tuning), null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void FanHead_FollowsOscillatingDirection()
        {
            var fan = _simulation.Fans.Fans[0];
            for (int i = 0; i < 120; i++)
            {
                _simulation.Step(PlayerCommand.None);
            }

            _view.Render(FrameTime);
            Vector3 expected = _simulation.Fans.Direction(fan, _simulation.Tick).ToUnity();
            Assert.That(Vector3.Angle(_view.FanHead(fan.Id).up, expected), Is.LessThan(0.5f));
        }

        /// <summary>에어컨 송풍 날개는 켜진 동안만 보인다 (spec/06, M14).</summary>
        [Test]
        public void AirConditionerVane_VisibleOnlyWhileOn()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var aircon = new FanDefinition("aircon", new System.Numerics.Vector3(0f, 200f, 0f), 180f, -20f, FanKind.AirConditioner, new FanSchedule(1f, 1f, 0f));
            var gimmicks = new GimmickSetup(new[] { aircon }, null, null);
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), new SimulationSetup(new Moqui.Core.Collision.CollisionWorld(), new System.Numerics.Vector3(0f, 100f, 300f), gimmicks: gimmicks));
            var view = new GameObject("AirconView").AddComponent<GimmickView>();
            view.transform.SetParent(_root.transform);
            view.Bind(simulation, new SensesSettings(tuning), null);

            view.Render(FrameTime);
            Assert.That(view.FanHead("aircon").GetComponent<Renderer>().enabled, Is.True, "on at start");
            for (int i = 0; i < 72; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            view.Render(FrameTime);
            Assert.That(view.FanHead("aircon").GetComponent<Renderer>().enabled, Is.False, "off after 1s");
        }

        /// <summary>조명은 켜진 동안만 점광원이 켜진다 (spec/06, M14).</summary>
        [Test]
        public void Lamp_LitOnlyWhileOn()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var center = new System.Numerics.Vector3(0f, 200f, 0f);
            var lamp = new LightDefinition("lamp", center, center, new System.Numerics.Vector3(100f, 100f, 100f), new FanSchedule(1f, 1f, 0f), false);
            var gimmicks = new GimmickSetup(null, null, null, new[] { lamp });
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), new SimulationSetup(new CollisionWorld(), new System.Numerics.Vector3(0f, 100f, 300f), gimmicks: gimmicks));
            var view = new GameObject("LampView").AddComponent<GimmickView>();
            view.transform.SetParent(_root.transform);
            view.Bind(simulation, new SensesSettings(tuning), null);

            simulation.Step(PlayerCommand.None);
            view.Render(FrameTime);
            Assert.That(view.Lamps[0].enabled, Is.True);
            for (int i = 0; i < 72; i++)
            {
                simulation.Step(PlayerCommand.None);
            }

            view.Render(FrameTime);
            Assert.That(view.Lamps[0].enabled, Is.False);
        }

        [Test]
        public void CoilSmoke_RisesFromCoil()
        {
            for (int i = 0; i < 30; i++)
            {
                _view.Render(FrameTime);
            }

            Assert.That(_view.VisibleSmokePuffs, Is.GreaterThan(0));
            var coil = _simulation.Toxin.Coils[0].ToUnity();
            Assert.That(_view.CoilPlumes[0].Puffs.All(p => p.Position.y > coil.y), Is.True);
        }

        [Test]
        public void SprayCloud_DrawnAtCloudWithItsRadius()
        {
            Assert.That(_view.VisibleClouds, Is.EqualTo(0));
            var cloud = _simulation.Toxin.Spawn(new System.Numerics.Vector3(0f, 100f, -100f), _simulation.Tick);
            _simulation.Step(PlayerCommand.None);
            _view.Render(FrameTime);

            Assert.That(_view.VisibleClouds, Is.EqualTo(1));
            var renderer = _view.transform.Find("SprayCloud_0");
            Assert.That(Vector3.Distance(renderer.position, cloud.Position.ToUnity()), Is.LessThan(1e-3f));
            Assert.That(renderer.localScale.x, Is.EqualTo(_simulation.Toxin.Radius(cloud, _simulation.Tick) * 2f).Within(1e-3f));
        }

        [Test]
        public void Webs_UseWebMaterial_InStageScene()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Stage.unity", OpenSceneMode.Single);
            var materials = Object.FindAnyObjectByType<LevelMaterials>();
            Assert.That(materials.Web, Is.Not.Null);
            var objects = LevelView.Build(_level, _level.CreateWorld(), null, materials);
            foreach (var web in _level.Webs)
            {
                Assert.That(objects[web.Id].GetComponent<Renderer>().sharedMaterial, Is.SameAs(materials.Web), web.Id);
            }
        }
    }
}
