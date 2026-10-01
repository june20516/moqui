using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Moqui.Unity.Tests
{
    /// <summary>에셋 크레딧 (tech/asset-pipeline.md 수용 기준).</summary>
    public class CreditsTests
    {
        private const string CreditsPath = "Assets/_Project/CREDITS.md";
        private const string ExternalSectionHeader = "## 외부 에셋";
        private const int ColumnCount = 7;
        private static readonly string[] AssetFolders = { "Assets/_Project/Art", "Assets/_Project/Audio" };
        private static readonly string[] AllowedLicenses = { "CC0", "CC-BY 3.0", "CC-BY 4.0", "MIT", "Apache-2.0", "Unity Companion License" };
        private static readonly Regex DatePattern = new Regex(@"^\d{4}-\d{2}-\d{2}$");

        [Test]
        public void EveryCreditRow_HasAllowedLicenseUrlAndDate()
        {
            foreach (var row in ExternalRows())
            {
                string label = string.Join(" | ", row);
                Assert.That(row.Length, Is.EqualTo(ColumnCount), label);
                Assert.That(AllowedLicenses, Does.Contain(row[4]), label);
                Assert.That(row[3], Does.StartWith("https://").Or.StartWith("http://"), label);
                Assert.That(DatePattern.IsMatch(row[5]), Is.True, label);
            }
        }

        [Test]
        public void EveryExternalFile_IsCredited()
        {
            var credited = new HashSet<string>(ExternalRows().Select(row => row[0].Trim('`')));
            var external = AssetFolders
                .Where(Directory.Exists)
                .SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                .Select(path => path.Replace('\\', '/'))
                .Where(path => !path.EndsWith(".meta") && !path.Contains("/Generated/"));
            foreach (string path in external)
            {
                Assert.That(credited, Does.Contain(path), $"{path} is not in CREDITS.md (or move self-made files under Generated/)");
            }
        }

        /// <summary>"외부 에셋" 절의 표 데이터 행 (머리글·구분선 제외).</summary>
        private static IEnumerable<string[]> ExternalRows()
        {
            string[] lines = File.ReadAllLines(CreditsPath);
            int start = System.Array.IndexOf(lines, ExternalSectionHeader);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "external asset section");
            var table = lines.Skip(start + 1)
                .TakeWhile(line => !line.StartsWith("## "))
                .Where(line => line.StartsWith("|"))
                .Skip(2);
            return table.Select(line => line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray()).ToList();
        }
    }
}
