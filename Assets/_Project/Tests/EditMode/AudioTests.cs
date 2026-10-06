using System.IO;
using System.Linq;
using System.Reflection;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Editor;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Audio;
using Moqui.Unity.Presentation.Sandbox;
using Moqui.Unity.Settings;
using Moqui.Unity.UI.Flow;
using NUnit.Framework;
using UnityEditor;

namespace Moqui.Unity.Tests
{
    /// <summary>사운드 ID 카탈로그와 이벤트 연결 (spec/10).</summary>
    public class AudioTests
    {
        private const string ScriptsFolder = "Assets/_Project/Scripts";

        [Test]
        public void Catalog_HasEverySpecIdWithClip()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(AudioCatalogBuilder.CatalogPath);
            Assert.That(catalog, Is.Not.Null, "run Moqui/Rebuild All Sandboxes");
            foreach (var definition in AudioIds.Definitions)
            {
                var entry = catalog.Find(definition.Id);
                Assert.That(entry, Is.Not.Null, definition.Id);
                Assert.That(entry.Clip, Is.Not.Null, definition.Id);
                Assert.That(entry.Clip.length, Is.GreaterThan(0f), definition.Id);
                Assert.That(entry.Loop, Is.EqualTo(definition.Loop), definition.Id);
                Assert.That(entry.Bus, Is.EqualTo(definition.Bus), definition.Id);
            }
        }

        [Test]
        public void Catalog_CoversSpecList()
        {
            string[] spec =
            {
                "sfx_wing_loop", "sfx_dash", "sfx_attach", "sfx_detach", "sfx_suck_loop", "sfx_slap", "sfx_clap",
                "sfx_frenzy", "sfx_telegraph", "sfx_spray", "sfx_toxin", "sfx_breath", "sfx_dislodge", "sfx_decoy",
                "sfx_drop_trap", "sfx_escape", "sfx_ui_select", "sfx_ui_confirm", "sfx_ui_cancel",
                "amb_stage1", "amb_stage2", "amb_stage3", "amb_stage4", "amb_stage5",
                "sfx_snore", "sfx_wake", "sfx_drip", "sfx_steam", "sfx_wind_loop", "sfx_wind_gust", "sfx_footstep", "bgm_title", "bgm_stage",
            };
            Assert.That(AudioIds.All, Is.SupersetOf(spec));
        }

        [Test]
        public void GeneratedFolder_HasOnlyCatalogClips()
        {
            var files = Directory.GetFiles(AudioCatalogBuilder.GeneratedFolder, "*.wav").Select(Path.GetFileNameWithoutExtension);
            Assert.That(files, Is.EquivalentTo(AudioIds.All));
        }

