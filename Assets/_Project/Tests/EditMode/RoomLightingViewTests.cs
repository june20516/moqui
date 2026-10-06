using System.Collections.Generic;
using System.Linq;
using Moqui.Core.Data.Levels;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation.Stage;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>방 분위기 조명 (spec/assets/lighting.md, M14): 데이터대로 조명이 생기고, 빛이 실제처럼 흔들리되 재현 가능하다.</summary>
    public class RoomLightingViewTests
    {
        private const float SampleStep = 0.05f;
        private const float SampleSeconds = 120f;

        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void LivingKitchen_BuildsEveryLight_AndTvScreenGlowsWithTheLight()
        {
            RoomDefinition room = new LevelLoader(new UnityDataSource()).LoadRoom("livingKitchen");
            _root = new GameObject("RoomLightingTest");
            var tv = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tv.transform.SetParent(_root.transform);
            var visuals = new Dictionary<string, GameObject> { ["tv"] = tv };
            var view = _root.AddComponent<RoomLightingView>();

            view.Build(room, visuals);

            Assert.That(view.Lights.Count, Is.EqualTo(room.Lights.Count));
            var tvLight = view.Lights.Single(l => l.name == "RoomLight_tv_screen");
            Assert.That(tvLight.type, Is.EqualTo(LightType.Spot));
            Assert.That(tvLight.shadows, Is.EqualTo(LightShadows.Soft));
            var moon = view.Lights.Single(l => l.name == "RoomLight_moonlight_window");
            Assert.That(moon.cookie, Is.Not.Null, "window cookie (procedural until the texture asset exists)");

            var block = new MaterialPropertyBlock();
            tv.GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.That(block.GetFloat("_SelfIllumination"), Is.EqualTo(RoomLightingView.GlowSelfIllumination));
        }

        /// <summary>휴대폰 빛은 손(오른 팔뚝 끝)을 따라가고, 방 환경광 색이 적용된다 (gulf §13).</summary>
        [Test]
        public void PhoneLight_FollowsTheHand_RoomAmbientApplied()
        {
            var tuning = Moqui.Core.Data.TuningLoader.Load(new UnityDataSource());
            var level = new LevelLoader(new UnityDataSource()).Load("stage03");
            var simulation = new Moqui.Core.Simulation.GameSimulation(Moqui.Core.Simulation.GameSettings.FromTuning(tuning), level.CreateSetup());
            _root = new GameObject("RoomLightingTest");
            var view = _root.AddComponent<RoomLightingView>();

            view.Build(level.Room, null, simulation);

            var phone = view.Lights.Single(l => l.name == "RoomLight_phone_screen");
            Vector3 hand = simulation.Human.Shapes["forearmR"].PointB.ToUnity();
            Assert.That(Vector3.Distance(phone.transform.position, hand + (Vector3.up * RoomLightingView.AttachLift)), Is.LessThan(1e-3f));
            var ambient = level.Room.Ambient.Value;
            Assert.That(RenderSettings.ambientLight.r, Is.EqualTo(ambient.X).Within(1e-3f));
        }

        [TestCase(RoomLightFlicker.Lamp)]
        [TestCase(RoomLightFlicker.Tv)]
        [TestCase(RoomLightFlicker.Fluorescent)]
        [TestCase(RoomLightFlicker.Phone)]
        [TestCase(RoomLightFlicker.Ember)]
        [TestCase(RoomLightFlicker.City)]
        public void Flicker_IsDeterministic_AveragesNearOne_NeverNegative(RoomLightFlicker kind)
        {
            uint seed = AmbientLightCurves.Seed("test");
            float sum = 0f;
            int count = 0;
            for (float t = 0f; t < SampleSeconds; t += SampleStep)
            {
                LightSample sample = AmbientLightCurves.Sample(kind, t, seed);
                Assert.That(sample.Intensity, Is.GreaterThanOrEqualTo(0f));
                Assert.That(AmbientLightCurves.Sample(kind, t, seed).Intensity, Is.EqualTo(sample.Intensity), "same time, same value");
                sum += sample.Intensity;
                count++;
            }

            Assert.That(sum / count, Is.EqualTo(1f).Within(0.2f), "the data intensity stays the average brightness");
        }

        [Test]
        public void Lamp_IsGentle_TvCutsBetweenScenes()
        {
            uint seed = AmbientLightCurves.Seed("tv_screen");
            float lampMin = float.MaxValue;
            float lampMax = float.MinValue;
            int tvCuts = 0;
            float previous = AmbientLightCurves.Sample(RoomLightFlicker.Tv, 0f, seed).Intensity;
            for (float t = SampleStep; t < SampleSeconds; t += SampleStep)
            {
                float lamp = AmbientLightCurves.Sample(RoomLightFlicker.Lamp, t, seed).Intensity;
                lampMin = Mathf.Min(lampMin, lamp);
                lampMax = Mathf.Max(lampMax, lamp);
                float tv = AmbientLightCurves.Sample(RoomLightFlicker.Tv, t, seed).Intensity;
                if (Mathf.Abs(tv - previous) > 0.1f)
                {
                    tvCuts++;
                }

                previous = tv;
            }

            Assert.That(lampMax - lampMin, Is.LessThan(0.1f), "a bulb does not visibly pulse");
            // 장면 길이 1.5~5초 → 120초에 약 24~80번 바뀐다 (비슷한 밝기로 바뀐 장면은 세지 않음).
            Assert.That(tvCuts, Is.InRange(10, 80));
        }

        [Test]
        public void None_IsSteady()
        {
            Assert.That(AmbientLightCurves.Sample(RoomLightFlicker.None, 12.3f, 1u).Intensity, Is.EqualTo(1f));
        }
    }
}
