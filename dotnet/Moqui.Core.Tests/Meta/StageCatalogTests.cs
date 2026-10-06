using System.IO;
using System.Linq;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Meta;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Meta
{
    /// <summary>스테이지 목록 (data/stages.json, spec/07·08, D-061): 레벨 추가는 데이터만으로.</summary>
    public class StageCatalogTests
    {
        private static StageCatalog Repo => StageCatalog.Load(FileSystemDataSource.ForRepoData());

        [Test]
        public void RepoCatalog_EveryListedLevelLoadsAndOrderDrivesNumbering()
        {
            var catalog = Repo;
            var loader = new LevelLoader(FileSystemDataSource.ForRepoData());
            foreach (var stage in catalog.Stages)
            {
                Assert.That(loader.Load(stage.LevelId).Id, Is.EqualTo(stage.LevelId));
                Assert.That(stage.Title, Is.Not.Empty, stage.LevelId);
                Assert.That(catalog.AmbienceOf(stage.LevelId), Is.Not.Null.And.Not.Empty, stage.LevelId);
            }

            string first = catalog.Stages[0].LevelId;
            string last = catalog.Stages[catalog.Stages.Count - 1].LevelId;
            Assert.That(catalog.Number(first), Is.EqualTo(1));
            Assert.That(catalog.Previous(first), Is.Null);
            Assert.That(catalog.IsLast(last), Is.True);
            Assert.That(catalog.Next(last), Is.Null);
            Assert.That(catalog.Next(first), Is.EqualTo(catalog.Stages[1].LevelId));
            Assert.That(catalog.Chapters.Count, Is.EqualTo(4), "four chapters (D-058)");
        }

        [Test]
        public void EveryLevelFile_IsListed()
        {
            var listed = Repo.LevelIds.ToHashSet();
            foreach (string file in Directory.GetFiles(Path.Combine(RepoPaths.Data, "levels"), "*.json"))
            {
                Assert.That(listed, Does.Contain(Path.GetFileNameWithoutExtension(file)), "a level file that is not in data/stages.json is never played");
            }
        }

        [Test]
        public void Ambience_StageOverridesChapter()
        {
            const string text = @"{ ""formatVersion"": 1, ""chapters"": [ { ""id"": ""a"", ""title"": ""A"", ""ambience"": ""amb_a"",
                ""stages"": [ { ""level"": ""x"", ""title"": ""X"" }, { ""level"": ""y"", ""title"": ""Y"", ""ambience"": ""amb_y"" } ] } ] }";
            var catalog = StageCatalog.Parse(text, "test");
            Assert.That(catalog.AmbienceOf("x"), Is.EqualTo("amb_a"));
            Assert.That(catalog.AmbienceOf("y"), Is.EqualTo("amb_y"));
            Assert.That(catalog.Number("y"), Is.EqualTo(2));
            Assert.That(catalog.Number("missing"), Is.EqualTo(0));
        }

        /// <summary>레벨별 기준 시간 키가 없으면 기본값을 쓴다 (새 레벨은 tuning 키를 추가하지 않아도 된다).</summary>
        [Test]
        public void ParTime_FallsBackToDefault()
        {
            var tuning = TestSimulations.Tuning;
            var calculator = new RewardCalculator(tuning);
            Assert.That(calculator.ParTime("stage02"), Is.EqualTo(tuning.GetFloat("meta.parTime.stage02")));
            Assert.That(calculator.ParTime("aBrandNewLevel"), Is.EqualTo(tuning.GetFloat("meta.parTime.default")));
        }

        [Test]
        public void DuplicateOrEmpty_Rejected()
        {
            const string duplicate = @"{ ""formatVersion"": 1, ""chapters"": [ { ""id"": ""a"", ""title"": ""A"", ""stages"": [ { ""level"": ""x"", ""title"": ""X"" } ] },
                { ""id"": ""b"", ""title"": ""B"", ""stages"": [ { ""level"": ""x"", ""title"": ""X again"" } ] } ] }";
            const string empty = @"{ ""formatVersion"": 1, ""chapters"": [ { ""id"": ""a"", ""title"": ""A"", ""stages"": [] } ] }";
            Assert.Throws<DataFormatException>(() => StageCatalog.Parse(duplicate, "test"));
            Assert.Throws<DataFormatException>(() => StageCatalog.Parse(empty, "test"));
        }
    }
}
