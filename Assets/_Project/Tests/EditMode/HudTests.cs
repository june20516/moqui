using System.Linq;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Core.Tutorial;
using Moqui.Unity.Data;
using Moqui.Unity.Settings;
using Moqui.Unity.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>HUD (spec/08): 모델 값 반영, 화면 밖 머리 화살표, 화면 밖 공격 예고·경고음, 광분 중 은신처 방향, 프롬프트.</summary>
    public class HudTests
    {
        private const float Tolerance = 1e-3f;
        private const float CameraDistance = 200f;

        private sealed class FakeAudio : IHudAudio
        {
            public int TelegraphPlays { get; private set; }

            public void PlayTelegraph() => TelegraphPlays++;
        }

        private Tuning _tuning;
        private GameSimulation _simulation;
        private Camera _camera;
        private HudView _view;
        private FakeAudio _audio;
        private HudPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _tuning = TuningLoader.Load(new UnityDataSource());
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            _simulation = new GameSimulation(GameSettings.FromTuning(_tuning), level.CreateSetup());
            _camera = new GameObject("HudTestCamera").AddComponent<Camera>();
            LookAtHead(true);
            _view = new GameObject("Hud", typeof(RectTransform)).AddComponent<HudView>();
            _view.Build();
            _audio = new FakeAudio();
            _presenter = new HudPresenter(_view, _audio);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_view.gameObject);
            Object.DestroyImmediate(_camera.gameObject);
        }

        private Vector3 Head => _simulation.Human.HeadCenter.ToUnity();

        /// <summary>머리 앞 200u에서 머리를 보거나(true) 반대쪽을 본다(false).</summary>
        private void LookAtHead(bool facing)
        {
            Vector3 position = Head + (_simulation.Human.HeadForward.ToUnity() * CameraDistance);
            _camera.transform.position = position;
            _camera.transform.rotation = Quaternion.LookRotation(facing ? Head - position : position - Head);
        }

        private HudState Present(bool firstPerson = false)
        {
            var state = HudState.Compute(_simulation, _camera, firstPerson, _tuning);
            _presenter.Present(state, 0f);
            return state;
        }

        /// <summary>1인칭 비행 중 대시할 수 있으면 조준점 둘레 고리, 3인칭이나 스태미나가 모자라면 없다 (gulf §5).</summary>
        [Test]
        public void DashAim_FirstPersonWhenDashReady()
        {
            Assert.That(Present(firstPerson: true).DashAimVisible, Is.True);
            Assert.That(_view.DashAim.gameObject.activeSelf, Is.True);
            Assert.That(Present(firstPerson: false).DashAimVisible, Is.False);

            _simulation.Player.Stamina = 0f;
            Assert.That(Present(firstPerson: true).DashAimVisible, Is.False);
            Assert.That(_view.DashAim.gameObject.activeSelf, Is.False);
        }

        /// <summary>정밀 비행 중 가장자리가 조용히 어두워진다 (gulf §6).</summary>
        [Test]
        public void PrecisionFlight_DarkensScreenEdges()
        {
            Present();
            Assert.That(_view.PrecisionVignette.gameObject.activeSelf, Is.False);

            _simulation.Step(new PlayerCommand { PrecisionHeld = true });
            Present();
            Assert.That(_view.PrecisionVignette.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ModelValues_AreReflectedInHudElements()
        {
            var settings = _simulation.Settings;
            var player = _simulation.Player;
            var human = _simulation.Human;
            player.BloodGauge = 45f;
            player.Stamina = settings.Stamina.Max * 0.5f;
            player.ExhaustedRemaining = 1f;
            player.WetRemaining = 2.5f;
            player.Humidity = 30f;
            human.BiteMarkCount = 3;
            human.State = AwarenessState.Suspicious;
            human.Awareness = settings.Awareness.FrenzyEnter * 0.5f;
            human.PlayerOccluded = true;

            Present();

            Assert.That(_view.BloodFill.fillAmount, Is.EqualTo(0.45f).Within(Tolerance));
            Assert.That(_view.BloodText.text, Is.EqualTo("45%"));
            Assert.That(_view.VisibleBiteDots, Is.EqualTo(3));
            Assert.That(_view.Eye.color, Is.EqualTo(HudView.AwarenessColor(AwarenessState.Suspicious)));
            Assert.That(_view.EyeFill.gameObject.activeSelf, Is.True);
            Assert.That(_view.EyeFill.fillAmount, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(_view.CalmRing.gameObject.activeSelf, Is.False);
            Assert.That(_view.StatusText.text, Is.EqualTo("가려짐"));
            Assert.That(_view.StaminaFill.fillAmount, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(_view.StaminaFill.color, Is.Not.EqualTo(_view.HumidityFill.color), "exhausted bar is grey");
            Assert.That(_view.DashTick.anchoredPosition.x, Is.GreaterThan(0f), "dash cost tick");
            Assert.That(_view.WetText.text, Does.Contain("2.5"));
            Assert.That(_view.HumidityRoot.activeSelf, Is.True);
            Assert.That(_view.HumidityFill.fillAmount, Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(_view.ItchRing.gameObject.activeSelf, Is.False, "itch ring only while sucking");
            Assert.That(_view.Crosshair.gameObject.activeSelf, Is.False, "third person, not sucking");

            Present(firstPerson: true);
            Assert.That(_view.Crosshair.gameObject.activeSelf, Is.True, "first person always shows crosshair");

            human.State = AwarenessState.Frenzy;
            human.FrenzyMinRemaining = 3.2f;
            human.CalmProgress = 0.4f;
            player.IsHidden = true;
            player.Humidity = 0f;
            player.WetRemaining = 0f;
            Present();

            Assert.That(_view.CalmRing.gameObject.activeSelf, Is.True);
            Assert.That(_view.CalmRing.fillAmount, Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(_view.FrenzyText.text, Is.EqualTo("3.2"));
            Assert.That(_view.StatusText.text, Is.EqualTo("은신"));
            Assert.That(_view.HumidityRoot.activeSelf, Is.False, "humidity gauge only when > 0");
            Assert.That(_view.WetText.text, Is.Empty);
        }

        [Test]
        public void Satiety_HighlightedOnlyWhenSpeedMultiplierBelowThreshold()
        {
            _simulation.Player.BloodGauge = 0f;
            Assert.That(Present().SatietyHighlighted, Is.False);
            Color normal = _view.SatietyIcon.color;

            _simulation.Player.BloodGauge = SuckSystem.GaugeMax;
            Assert.That(Present().SatietyHighlighted, Is.True);
            Assert.That(_view.SatietyIcon.color, Is.Not.EqualTo(normal));
            Assert.That(_view.SatietyIcon.sprite, Is.Not.SameAs(_view.BiteDotSprite), "satiety does not look like a bite dot (M12)");
            Assert.That(_view.SatietyLabel.text, Is.EqualTo("포만"));
        }

        [Test]
        public void HeadArrow_VisibleOnlyWhenHeadOffscreen()
        {
            LookAtHead(true);
            Present();
            Assert.That(_view.HeadArrow.gameObject.activeSelf, Is.False);

            LookAtHead(false);
            Present();
            Assert.That(_view.HeadArrow.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void OffscreenTelegraph_EdgeWarningAndSoundOnStart()
        {
            var attack = _simulation.Human.Attack;
            LookAtHead(false);
            attack.Phase = AttackPhase.Telegraph;
            attack.Target = _simulation.Human.HeadCenter;

            Present();
            Assert.That(_view.AttackWarning.gameObject.activeSelf, Is.True);
            Assert.That(_audio.TelegraphPlays, Is.EqualTo(1));

            Present();
            Assert.That(_audio.TelegraphPlays, Is.EqualTo(1), "once per telegraph");

            attack.Phase = AttackPhase.Idle;
            Present();
            Assert.That(_view.AttackWarning.gameObject.activeSelf, Is.False);

            LookAtHead(true);
            attack.Phase = AttackPhase.Telegraph;
            Present();
            Assert.That(_view.AttackWarning.gameObject.activeSelf, Is.False, "on-screen telegraph has no edge warning");
            Assert.That(_audio.TelegraphPlays, Is.EqualTo(2), "sound for every telegraph");
        }

        [Test]
        public void HidingDirection_OnlyDuringFrenzy()
        {
            var human = _simulation.Human;
            foreach (var state in new[] { AwarenessState.Safe, AwarenessState.Suspicious })
            {
                human.State = state;
                Present();
                Assert.That(_view.HidingArrow.gameObject.activeSelf, Is.False, state.ToString());
            }

            human.State = AwarenessState.Frenzy;
            Present();
            Assert.That(_view.HidingArrow.gameObject.activeSelf, Is.True);

            _simulation.Player.IsHidden = true;
            Present();
            Assert.That(_view.HidingArrow.gameObject.activeSelf, Is.False, "already hidden");
        }

        /// <summary>흡혈 중 이벤트마다 조준점 위에 경고 문구가 뜨고, 흡혈 세션이 없으면 사라진다 (spec/04 §8, M13).</summary>
        [Test]
        public void SuckEventWarnings_ShowDuringSessionOnly()
        {
            var human = _simulation.Human;
            var site = human.SkinSites.First();
            _simulation.Player.SuckSession = new SuckSession(site, 0);
            var expected = new[]
            {
                (SuckEventKind.Twitch, SuckEventPhase.Active, HudSuckWarning.Twitch),
                (SuckEventKind.Shift, SuckEventPhase.Telegraph, HudSuckWarning.ShiftComing),
                (SuckEventKind.Shift, SuckEventPhase.Active, HudSuckWarning.Riding),
                (SuckEventKind.Glance, SuckEventPhase.Telegraph, HudSuckWarning.Glance),
                (SuckEventKind.Glance, SuckEventPhase.Return, HudSuckWarning.Glance),
            };
            foreach (var (kind, phase, warning) in expected)
            {
                human.SuckEvent.Kind = kind;
                human.SuckEvent.Phase = phase;
                var state = Present();
                Assert.That(state.SuckWarning, Is.EqualTo(warning), $"{kind}/{phase}");
                Assert.That(_view.SuckWarningText.gameObject.activeSelf, Is.True);
                Assert.That(_view.SuckWarningText.text, Is.Not.Empty);
            }

            _simulation.Player.SuckSession = null;
            Present();
            Assert.That(_view.SuckWarningText.gameObject.activeSelf, Is.False);
            human.SuckEvent.Clear();
        }

        [Test]
        public void Prompts_FollowPlayerStateAndInputDevice()
        {
            var wall = _simulation.World.Shapes.First(shape => shape.Matches(ShapeFlags.Attachable) && !shape.Matches(ShapeFlags.SkinSite) && shape.Type == ShapeType.Box);
            var surface = ShapeGeometry.Closest(wall, wall.Center + (System.Numerics.Vector3.UnitY * 10000f));
            _simulation.Player.Position = surface.Point + (surface.Normal * (_simulation.Settings.Attach.AttachRange * 0.5f));
            Present();
            Assert.That(_view.PromptText.text, Is.EqualTo("F: 착지"));

            _view.UseGamepadLabels = true;
            Present();
            Assert.That(_view.PromptText.text, Is.EqualTo("B: 착지"));

            _view.UseGamepadLabels = false;
            _simulation.Player.State = PlayerState.Trapped;
            _simulation.Player.EscapePresses = 1;
            Present();
            Assert.That(_view.PromptText.text, Is.EqualTo($"우클릭 ×{_simulation.Settings.Water.EscapePresses - 1}!"));
        }

        [Test]
        public void TutorialHints_FollowTrackerSteps_AndCanBeTurnedOff()
        {
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var tracker = new TutorialTracker(level.Tutorial, new TutorialSettings(_tuning));
            var hints = new TutorialHints(new MemoryPreferenceStore());
            Assert.That(hints.Enabled, Is.True, "on by default");
            string moveText = hints.CurrentText(tracker, gamepad: false);
            Assert.That(moveText, Is.Not.Empty);
            Assert.That(hints.CurrentText(tracker, gamepad: true), Is.Not.EqualTo(moveText), "device specific wording");

            for (int i = 0; i < 70; i++)
            {
                _simulation.Step(new PlayerCommand { Move = new System.Numerics.Vector2(0f, 1f) });
                tracker.Observe(_simulation);
            }

            Assert.That(tracker.CurrentStep, Is.EqualTo("look"));
            Assert.That(hints.CurrentText(tracker, gamepad: false), Is.Not.EqualTo(moveText), "advanced to the next hint");

            hints.Enabled = false;
            Assert.That(hints.CurrentText(tracker, gamepad: false), Is.Empty);
        }

        [Test]
        public void TutorialHints_SettingPersistsInStore_AndEveryLevelStepHasText()
        {
            var store = new MemoryPreferenceStore();
            new TutorialHints(store).Enabled = false;
            Assert.That(new TutorialHints(store).Enabled, Is.False);

            foreach (string id in new[] { "stage01", "stage02" })
            {
                var level = new LevelLoader(new UnityDataSource()).Load(id);
                Assert.That(level.Tutorial.All(TutorialHints.HasText), Is.True, id);
            }
        }
    

        [Test]
        public void EdgeMarkers_StayInsideSafeBandAvoidingTopAndBottomHud()
        {
            var random = new System.Random(7);
            Vector2 margin = HudState.EdgeMargin;
            for (int i = 0; i < 200; i++)
            {
                var direction = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
                Vector3 point = _camera.transform.position + (direction.normalized * 500f);
                var marker = ScreenEdge.OffscreenMarker(_camera, point, margin);
                if (!marker.Visible)
                {
                    continue;
                }

                Assert.That(marker.Viewport.x, Is.InRange(margin.x - Tolerance, 1f - margin.x + Tolerance));
                Assert.That(marker.Viewport.y, Is.InRange(margin.y - Tolerance, 1f - margin.y + Tolerance));
            }
        }
    

        [Test]
        public void ActiveSkill_SlotShowsOnlyWhenEquipped_WithCooldown()
        {
            Present();
            Assert.That(_view.ActiveSkillRoot.activeSelf, Is.False, "nothing equipped");

            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var skills = SkillLoadout.Of((SkillCatalog.DecoyCharm, 1)).WithEquipped(SkillCatalog.DecoyCharm);
            _simulation = new GameSimulation(GameSettings.FromTuning(_tuning), level.CreateSetup(skills));
            Present();
            Assert.That(_view.ActiveSkillRoot.activeSelf, Is.True);
            Assert.That(_view.ActiveSkillText.text, Is.EqualTo("Q"), "ready shows the key");

            _simulation.Step(new PlayerCommand { SkillPressed = true });
            var state = Present();
            Assert.That(state.ActiveSkillInUse, Is.True);
            Assert.That(state.ActiveSkillCooldown, Is.GreaterThan(0f));
            Assert.That(_view.ActiveSkillText.text, Does.EndWith("s"), "cooldown seconds");
        }
    

        [Test]
        public void ToxinGauge_OnlyWhenPoisoned_TierIconMatches()
        {
            var toxin = _simulation.Settings.Toxin;
            _view.SetToxinTiers(toxin.Tier1 / 100f, toxin.Tier2 / 100f, toxin.Tier3 / 100f);
            Present();
            Assert.That(_view.ToxinRoot.activeSelf, Is.False, "hidden at 0");

            var cases = new[] { (toxin.Tier1 - 1f, "중독"), (toxin.Tier1, "중독 · 끊김"), (toxin.Tier2, "중독 · 반전"), (toxin.Tier3, "중독 · 랜덤") };
            foreach (var (value, label) in cases)
            {
                _simulation.Player.Toxin = value;
                var state = Present();
                Assert.That(_view.ToxinRoot.activeSelf, Is.True, $"{value}");
                Assert.That(_view.ToxinFill.fillAmount, Is.EqualTo(value / 100f).Within(1e-4f));
                Assert.That(_view.ToxinTierText.text, Is.EqualTo(label), $"{value}");
                Assert.That(_view.ToxinVignette.color.a > 0f, Is.EqualTo(state.ToxinTier > 0), "green edge only with a debuff");
            }

            Assert.That(_view.ToxinTicks, Has.Count.EqualTo(3));
            Assert.That(_view.ToxinTicks[1].anchoredPosition.x, Is.GreaterThan(_view.ToxinTicks[0].anchoredPosition.x));
            _simulation.Player.ToxinFloor = 40f;
            Present();
            Assert.That(_view.ToxinFloorMarker.gameObject.activeSelf, Is.True, "coil floor marker");
        }
    }
}
