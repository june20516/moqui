using System.Collections.Generic;
using System.Linq;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation;
using Moqui.Unity.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>모키 표현 큐 (spec/12): 조건에 맞는 큐가 나오고, 표시기를 바꿔 끼울 수 있다.</summary>
    public class MokiCueTests
    {
        private sealed class RecordingPresenter : IMokiCuePresenter
        {
            public readonly List<string> Played = new List<string>();
            public readonly HashSet<string> Loops = new HashSet<string>();

            public void Play(MokiCueDefinition cue) => Played.Add(cue.Id);

            public void SetLoop(MokiCueDefinition cue, bool active)
            {
                if (active)
                {
                    Loops.Add(cue.Id);
                }
                else
                {
                    Loops.Remove(cue.Id);
                }
            }
        }

        private GameSimulation _simulation;
        private MokiCueSystem _cues;
        private RecordingPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var level = new LevelLoader(new UnityDataSource()).Load("stage01");
            _simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            _simulation.HumanSystem.Reactions.ExtraMultiplier = 0f;
            _simulation.HumanSystem.Reactions.LandingSkillMultiplier = 0f;
            _simulation.HumanSystem.SuckEvents.Enabled = false;
            _cues = new MokiCueSystem(tuning.GetFloat("hud.satietyHighlightLevel"));
            _presenter = new RecordingPresenter();
        }

        private void Step(PlayerCommand command, int ticks = 1)
        {
            for (int i = 0; i < ticks; i++)
            {
                _simulation.Step(command);
                _cues.Step(_simulation, _presenter);
            }
        }

        private void PlaceNear(string partId)
        {
            var shape = _simulation.Human.Shapes[partId];
            var surface = ShapeGeometry.Closest(shape, shape.Center + (System.Numerics.Vector3.UnitX * 50f));
            _simulation.Player.Position = surface.Point + (surface.Normal * 1f);
            _simulation.Player.Velocity = System.Numerics.Vector3.Zero;
        }

        [Test]
        public void LandingAndWand_CuesFollowTheAction()
        {
            PlaceNear("torso");
            Step(PlayerCommand.None);
            Assert.That(_presenter.Loops, Does.Contain(MokiCueIds.LandReady), "can land on the clothes (feedforward)");
            Assert.That(_presenter.Loops, Does.Not.Contain(MokiCueIds.WandAim), "clothes are not bare skin");

            PlaceNear("forearmR");
            Step(PlayerCommand.None);
            Assert.That(_presenter.Loops, Does.Contain(MokiCueIds.WandAim), "bare skin: the wand reacts (gulf §3)");
            Assert.That(_presenter.Loops, Does.Not.Contain(MokiCueIds.LandReady));

            Step(new PlayerCommand { AttachPressed = true });
            Assert.That(_presenter.Played, Does.Contain(MokiCueIds.Land));
            Assert.That(_presenter.Loops, Does.Not.Contain(MokiCueIds.WandAim));

            Step(new PlayerCommand { SuckHeld = true }, 3);
            Assert.That(_presenter.Played, Does.Contain(MokiCueIds.WandIn));
            Assert.That(_presenter.Loops, Does.Contain(MokiCueIds.WandDrink));

            Step(new PlayerCommand { SuckHeld = true, Move = new System.Numerics.Vector2(0f, 1f) });
            Assert.That(_presenter.Played, Does.Contain(MokiCueIds.WandTug), "the wand holds the body");
            Assert.That(_simulation.Player.State, Is.EqualTo(PlayerState.Attached));

            Step(PlayerCommand.None);
            Assert.That(_presenter.Played, Does.Contain(MokiCueIds.WandOut));
            Assert.That(_presenter.Loops, Does.Not.Contain(MokiCueIds.WandDrink));

            Step(new PlayerCommand { Move = new System.Numerics.Vector2(0f, 1f) });
            Assert.That(_presenter.Played, Does.Contain(MokiCueIds.Detach));
        }

        [Test]
        public void ClothAndMiss_CuesExplainWhyNothingHappened()
        {
            Step(new PlayerCommand { AttachPressed = true });
            Assert.That(_presenter.Played, Does.Contain(MokiCueIds.LandMiss), "nothing to land on");

            PlaceNear("torso");
            Step(new PlayerCommand { AttachPressed = true });
            Step(new PlayerCommand { SuckHeld = true });
            Assert.That(_presenter.Played, Does.Contain(MokiCueIds.WandCloth), "clothed body part");
        }

        /// <summary>모든 큐가 모델·VFX 요구 문서에 있다 (spec/12 수용 기준).</summary>
        [Test]
        public void EveryCue_IsSpecifiedInVfxDoc()
        {
            string doc = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "..", "spec", "assets", "vfx", "moki-cues.md"));
            foreach (string id in MokiCueCatalog.All.Keys)
            {
                Assert.That(doc, Does.Contain($"| `{id}` |"), id);
            }
        }

        [Test]
        public void EveryCue_HasTextForTheTextPresenter()
        {
            Assert.That(MokiCueCatalog.All.Values.All(cue => !string.IsNullOrEmpty(cue.Text)), Is.True);
            Assert.That(typeof(MokiCueIds).GetFields().Length, Is.EqualTo(MokiCueCatalog.All.Count));
        }

        [Test]
        public void TextPresenter_OneShotFades_LoopStaysUntilOff()
        {
            var cameraObject = new GameObject("CueCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var presenter = new GameObject("Cues").AddComponent<TextCuePresenter>();
            try
            {
                presenter.Bind(camera, () => camera.transform.position + (camera.transform.forward * 10f));
                presenter.Play(MokiCueCatalog.All[MokiCueIds.Land]);
                presenter.SetLoop(MokiCueCatalog.All[MokiCueIds.WandDrink], true);
                presenter.Tick(0.1f);
                Assert.That(presenter.VisibleTexts, Is.EquivalentTo(new[] { "사뿐", "쪽…" }));

                presenter.Tick(TextCuePresenter.Lifetime);
                Assert.That(presenter.VisibleTexts, Is.EquivalentTo(new[] { "쪽…" }), "one-shot gone, loop stays");

                presenter.SetLoop(MokiCueCatalog.All[MokiCueIds.WandDrink], false);
                Assert.That(presenter.VisibleTexts, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(presenter.gameObject);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
