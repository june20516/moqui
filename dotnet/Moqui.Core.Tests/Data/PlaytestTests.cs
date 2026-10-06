using System;
using System.IO;
using System.Linq;
using Moqui.Core.Data;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Data
{
    /// <summary>플레이 검증 변수 구조 (plan/gulf-improvements.md §11, D-065).</summary>
    public class PlaytestTests
    {
        private static Tuning RepoTuning => TuningLoader.Load(FileSystemDataSource.ForRepoData());

        private static PlaytestKeyCatalog Catalog => PlaytestKeyCatalog.Load(FileSystemDataSource.ForRepoData(), RepoTuning);

        [Test]
        public void Catalog_EveryKeyIsANumberTuningKey_WithTheCurrentValueInRange()
        {
            // Load가 검사한다(키 존재·숫자·범위). 그룹마다 키가 있어야 패널 탭이 비지 않는다.
            Assert.That(Catalog.Groups, Is.Not.Empty);
            Assert.That(Catalog.Groups.All(group => group.Keys.Count > 0), Is.True);
        }

        [Test]
        public void Catalog_EveryKeyIsMarkedInTheSpec()
        {
            string spec = File.ReadAllText(Path.Combine(RepoPaths.Spec, "tuning.md"));
            foreach (var key in Catalog.AllKeys)
            {
                Assert.That(spec, Does.Contain($"`{key.Key}`"), "spec/tuning.md '체감 검증 대상' lists every playtest key");
            }
        }

        [Test]
        public void Catalog_RejectsUnknownKey_AndValueOutsideRange()
        {
            const string Unknown = "{\"formatVersion\": 1, \"groups\": [{\"id\": \"a\", \"title\": \"a\", \"keys\": [{\"key\": \"nope.key\", \"min\": 0, \"max\": 1}]}]}";
            const string OutOfRange = "{\"formatVersion\": 1, \"groups\": [{\"id\": \"a\", \"title\": \"a\", \"keys\": [{\"key\": \"flight.speed\", \"min\": 0, \"max\": 1}]}]}";

            Assert.Throws<DataFormatException>(() => PlaytestKeyCatalog.Parse(Unknown, RepoTuning));
            Assert.Throws<DataFormatException>(() => PlaytestKeyCatalog.Parse(OutOfRange, RepoTuning));
        }

        [Test]
        public void Overrides_ApplyOnlyPlaytestKeys_AndLeaveTheBaseUntouched()
        {
            var tuning = RepoTuning;
            float baseSpeed = tuning.GetFloat("flight.speed");
            var overrides = PlaytestOverrides.Parse("{\"formatVersion\": 1, \"values\": {\"flight.speed\": 75, \"world.gravity\": 1}}");

            var applied = overrides.Apply(tuning, Catalog, out var appliedKeys, out var rejected);

            Assert.That(applied.GetFloat("flight.speed"), Is.EqualTo(75f));
            Assert.That(applied.GetFloat("world.gravity"), Is.EqualTo(tuning.GetFloat("world.gravity")), "not a playtest key");
            Assert.That(appliedKeys, Is.EqualTo(new[] { "flight.speed" }));
            Assert.That(rejected.Single(), Does.StartWith("world.gravity"));
            Assert.That(tuning.GetFloat("flight.speed"), Is.EqualTo(baseSpeed));
            Assert.That(applied.Count, Is.EqualTo(tuning.Count));
        }

        [Test]
        public void Overrides_RoundTrip_SortedAndEmptyIsValid()
        {
            var overrides = new PlaytestOverrides();
            overrides.Set("suck.rateMax", 7.5);
            overrides.Set("flight.speed", 70);

            string json = overrides.ToJson();
            var back = PlaytestOverrides.Parse(json);

            Assert.That(json.IndexOf("flight.speed", StringComparison.Ordinal), Is.LessThan(json.IndexOf("suck.rateMax", StringComparison.Ordinal)));
            Assert.That(back.Values, Is.EqualTo(overrides.Values));
            Assert.That(PlaytestOverrides.Parse(new PlaytestOverrides().ToJson()).Count, Is.Zero);
            Assert.That(PlaytestOverrides.Parse("").Count, Is.Zero);
        }

        [Test]
        public void Log_RecordsOnlyChanges_FromBaseWhenNotOverriddenBefore()
        {
            var tuning = RepoTuning;
            var before = new PlaytestOverrides();
            var after = new PlaytestOverrides();
            after.Set("flight.speed", 70);
            after.Set("camera.fov", tuning.GetFloat("camera.fov"));

            string line = PlaytestLog.Line(new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc), "stage01", before, after, tuning);

            Assert.That(line, Is.EqualTo("{\"time\": \"2026-10-06T12:00:00Z\", \"level\": \"stage01\", \"changes\": {\"flight.speed\": [60, 70]}}"));
            Assert.That(PlaytestLog.Line(DateTime.UtcNow, "stage01", after, after.Clone(), tuning), Is.Null);
        }

        [Test]
        public void Promotion_RewritesJsonLineAndSpecValueCell_KeepingUnits()
        {
            const string Json = "{\n  \"values\": {\n    \"flight.speed\": 60,\n    \"flight.speedX\": 1\n  }\n}\n";
            const string Spec = "| 키 | 값 | 설명 |\n|---|---|---|\n| flight.speed | 60 u/s | 최고 속도 |\n| flight.speedX | 1 | x |\n";

            string json = TuningPromotion.ReplaceInTuningJson(Json, "flight.speed", 72.5);
            string spec = TuningPromotion.ReplaceInSpec(Spec, "flight.speed", 72.5);

            Assert.That(json, Does.Contain("\"flight.speed\": 72.5,"));
            Assert.That(json, Does.Contain("\"flight.speedX\": 1"));
            Assert.That(spec, Does.Contain("| flight.speed | 72.5 u/s | 최고 속도 |"));
            Assert.That(spec, Does.Contain("| flight.speedX | 1 | x |"));
            Assert.Throws<DataFormatException>(() => TuningPromotion.ReplaceInSpec(Spec, "missing.key", 1));
        }

        /// <summary>값 칸의 부호를 지킨다: 문서의 '−' 표기, 양수↔음수 전환 (리뷰 M14).</summary>
        [Test]
        public void Promotion_HandlesSignedSpecValues()
        {
            const string Spec = "| a.b | −0.05 | x |\n| c.d | -5u | y |\n";

            string spec = TuningPromotion.ReplaceInSpec(Spec, "a.b", -0.1);
            spec = TuningPromotion.ReplaceInSpec(spec, "c.d", 3);

            Assert.That(spec, Does.Contain("| a.b | −0.1 | x |"));
            Assert.That(spec, Does.Contain("| c.d | 3u | y |"));
            Assert.That(TuningPromotion.ReplaceInSpec(spec, "c.d", -2), Does.Contain("| c.d | -2u | y |"));
            Assert.That(TuningPromotion.ReplaceInTuningJson("{ \"c.d\": -5 }", "c.d", 3), Does.Contain("\"c.d\": 3"));
        }

        /// <summary>목록 밖 키는 패널 세션에 들어오지 않고, 기록 JSON은 이스케이프된다 (리뷰 M14).</summary>
        [Test]
        public void Session_IgnoresUnknownSavedKeys_LogEscapesStrings()
        {
            var saved = new PlaytestOverrides();
            saved.Set("world.gravity", 1);
            saved.Set("not.a.key", 2);
            saved.Set("flight.speed", 70);

            var session = new PlaytestSession(RepoTuning, Catalog, saved);

            Assert.That(session.Working.Values.Keys, Is.EqualTo(new[] { "flight.speed" }));
            Assert.That(session.HasUnsavedChanges, Is.False);
            Assert.That(PlaytestOverrides.Quote("a\"b\\c"), Is.EqualTo("\"a\\\"b\\\\c\""));
            var after = new PlaytestOverrides();
            after.Set("flight.speed", 70);
            string line = PlaytestLog.Line(System.DateTime.UtcNow, "odd\"level", new PlaytestOverrides(), after, RepoTuning);
            Assert.That(line, Does.Contain("\"level\": \"odd\\\"level\""));
        }

        [Test]
        public void Promotion_OfEveryPlaytestKey_KeepsJsonAndSpecInAgreement()
        {
            // 모든 체감 키를 지금 값으로 다시 써도 문서·json 표기가 깨지지 않는다(정본 올리기의 회귀 방지).
            string json = File.ReadAllText(Path.Combine(RepoPaths.Data, "tuning.json"));
            string spec = File.ReadAllText(Path.Combine(RepoPaths.Spec, "tuning.md"));
            var tuning = RepoTuning;
            foreach (var key in Catalog.AllKeys)
            {
                json = TuningPromotion.ReplaceInTuningJson(json, key.Key, tuning.GetFloat(key.Key));
                spec = TuningPromotion.ReplaceInSpec(spec, key.Key, tuning.GetFloat(key.Key));
            }

            var rewritten = TuningLoader.Parse(json);
            var expected = TuningSpecParser.Parse(spec).ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (var key in Catalog.AllKeys)
            {
                Assert.That(rewritten.GetFloat(key.Key), Is.EqualTo(tuning.GetFloat(key.Key)).Within(1e-4f), key.Key);
                Assert.That(expected[key.Key].NumberValue, Is.EqualTo(tuning.GetFloat(key.Key)).Within(1e-4), key.Key);
            }
        }

        [Test]
        public void Session_StepsWithinRange_BackToBaseDropsTheOverride_CommitLogsOnce()
        {
            var tuning = RepoTuning;
            var session = new PlaytestSession(tuning, Catalog, new PlaytestOverrides());
            Catalog.TryGet("flight.speed", out var speed);

            session.Step(speed, +1);
            Assert.That(session.Value("flight.speed"), Is.EqualTo(tuning.GetFloat("flight.speed") + PlaytestSession.StepOf(speed)).Within(1e-6));
            Assert.That(session.IsOverridden("flight.speed"), Is.True);
            Assert.That(session.HasUnsavedChanges, Is.True);

            session.Step(speed, -1);
            Assert.That(session.IsOverridden("flight.speed"), Is.False, "back at the base value");

            for (int i = 0; i < PlaytestSession.StepsPerRange * 2; i++)
            {
                session.Step(speed, +1);
            }

            Assert.That(session.Value("flight.speed"), Is.EqualTo(speed.Max).Within(1e-6), "clamped to the slider range");
            string line = session.Commit(DateTime.UtcNow, "stage01");
            Assert.That(line, Does.Contain("\"flight.speed\""));
            Assert.That(session.HasUnsavedChanges, Is.False);
            Assert.That(session.Commit(DateTime.UtcNow, "stage01"), Is.Null, "nothing new to log");
        }

        [Test]
        public void Session_LoadWorking_DropsUnknownKeys_ResetAllClears()
        {
            var session = new PlaytestSession(RepoTuning, Catalog, new PlaytestOverrides());
            var preset = new PlaytestOverrides();
            preset.Set("camera.fov", 85);
            preset.Set("world.gravity", 1);

            session.LoadWorking(preset);

            Assert.That(session.Working.Values.Keys, Is.EqualTo(new[] { "camera.fov" }));
            session.ResetAll();
            Assert.That(session.Working.Count, Is.Zero);
        }
    }
}
