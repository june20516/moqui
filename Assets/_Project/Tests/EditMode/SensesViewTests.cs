using System.Linq;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation.Senses;
using Moqui.Unity.Presentation.Stage;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>모기 감각 표현 (spec/11 §2~4): CO₂, 체온·물린 자국, 은신처 표시 강도.</summary>
    public class SensesViewTests
    {
        private const string SiteId = "forearmR";
        private const float FrameTime = 1f / 60f;
        private const float Tolerance = 1e-3f;

        private GameObject _root;
        private GameSimulation _simulation;
        private SensesSettings _settings;
        private SensesView _view;

        [SetUp]
        public void SetUp()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            _settings = new SensesSettings(tuning);
            _simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            _root = new GameObject("SensesTest");
            var visuals = LevelView.Build(level, _simulation.World, _root.transform, null);
            _view = new GameObject("SensesView").AddComponent<SensesView>();
            _view.transform.SetParent(_root.transform);
            _view.Bind(_simulation, _settings, visuals, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        private SkinSiteState Site => _simulation.Human.SkinSites.First(site => site.PartId == SiteId);

        private void PlacePlayerFromSite(float distance)
        {
            _simulation.Player.Position = Site.Shape.Center + (System.Numerics.Vector3.UnitY * distance);
        }

        [Test]
        public void Heat_OnlyInsideHeatRange_StrongerWhenCloser()
        {
            PlacePlayerFromSite(_settings.HeatRange + 5f);
            _view.Render(FrameTime);
            Assert.That(_view.HeatGlow(SiteId).enabled, Is.False);
            Assert.That(_view.HeatIntensity(SiteId), Is.EqualTo(0f));

            PlacePlayerFromSite(_settings.HeatRange * 0.75f);
            _view.Render(FrameTime);
            float far = _view.HeatIntensity(SiteId);

            PlacePlayerFromSite(_settings.HeatRange * 0.25f);
            _view.Render(FrameTime);
            float near = _view.HeatIntensity(SiteId);

            Assert.That(far, Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(near, Is.EqualTo(0.75f).Within(Tolerance));
            Assert.That(_view.HeatGlow(SiteId).transform.position, Is.EqualTo(Site.Shape.Center.ToUnity()), "glow follows the site");
        }

        [Test]
        public void BiteMark_DotOnlyOnMarkedSite_InsideHeatRange()
        {
            PlacePlayerFromSite(_settings.HeatRange * 0.5f);
            _view.Render(FrameTime);
            Assert.That(_view.BiteDot(SiteId).enabled, Is.False, "no mark yet");

            Site.HasBiteMark = true;
            _view.Render(FrameTime);
            Assert.That(_view.BiteDot(SiteId).enabled, Is.True);
            Assert.That(Vector3.Distance(_view.BiteDot(SiteId).transform.position, Site.Shape.Center.ToUnity()), Is.LessThan(Site.Shape.Radius * _settings.HeatGlowScale + Tolerance));
            var otherSite = _simulation.Human.SkinSites.First(site => site.PartId != SiteId);
            Assert.That(_view.BiteDot(otherSite.PartId).enabled, Is.False, "only the marked site");

            PlacePlayerFromSite(_settings.HeatRange + 5f);
            _view.Render(FrameTime);
            Assert.That(_view.BiteDot(SiteId).enabled, Is.False, "outside heat range");
        }

        [Test]
        public void ShadowCue_IntensityStepsWithAwarenessState_HiddenBeyondCueRange()
        {
            var zone = _simulation.World.Shapes.First(shape => shape.Matches(ShapeFlags.ShadowZone));
            _simulation.Player.Position = zone.Center;

            float[] intensities = new[] { AwarenessState.Safe, AwarenessState.Suspicious, AwarenessState.Frenzy }
                .Select(state =>
                {
                    _simulation.Human.State = state;
                    _view.Render(FrameTime);
                    return _view.ShadowCueIntensity(zone.Id);
                })
                .ToArray();

            Assert.That(intensities[0], Is.EqualTo(_settings.CueIntensitySafe).Within(Tolerance));
            Assert.That(intensities[1], Is.EqualTo(_settings.CueIntensitySuspicious).Within(Tolerance));
            Assert.That(intensities[2], Is.EqualTo(_settings.CueIntensityFrenzy).Within(Tolerance));
            Assert.That(intensities[0], Is.LessThan(intensities[1]));
            Assert.That(intensities[1], Is.LessThan(intensities[2]));

            var surface = ShapeGeometry.Closest(zone, zone.Center + (System.Numerics.Vector3.UnitX * 1000f));
            _simulation.Player.Position = surface.Point + (surface.Normal * (_settings.CueRange + 5f));
            _view.Render(FrameTime);
            Assert.That(_view.ShadowCue(zone.Id).enabled, Is.False);
        }

        [Test]
        public void Co2_PuffsDuringExhale_VisibleBeyondFullFogUpToCo2Range()
        {
            var human = _simulation.Human;
            human.IsExhaling = true;
            human.ExhaleStrength = 1f;
            Vector3 mouth = human.ExhalePosition.ToUnity();
            Assert.That(_settings.Co2VisibleRange, Is.GreaterThan(_settings.FogFullRange), "CO2 must reach beyond full fog");

            float beyondFog = (_settings.FogFullRange + _settings.Co2VisibleRange) * 0.5f;
            _simulation.Player.Position = (mouth + (Vector3.down * beyondFog)).ToCore();
            for (int i = 0; i < 10; i++)
            {
                _view.Render(FrameTime);
            }

            Assert.That(_view.Plume.Puffs.Count, Is.GreaterThan(0));
            Assert.That(_view.VisibleCo2Puffs, Is.EqualTo(_view.Plume.Puffs.Count), "visible beyond fogFullRange");

            _simulation.Player.Position = (mouth + (Vector3.down * (_settings.Co2VisibleRange + 50f))).ToCore();
            _view.Render(FrameTime);
            Assert.That(_view.VisibleCo2Puffs, Is.EqualTo(0), "hidden beyond co2VisibleRange");
        }

        [Test]
        public void Co2Plume_RisesFadesAndExpires_StrongerBreathIsLarger()
        {
            var plume = new Co2Plume(_settings);
            plume.Update(FrameTime, true, Vector3.zero, Vector3.forward, 1f);
            Assert.That(plume.Puffs.Count, Is.EqualTo(1), "first puff at exhale start");
            var puff = plume.Puffs[0];

            plume.Update(_settings.Co2PuffLifetime * 0.5f, false, Vector3.zero, Vector3.forward, 1f);
            Assert.That(puff.Position.y, Is.GreaterThan(0f), "rises");
            Assert.That(puff.Position.z, Is.GreaterThan(0f), "pushed along the breath direction");
            Assert.That(plume.Opacity(puff), Is.LessThan(1f));
            Assert.That(plume.Radius(puff), Is.GreaterThan(_settings.Co2PuffStartRadius));

            plume.Update(_settings.Co2PuffLifetime, false, Vector3.zero, Vector3.forward, 1f);
            Assert.That(plume.Puffs.Count, Is.EqualTo(0), "expired");

            const float drunkStrength = 1.6f;
            plume.Update(FrameTime, true, Vector3.zero, Vector3.forward, drunkStrength);
            Assert.That(plume.Radius(plume.Puffs[0]), Is.EqualTo(_settings.Co2PuffStartRadius * drunkStrength).Within(0.1f));
        }
    

        [Test]
        public void Decoy_MarkerShownAtDecoyWhileActive()
        {
            Assert.That(_view.DecoyMarker, Is.Null, "not equipped");

            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var skills = SkillLoadout.Of((SkillCatalog.DecoyCharm, 1)).WithEquipped(SkillCatalog.DecoyCharm);
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup(skills));
            var visuals = LevelView.Build(level, simulation.World, _root.transform, null);
            var view = new GameObject("DecoyView").AddComponent<SensesView>();
            view.transform.SetParent(_root.transform);
            view.Bind(simulation, _settings, visuals, null);

            view.Render(FrameTime);
            Assert.That(view.DecoyMarker.enabled, Is.False);
            simulation.Step(new PlayerCommand { SkillPressed = true });
            view.Render(FrameTime);
            Assert.That(view.DecoyMarker.enabled, Is.True);
            Assert.That(view.DecoyMarker.transform.position, Is.EqualTo(simulation.Decoy.Position.ToUnity()));
        }
    }
}
