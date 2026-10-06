using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Moqui.Core.Data.Levels;
using Moqui.Core.Meta;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Data
{
    /// <summary>
    /// 애셋 스펙 (spec/assets, D-062): 스테이지 목록의 모든 방·레벨 형상이 애셋 인덱스의 어느 애셋에 속하고, 그 문서가 있어야 한다.
    /// 스테이지를 추가할 때 애셋 스펙을 빠뜨리지 않게 한다.
    /// </summary>
    public class AssetIndexTests
    {
        private static string AssetsRoot => Path.Combine(RepoPaths.Data, "..", "spec", "assets");

        private sealed class AssetRow
        {
            public string Id;
            public string Doc;
            public List<string> Patterns;
            public List<string> Rooms;
        }

        private static List<AssetRow> Rows()
        {
            var rows = new List<AssetRow>();
            bool inModels = false;
            foreach (string line in File.ReadLines(Path.Combine(AssetsRoot, "INDEX.md")))
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inModels = line.Contains("모델");
                    continue;
                }

                if (!inModels || !line.StartsWith("| ", StringComparison.Ordinal) || line.StartsWith("| 애셋 ID", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('|').Select(cell => cell.Trim()).ToArray();
                var doc = Regex.Match(cells[5], @"\(([^)]+)\)");
                rows.Add(new AssetRow
                {
                    Id = cells[1],
                    Doc = doc.Success ? doc.Groups[1].Value : null,
                    Patterns = Regex.Matches(cells[6], "`([^`]+)`").Select(match => match.Groups[1].Value).ToList(),
                    Rooms = cells[7] == "-" ? new List<string>() : cells[7].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList(),
                });
            }

            return rows;
        }

        private static bool Matches(string pattern, string id)
        {
            string regex = "^" + Regex.Escape(pattern.ToLowerInvariant()).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(id.ToLowerInvariant(), regex);
        }

        [Test]
        public void EveryIndexedAsset_HasItsDocument()
        {
            var rows = Rows();
            Assert.That(rows.Count, Is.GreaterThan(10));
            foreach (var row in rows)
            {
                Assert.That(row.Doc, Is.Not.Null, row.Id);
                Assert.That(File.Exists(Path.Combine(AssetsRoot, row.Doc)), Is.True, $"{row.Id}: {row.Doc}");
            }

            Assert.That(rows.Select(row => row.Id), Does.Contain("moki").And.Contain("human-adult"));
        }

        [Test]
        public void EveryShapeOfEveryCatalogStage_BelongsToAnAsset()
        {
            var rows = Rows();
            var source = FileSystemDataSource.ForRepoData();
            var loader = new LevelLoader(source);
            var missing = new List<string>();
            foreach (string levelId in StageCatalog.Load(source).LevelIds)
            {
                var level = loader.Load(levelId);
                foreach (var shape in level.AllShapes())
                {
                    bool covered = rows.Any(row => row.Patterns.Any(pattern => Matches(pattern, shape.Id))
                        && (row.Rooms.Count == 0 || row.Rooms.Contains(level.Room.Id) || !level.Room.Shapes.Any(roomShape => roomShape.Id == shape.Id)));
                    if (!covered)
                    {
                        missing.Add($"{levelId} ({level.Room.Id}): {shape.Id}");
                    }
                }
            }

            Assert.That(missing, Is.Empty, "add these to Tools/asset_specs.py and regenerate spec/assets:\n" + string.Join("\n", missing));
        }
    }
}
