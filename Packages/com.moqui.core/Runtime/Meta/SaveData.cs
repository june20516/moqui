using System;
using System.Collections.Generic;
using System.Linq;
using Moqui.Core.Data;
using Moqui.Core.Simulation;

namespace Moqui.Core.Meta
{
    /// <summary>스테이지 하나의 클리어 기록 (spec/08 Stage Select 표시, spec/09 §3).</summary>
    public sealed class StageRecord
    {
        public bool Cleared { get; set; }

        /// <summary>최고(가장 짧은) 클리어 시간 초.</summary>
        public float BestSeconds { get; set; }

        /// <summary>광분 0회로 클리어한 적이 있는가.</summary>
        public bool NoFrenzy { get; set; }

        public int MinBiteMarks { get; set; }
    }

    /// <summary>저장 내용 (spec/09 §3): 혈액 포인트, 스킬 레벨, 장착 액티브, 스테이지 기록, 포맷 버전.</summary>
    public sealed class SaveData
    {
        public const int CurrentFormatVersion = 1;

        public int FormatVersion { get; set; } = CurrentFormatVersion;

        public int BloodPoints { get; set; }

        public Dictionary<string, int> SkillLevels { get; } = new Dictionary<string, int>();

        public string EquippedActive { get; set; }

        /// <summary>레벨 ID("stage01") → 기록.</summary>
        public Dictionary<string, StageRecord> Stages { get; } = new Dictionary<string, StageRecord>();

        public int SkillLevel(string skillId)
        {
            return SkillLevels.TryGetValue(skillId, out int level) ? level : 0;
        }

        public SkillLoadout Loadout => new SkillLoadout(SkillLevels, EquippedActive);

        public StageRecord Record(string levelId)
        {
            return Stages.TryGetValue(levelId, out var record) ? record : null;
        }

        /// <summary>목록의 첫 스테이지는 처음부터, 나머지는 목록에서 바로 앞 스테이지를 클리어해야 열린다 (spec/08, D-061).</summary>
        public bool IsUnlocked(string levelId, StageCatalog catalog)
        {
            if (!catalog.Contains(levelId))
            {
                return false;
            }

            string previous = catalog.Previous(levelId);
            return previous == null || (Record(previous)?.Cleared ?? false);
        }

        /// <summary>클리어를 기록하고 보상을 더한다. 최고 시간·최소 자국은 더 좋은 값만, 광분 0회는 한 번이라도 달성하면 유지.</summary>
        public void RecordClear(string levelId, StageResult result, RewardBreakdown reward)
        {
            BloodPoints += reward.Total;
            if (!Stages.TryGetValue(levelId, out var record) || !record.Cleared)
            {
                Stages[levelId] = new StageRecord
                {
                    Cleared = true,
                    BestSeconds = result.ClearSeconds,
                    NoFrenzy = result.FrenzyCount == 0,
                    MinBiteMarks = result.BiteMarkCount,
                };
                return;
            }

            record.BestSeconds = Math.Min(record.BestSeconds, result.ClearSeconds);
            record.NoFrenzy |= result.FrenzyCount == 0;
            record.MinBiteMarks = Math.Min(record.MinBiteMarks, result.BiteMarkCount);
        }
    }

    /// <summary>save.json 형식 (spec/09 §3). 쓰기는 고정 순서, 읽기는 JsonAccess로 엄격하게 검사한다.</summary>
    public static class SaveSerializer
    {
        public static string Serialize(SaveData data)
        {
            var skills = new Dictionary<string, JsonValue>();
            foreach (var pair in data.SkillLevels.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                skills[pair.Key] = JsonValue.FromNumber(pair.Value);
            }

            var stages = new Dictionary<string, JsonValue>();
            foreach (var pair in data.Stages.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                stages[pair.Key] = JsonValue.FromObject(new Dictionary<string, JsonValue>
                {
                    ["cleared"] = Bool(pair.Value.Cleared),
                    ["bestSeconds"] = JsonValue.FromNumber(pair.Value.BestSeconds),
                    ["noFrenzy"] = Bool(pair.Value.NoFrenzy),
                    ["minBiteMarks"] = JsonValue.FromNumber(pair.Value.MinBiteMarks),
                });
            }

            var root = new Dictionary<string, JsonValue>
            {
                ["formatVersion"] = JsonValue.FromNumber(data.FormatVersion),
                ["bloodPoints"] = JsonValue.FromNumber(data.BloodPoints),
                ["skillLevels"] = JsonValue.FromObject(skills),
                ["equippedActive"] = data.EquippedActive != null ? JsonValue.FromString(data.EquippedActive) : JsonValue.Null,
                ["stages"] = JsonValue.FromObject(stages),
            };
            return JsonWriter.Write(JsonValue.FromObject(root));
        }

        private static JsonValue Bool(bool value)
        {
            return value ? JsonValue.True : JsonValue.False;
        }

        /// <exception cref="DataFormatException">형식이 맞지 않을 때.</exception>
        public static SaveData Deserialize(string text)
        {
            var root = JsonAccess.Parse(text, "save.json");
            int version = root.Get("formatVersion").Int();
            if (version != SaveData.CurrentFormatVersion)
            {
                throw root.Get("formatVersion").Error($"must be {SaveData.CurrentFormatVersion}");
            }

            var data = new SaveData { FormatVersion = version, BloodPoints = root.Get("bloodPoints").Int() };
            var skills = root.Get("skillLevels");
            foreach (var pair in Members(skills))
            {
                if (!SkillCatalog.Exists(pair.Key))
                {
                    throw pair.Value.Error("unknown skill");
                }

                data.SkillLevels[pair.Key] = Math.Clamp(pair.Value.Int(), 0, SkillCatalog.Get(pair.Key).MaxLevel);
            }

            var equipped = root.Get("equippedActive");
            data.EquippedActive = equipped.Value.Kind == JsonKind.Null ? null : equipped.String();
            foreach (var pair in Members(root.Get("stages")))
            {
                data.Stages[pair.Key] = new StageRecord
                {
                    Cleared = pair.Value.Get("cleared").Bool(),
                    BestSeconds = pair.Value.Get("bestSeconds").Float(),
                    NoFrenzy = pair.Value.Get("noFrenzy").Bool(),
                    MinBiteMarks = pair.Value.Get("minBiteMarks").Int(),
                };
            }

            return data;
        }

        private static IEnumerable<KeyValuePair<string, JsonAccess>> Members(JsonAccess access)
        {
            if (access.Value.Kind != JsonKind.Object)
            {
                throw access.Error("must be an object");
            }

            foreach (var pair in access.Value.Members)
            {
                yield return new KeyValuePair<string, JsonAccess>(pair.Key, new JsonAccess(pair.Value, $"{access.Path}.{pair.Key}"));
            }
        }
    }
}
