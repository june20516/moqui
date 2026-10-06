using System.Collections.Generic;
using Moqui.Core.Data;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Stage;
using Moqui.Unity.Settings;
using Moqui.Unity.UI;
using Moqui.Unity.UI.Flow;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>화면 흐름 (spec/08): 버튼 이벤트를 직접 호출해 전이·잠금·구매·장착·설정 저장을 확인한다.</summary>
    public class FlowTests
    {
        private sealed class FakeNavigator : ISceneNavigator
        {
            public List<ScreenId> Loaded { get; } = new List<ScreenId>();

            public ScreenId? Last => Loaded.Count > 0 ? Loaded[Loaded.Count - 1] : (ScreenId?)null;

            public bool QuitRequested { get; private set; }

            public void Load(ScreenId screen) => Loaded.Add(screen);

            public void Quit() => QuitRequested = true;
        }

        private sealed class MemoryStorage : ISaveStorage
        {
            public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();

            public bool Exists(string fileName) => Files.ContainsKey(fileName);

            public string Read(string fileName) => Files[fileName];

            public void Write(string fileName, string text) => Files[fileName] = text;

            public void Move(string fromFileName, string toFileName)
            {
                Files[toFileName] = Files[fromFileName];
                Files.Remove(fromFileName);
            }

            public void Delete(string fileName) => Files.Remove(fileName);
        }

        private readonly List<GameObject> _created = new List<GameObject>();
        private Tuning _tuning;
        private MemoryStorage _storage;
        private MemoryPreferenceStore _preferences;
        private GameSession _session;
        private FakeNavigator _navigator;
        private ScreenFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _tuning = TuningLoader.Load(new UnityDataSource());
            _storage = new MemoryStorage();
            _preferences = new MemoryPreferenceStore();
            _session = new GameSession(_tuning, new SaveStore(_storage), _preferences, StageCatalogs.Repo);
            _navigator = new FakeNavigator();
            _flow = new ScreenFlow(_session, _navigator);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                Object.DestroyImmediate(go);
            }

            foreach (var system in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>())
            {
                Object.DestroyImmediate(system.gameObject);
            }

            StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
            StageBootstrap.RequestedSkills = SkillLoadout.None;
        }

        private T Screen<T>() where T : ScreenBase
        {
            var go = new GameObject(typeof(T).Name);
            _created.Add(go);
            var screen = go.AddComponent<T>();
            screen.Initialize(_flow);
            return screen;
        }

        private static StageResult FastClean(float seconds = 30f) => new StageResult(SimulationTime.ToTicks(seconds), 0, 0);

        [Test]
        public void Title_ButtonsLeadToStageSelectSettingsResetAndQuit()
        {
            var title = Screen<TitleScreen>();

            title.SettingsButton.onClick.Invoke();
            Assert.That(title.Settings.IsOpen, Is.True);
            title.Settings.CloseButton.onClick.Invoke();
            Assert.That(title.Settings.IsOpen, Is.False);

            _session.Save.BloodPoints = 500;
            _session.Persist();
            title.ResetButton.onClick.Invoke();
            Assert.That(title.ResetConfirm.IsOpen, Is.True);
            title.ResetConfirm.NoButton.onClick.Invoke();
            Assert.That(_session.Save.BloodPoints, Is.EqualTo(500), "cancel keeps data");
            title.ResetButton.onClick.Invoke();
            title.ResetConfirm.YesButton.onClick.Invoke();
            Assert.That(_session.Save.BloodPoints, Is.EqualTo(0));
            Assert.That(_storage.Exists(SaveStore.FileName), Is.False);

            title.StartButton.onClick.Invoke();
            Assert.That(_navigator.Last, Is.EqualTo(ScreenId.StageSelect));
            title.QuitButton.onClick.Invoke();
            Assert.That(_navigator.QuitRequested, Is.True);
        }

        [Test]
        public void StageSelect_LockedStagesCannotStart_UnlockAfterClear()
        {
            var select = Screen<StageSelectScreen>();
            Assert.That(select.StageButtons[0].interactable, Is.True);
            Assert.That(select.StageButtons[1].interactable, Is.False, "stage 2 locked");
            Assert.That(select.ChapterIndex, Is.EqualTo(0), "opens on the chapter in progress");
            select.NextChapterButton.onClick.Invoke();
            Assert.That(select.ChapterIndex, Is.EqualTo(1));
            Assert.That(select.StageButtons[0].interactable, Is.False, "next chapter locked");
            select.PreviousChapterButton.onClick.Invoke();

            select.StageButtons[1].onClick.Invoke();
            Assert.That(_flow.StartStage("stage02"), Is.False);
            Assert.That(_flow.StartStage("notInCatalog"), Is.False);
            Assert.That(_navigator.Loaded, Is.Empty, "locked stage does not load");

            _session.SelectedLevelId = "stage01";
            _flow.CompleteStage(StageOutcome.Cleared, FastClean(), null, 30f);
            select.Refresh();
            Assert.That(select.StageButtons[1].interactable, Is.True);
            Assert.That(select.RecordTexts[0].text, Does.Contain("0:30"));

            select.StageButtons[1].onClick.Invoke();
            Assert.That(_navigator.Last, Is.EqualTo(ScreenId.Stage));
            Assert.That(StageBootstrap.RequestedLevelId, Is.EqualTo("stage02"));

            select.BackButton.onClick.Invoke();
            Assert.That(_navigator.Last, Is.EqualTo(ScreenId.Title));
        }

        [Test]
        public void Result_RecordsAndSaves_RetrySkillsStageSelectAndEnding()
        {
            _session.SelectedLevelId = "stage01";
            var info = _flow.CompleteStage(StageOutcome.Cleared, FastClean(), null, 30f);
            Assert.That(info.Reward.Total, Is.GreaterThan(0));
            Assert.That(SaveSerializer.Deserialize(_storage.Read(SaveStore.FileName)).BloodPoints, Is.EqualTo(info.Reward.Total), "saved on result");
            Assert.That(info.UnlocksEnding, Is.False);

            var died = _flow.CompleteStage(StageOutcome.Died, FastClean(), DeathCause.Attack, 12f);
            Assert.That(died.Reward, Is.Null);
            Assert.That(_session.Save.BloodPoints, Is.EqualTo(info.Reward.Total), "failure pays nothing");

            _flow.Retry();
            Assert.That(_navigator.Last, Is.EqualTo(ScreenId.Stage));
            Assert.That(StageBootstrap.RequestedLevelId, Is.EqualTo("stage01"));
            _flow.OpenStageSelect(openSkills: true);
            Assert.That(_navigator.Last, Is.EqualTo(ScreenId.StageSelect));
            var select = Screen<StageSelectScreen>();
            Assert.That(select.Skills.IsOpen, Is.True, "Result → Skills opens the skills panel");

            _session.SelectedLevelId = StageCatalogs.Repo.Stages[StageCatalogs.Repo.Stages.Count - 1].LevelId;
            var final = _flow.CompleteStage(StageOutcome.Cleared, FastClean(), null, 30f);
            Assert.That(final.UnlocksEnding, Is.True, "last stage in the catalog, first clear → ending");
            Assert.That(_flow.CompleteStage(StageOutcome.Cleared, FastClean(), null, 30f).UnlocksEnding, Is.False, "only the first clear");
            _flow.OpenEnding();
            Assert.That(_navigator.Last, Is.EqualTo(ScreenId.Ending));

            var ending = Screen<EndingScreen>();
            Assert.That(ending.Lines.text, Does.Contain("Thanks for playing"));
            ending.ContinueButton.onClick.Invoke();
            Assert.That(_navigator.Last, Is.EqualTo(ScreenId.Title));
        }

        [Test]
        public void Skills_PurchaseAndEquip_AreSaved_AndCarriedIntoStage()
        {
            _session.Save.BloodPoints = 80 + 60;
            var select = Screen<StageSelectScreen>();
            select.SkillsButton.onClick.Invoke();
            var skills = select.Skills;
            Assert.That(skills.IsOpen, Is.True);

            skills.TabButtons[(int)SkillCategory.Misc].onClick.Invoke();
            Assert.That(skills.Rows[SkillCatalog.DecoyCharm].Root.activeSelf, Is.True);
            Assert.That(skills.Rows[SkillCatalog.SwiftWings].Root.activeSelf, Is.False, "other tab hidden");
            Assert.That(skills.Rows[SkillCatalog.DecoyCharm].EquipButton.interactable, Is.False, "not owned yet");

            skills.Rows[SkillCatalog.DecoyCharm].BuyButton.onClick.Invoke();
            skills.Rows[SkillCatalog.DecoyCharm].EquipButton.onClick.Invoke();
            skills.TabButtons[(int)SkillCategory.Stats].onClick.Invoke();
            skills.Rows[SkillCatalog.SwiftWings].BuyButton.onClick.Invoke();
            Assert.That(skills.Rows[SkillCatalog.SwiftWings].BuyButton.interactable, Is.False, "no points left");

            var saved = SaveSerializer.Deserialize(_storage.Read(SaveStore.FileName));
            Assert.That(saved.SkillLevel(SkillCatalog.DecoyCharm), Is.EqualTo(1));
            Assert.That(saved.SkillLevel(SkillCatalog.SwiftWings), Is.EqualTo(1));
            Assert.That(saved.EquippedActive, Is.EqualTo(SkillCatalog.DecoyCharm));
            Assert.That(saved.BloodPoints, Is.EqualTo(0));

            _flow.StartStage("stage01");
            Assert.That(StageBootstrap.RequestedSkills.Level(SkillCatalog.SwiftWings), Is.EqualTo(1));
            Assert.That(StageBootstrap.RequestedSkills.EquippedActive, Is.EqualTo(SkillCatalog.DecoyCharm));
        }

        [Test]
        public void Settings_ChangedThroughPanel_PersistInStore()
        {
            var title = Screen<TitleScreen>();
            var panel = title.Settings;
            panel.Open(title.SettingsButton);
            panel.Controls["sensitivity"].Secondary.onClick.Invoke();
            panel.Controls["invertY"].Primary.onClick.Invoke();
            panel.Controls["tutorialHints"].Primary.onClick.Invoke();
            panel.Controls["defaultView"].Primary.onClick.Invoke();
            panel.Controls["flightMode"].Primary.onClick.Invoke();
            panel.Controls["sfxVolume"].Primary.onClick.Invoke();
            panel.Controls["resolution"].Primary.onClick.Invoke();

            var reloaded = new UserSettings(_preferences);
            Assert.That(reloaded.MouseSensitivity, Is.EqualTo(1.25f).Within(1e-4f));
            Assert.That(reloaded.InvertY, Is.True);
            Assert.That(reloaded.TutorialHints, Is.False);
            Assert.That(reloaded.DefaultView, Is.EqualTo(CameraViewMode.FirstPerson));
            Assert.That(reloaded.FlightMode, Is.EqualTo(Moqui.Core.Simulation.FlightControlMode.Free));
            Assert.That(reloaded.SfxVolume, Is.EqualTo(0.9f).Within(1e-4f));
            Assert.That(reloaded.ResolutionIndex, Is.EqualTo(1));
            Assert.That(panel.Labels["sensitivity"].text, Does.Contain("1.25"));

            for (int i = 0; i < 40; i++)
            {
                panel.Controls["sensitivity"].Secondary.onClick.Invoke();
            }

            Assert.That(reloaded.MouseSensitivity, Is.EqualTo(UserSettings.MaxSensitivity), "clamped to 4x");
        }

        [Test]
        public void Settings_SurvivePlayerPrefsRestart()
        {
            string[] keys = { UserSettings.SensitivityKey, UserSettings.InvertYKey };
            var previous = new Dictionary<string, string>();
            foreach (string key in keys)
            {
                previous[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            }

            try
            {
                var settings = new UserSettings(new PlayerPrefsStore()) { MouseSensitivity = 2.5f, InvertY = true };
                var afterRestart = new UserSettings(new PlayerPrefsStore());
                Assert.That(afterRestart.MouseSensitivity, Is.EqualTo(2.5f).Within(1e-4f));
                Assert.That(afterRestart.InvertY, Is.True);
                Assert.That(settings.MouseSensitivity, Is.EqualTo(afterRestart.MouseSensitivity));
            }
            finally
            {
                foreach (var pair in previous)
                {
                    if (pair.Value == null)
                    {
                        PlayerPrefs.DeleteKey(pair.Key);
                    }
                    else
                    {
                        PlayerPrefs.SetString(pair.Key, pair.Value);
                    }
                }

                PlayerPrefs.Save();
            }
        }
    }
}