        /// <summary>모든 ID 상수가 카탈로그 정의 밖의 코드에서 쓰인다 (= 어떤 이벤트·상태·화면에 연결됨). 환경음은 레벨 ID로 고른다.</summary>
        [Test]
        public void EveryId_IsReferencedOutsideDefinitions()
        {
            string sources = string.Join("\n", Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories)
                .Where(path => Path.GetFileName(path) != "AudioIds.cs")
                .Select(File.ReadAllText));
            var constants = typeof(AudioIds).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string));
            foreach (var constant in constants)
            {
                Assert.That(sources, Does.Contain($"AudioIds.{constant.Name}"), constant.Name);
            }

            for (int stage = 1; stage <= AudioIds.StageCount; stage++)
            {
                Assert.That(AudioIds.AmbienceForLevel($"stage0{stage}"), Is.EqualTo(AudioIds.Ambience(stage)));
            }

            Assert.That(AudioIds.AmbienceForLevel("sandbox"), Is.Null);
        }

        [Test]
        public void Events_MapToSounds()
        {
            Assert.That(AudioCues.FromEvent(new PlayerAttached(1, "arm", true)), Is.EqualTo(AudioIds.Attach));
            Assert.That(AudioCues.FromEvent(new PlayerDetached(1, "arm")), Is.EqualTo(AudioIds.Detach));
            Assert.That(AudioCues.FromEvent(new PlayerTrapped(1, "drop")), Is.EqualTo(AudioIds.DropTrap));
            Assert.That(AudioCues.FromEvent(new SprayReleased(1, System.Numerics.Vector3.Zero)), Is.EqualTo(AudioIds.Spray));
            Assert.That(AudioCues.FromEvent(new AwarenessStateChanged(1, "h", AwarenessState.Suspicious, AwarenessState.Frenzy)), Is.EqualTo(AudioIds.Frenzy));
            Assert.That(AudioCues.FromEvent(new AwarenessStateChanged(1, "h", AwarenessState.Safe, AwarenessState.Suspicious)), Is.Null);
        }

        [Test]
        public void Dash_PlaysDashOnStartTick()
        {
            var simulation = CreateFlightSimulation();
            var cues = new AudioCues();
            simulation.Step(PlayerCommand.None);
            Assert.That(cues.OneShots(simulation), Does.Not.Contain(AudioIds.Dash));

            simulation.Step(new PlayerCommand { DashPressed = true });
            Assert.That(cues.OneShots(simulation), Does.Contain(AudioIds.Dash));
        }

        [Test]
        public void Loops_WingWhileFlying_SuckOnlyWhileSucking()
        {
            var simulation = CreateFlightSimulation();
            var idle = AudioCues.Loops(simulation, MokiPose.Idle).ToDictionary(loop => loop.Id);
            Assert.That(idle[AudioIds.WingLoop].Playing, Is.True);
            Assert.That(idle[AudioIds.WingLoop].Pitch, Is.EqualTo(AudioCues.WingPitchIdle).Within(1e-4f));
            Assert.That(idle[AudioIds.SuckLoop].Playing, Is.False);

            var suck = AudioCues.Loops(simulation, MokiPose.Suck).ToDictionary(loop => loop.Id);
            Assert.That(suck[AudioIds.WingLoop].Playing, Is.False);
            Assert.That(suck[AudioIds.SuckLoop].Playing, Is.True);
        }

        /// <summary>바람에 밀리는 동안 바람 반복음이 세기에 따라 커지고 높아지며, 처음 밀릴 때 "휙"이 한 번 난다 (M13).</summary>
        [Test]
        public void Wind_LoopFollowsStrength_GustOnceOnEntering()
        {
            var simulation = CreateFlightSimulation();
            float windSpeed = simulation.Settings.Fan.WindSpeed;
            var cues = new AudioCues();

            var calm = AudioCues.Loops(simulation, MokiPose.Idle).ToDictionary(loop => loop.Id);
            Assert.That(calm[AudioIds.WindLoop].Playing, Is.False);
            Assert.That(cues.OneShots(simulation), Does.Not.Contain(AudioIds.WindGust));

            simulation.Player.ExternalVelocity = new System.Numerics.Vector3(windSpeed * 0.3f, 0f, 0f);
            var weak = AudioCues.Loops(simulation, MokiPose.Idle).ToDictionary(loop => loop.Id);
            Assert.That(cues.OneShots(simulation), Does.Contain(AudioIds.WindGust));
            Assert.That(cues.OneShots(simulation), Does.Not.Contain(AudioIds.WindGust), "gust only when entering");

            simulation.Player.ExternalVelocity = new System.Numerics.Vector3(windSpeed, 0f, 0f);
            var strong = AudioCues.Loops(simulation, MokiPose.Idle).ToDictionary(loop => loop.Id);
            Assert.That(weak[AudioIds.WindLoop].Playing && strong[AudioIds.WindLoop].Playing, Is.True);
            Assert.That(strong[AudioIds.WindLoop].Gain, Is.GreaterThan(weak[AudioIds.WindLoop].Gain));
            Assert.That(strong[AudioIds.WindLoop].Pitch, Is.GreaterThan(weak[AudioIds.WindLoop].Pitch));
            Assert.That(strong[AudioIds.WindLoop].Gain, Is.EqualTo(1f).Within(1e-4f));
        }

        /// <summary>걷는 인간의 걸음(다리 위상 반 바퀴)마다 발소리가 한 번 난다 (spec/02 §9, M14).</summary>
        [Test]
        public void Footstep_OncePerStep()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var level = new LevelLoader(new UnityDataSource()).Load("stage01");
            var simulation = new GameSimulation(GameSettings.FromTuning(tuning), level.CreateSetup());
            var cues = new AudioCues();
            cues.OneShots(simulation);

            simulation.Human.WalkPhase = (float)System.Math.PI * 0.5f;
            Assert.That(cues.OneShots(simulation), Does.Not.Contain(AudioIds.Footstep));
            simulation.Human.WalkPhase = (float)System.Math.PI * 1.1f;
            Assert.That(cues.OneShots(simulation), Does.Contain(AudioIds.Footstep));
            Assert.That(cues.OneShots(simulation), Does.Not.Contain(AudioIds.Footstep));
        }

        [Test]
        public void WingPitch_RisesWithSpeed()
        {
            Assert.That(AudioCues.WingPitch(60f, 60f), Is.EqualTo(AudioCues.WingPitchIdle + AudioCues.WingPitchRange).Within(1e-4f));
            Assert.That(AudioCues.WingPitch(30f, 60f), Is.GreaterThan(AudioCues.WingPitch(0f, 60f)));
        }

        [Test]
        public void UserSettings_ApplyToEngine_SetsSfxAndMusicVolumes()
        {
            var settings = new UserSettings(new MemoryPreferenceStore()) { SfxVolume = 0.4f, MusicVolume = 0.3f };
            try
            {
                settings.ApplyToEngine();
                Assert.That(AudioVolumes.Sfx, Is.EqualTo(0.4f).Within(1e-4f));
                Assert.That(AudioVolumes.Music, Is.EqualTo(0.3f).Within(1e-4f));
                Assert.That(AudioVolumes.For(AudioBus.Music), Is.EqualTo(0.3f).Within(1e-4f));
                Assert.That(AudioVolumes.For(AudioBus.Ambience), Is.EqualTo(0.4f).Within(1e-4f));
            }
            finally
            {
                AudioVolumes.Sfx = 1f;
                AudioVolumes.Music = 1f;
                UnityEngine.AudioListener.volume = 1f;
            }
        }

        private static GameSimulation CreateFlightSimulation()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            return new GameSimulation(GameSettings.FromTuning(tuning), SandboxFlightWorld.Create(), SandboxFlightWorld.PlayerSpawn);
        }
    }
}
