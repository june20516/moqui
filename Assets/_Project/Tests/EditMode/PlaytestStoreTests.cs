using System.IO;
using Moqui.Core.Data;
using Moqui.Unity.Data;
using Moqui.Unity.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>플레이 검증 파일·적용·정본 올리기 (D-065).</summary>
    public class PlaytestStoreTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "moqui-playtest-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            PlaytestStore.ActiveOverride = null;
            Directory.Delete(_directory, true);
        }

        [Test]
        public void Store_RoundTripsOverridesPresetsAndLog_BrokenFileIsIgnoredWithError()
        {
            var store = new PlaytestStore(_directory);
            var overrides = new PlaytestOverrides();
            overrides.Set("flight.speed", 72);

            store.Save(overrides);
            store.SavePreset("A", overrides);
            store.AppendLog("{\"x\": 1}");
            store.AppendLog(null);

            Assert.That(store.Load().Values["flight.speed"], Is.EqualTo(72));
            Assert.That(store.HasPreset("A"), Is.True);
            Assert.That(store.HasPreset("B"), Is.False);
            Assert.That(File.ReadAllLines(store.LogPath), Has.Length.EqualTo(1));

            File.WriteAllText(store.OverridesPath, "{ broken");
            Assert.That(store.Load().Count, Is.Zero);
            Assert.That(store.LastError, Is.Not.Null);
        }

        [Test]
        public void Tuning_AppliesOnlyWhenActive()
        {
            var store = new PlaytestStore(_directory);
            var overrides = new PlaytestOverrides();
            overrides.Set("flight.speed", 72);
            store.Save(overrides);
            var source = new UnityDataSource();
            var baseTuning = TuningLoader.Load(source);

            PlaytestStore.ActiveOverride = false;
            Assert.That(PlaytestTuning.Apply(baseTuning, source, store).GetFloat("flight.speed"), Is.EqualTo(baseTuning.GetFloat("flight.speed")));
            Assert.That(PlaytestTuning.Applied, Is.Empty);

            PlaytestStore.ActiveOverride = true;
            Assert.That(PlaytestTuning.Apply(baseTuning, source, store).GetFloat("flight.speed"), Is.EqualTo(72f));
            Assert.That(PlaytestTuning.Applied, Is.EqualTo(new[] { "flight.speed" }));
        }

        [Test]
        public void Promote_RewritesCopiesOfTheCanonicalFiles()
        {
            string repo = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Directory.CreateDirectory(Path.Combine(_directory, "data"));
            Directory.CreateDirectory(Path.Combine(_directory, "spec"));
            File.Copy(Path.Combine(repo, "data", "tuning.json"), Path.Combine(_directory, "data", "tuning.json"));
            File.Copy(Path.Combine(repo, "data", "playtest-keys.json"), Path.Combine(_directory, "data", "playtest-keys.json"));
            File.Copy(Path.Combine(repo, "spec", "tuning.md"), Path.Combine(_directory, "spec", "tuning.md"));
            var overrides = new PlaytestOverrides();
            overrides.Set("camera.fov", 75);

            PlaytestMenu.Promote(overrides, _directory);

            var tuning = TuningLoader.Parse(File.ReadAllText(Path.Combine(_directory, "data", "tuning.json")));
            Assert.That(tuning.GetFloat("camera.fov"), Is.EqualTo(75f));
            Assert.That(File.ReadAllText(Path.Combine(_directory, "spec", "tuning.md")), Does.Contain("| camera.fov | 75"));

            var unknown = new PlaytestOverrides();
            unknown.Set("world.gravity", 1);
            Assert.Throws<DataFormatException>(() => PlaytestMenu.Promote(unknown, _directory));
        }
    }
}
