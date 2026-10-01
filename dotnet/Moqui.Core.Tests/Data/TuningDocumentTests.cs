using System.IO;
using System.Linq;
using Moqui.Core.Data;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Data
{
    /// <summary>spec/tuning.md(정본)와 data/tuning.json이 일치하는지 검사한다 (tech/verification.md §1).</summary>
    public class TuningDocumentTests
    {
        private static string SpecText => File.ReadAllText(Path.Combine(RepoPaths.Spec, "tuning.md"));

        [Test]
        public void Load_RepoTuningJson_Succeeds()
        {
            var tuning = TuningLoader.Load(FileSystemDataSource.ForRepoData());

            Assert.That(tuning.Count, Is.GreaterThan(0));
        }

        [Test]
        public void TuningJson_EverySpecKey_HasMatchingValue()
        {
            var tuning = TuningLoader.Load(FileSystemDataSource.ForRepoData());
            var expected = TuningSpecParser.Parse(SpecText);

            var mismatches = expected
                .Where(pair => !tuning.Contains(pair.Key) || !tuning.GetRaw(pair.Key).StructurallyEquals(pair.Value))
                .Select(pair => $"{pair.Key}: spec={pair.Value}, json={(tuning.Contains(pair.Key) ? tuning.GetRaw(pair.Key).ToString() : "(missing)")}")
                .ToList();

            Assert.That(mismatches, Is.Empty);
        }

        [Test]
        public void TuningJson_NoKeysOutsideSpec()
        {
            var tuning = TuningLoader.Load(FileSystemDataSource.ForRepoData());
            var specKeys = TuningSpecParser.Parse(SpecText).Select(pair => pair.Key).ToHashSet();

            var extra = tuning.Keys.Where(key => !specKeys.Contains(key)).ToList();

            Assert.That(extra, Is.Empty);
        }

        [Test]
        public void SpecDocument_Keys_AreUnique()
        {
            var keys = TuningSpecParser.Parse(SpecText).Select(pair => pair.Key).ToList();

            var duplicates = keys.GroupBy(key => key).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

            Assert.That(duplicates, Is.Empty);
        }

        [TestCase("1.0~1.5s", "{\"min\": 1, \"max\": 1.5}")]
        [TestCase("60 / 120 / 200", "[60, 120, 200]")]
        [TestCase("20s / 14s", "[20, 14]")]
        [TestCase("(0, 0.15, 0.1)u", "[0, 0.15, 0.1]")]
        [TestCase("±85°", "85")]
        [TestCase("−0.05", "-0.05")]
        [TestCase("+40u", "40")]
        [TestCase("20 /s", "20")]
        [TestCase("2 %/s", "2")]
        [TestCase("1u = 1cm", "1")]
        [TestCase("ThirdPerson", "\"ThirdPerson\"")]
        public void SpecParser_ValueNotation_ParsesToExpectedJson(string cell, string expectedJson)
        {
            var parsed = TuningSpecParser.ParseValue("test.key", cell);

            Assert.That(parsed.StructurallyEquals(JsonReader.Parse(expectedJson)), Is.True, parsed.ToString());
        }
    }
}
