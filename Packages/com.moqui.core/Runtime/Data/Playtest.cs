using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Moqui.Core.Data
{
    /// <summary>체감 검증 대상 키 하나: 조정 패널 슬라이더 범위.</summary>
    public sealed class PlaytestKey
    {
        public PlaytestKey(string key, float min, float max)
        {
            Key = key;
            Min = min;
            Max = max;
        }

        public string Key { get; }

        public float Min { get; }

        public float Max { get; }
    }

    /// <summary>체감 검증 대상 키 묶음 (패널의 탭).</summary>
    public sealed class PlaytestGroup
    {
        public PlaytestGroup(string id, string title, IReadOnlyList<PlaytestKey> keys)
        {
            Id = id;
            Title = title;
            Keys = keys;
        }

        public string Id { get; }

        public string Title { get; }

        public IReadOnlyList<PlaytestKey> Keys { get; }
    }

    /// <summary>
    /// 사람이 플레이하며 체감으로 맞출 수치 목록 (data/playtest-keys.json, plan/gulf-improvements.md §11, D-065).
    /// 조정 패널에는 이 키만 나오고, 덮어쓰기 파일도 이 키만 받는다.
    /// </summary>
    public sealed class PlaytestKeyCatalog
    {
        public const string FilePath = "playtest-keys.json";
        public const int SupportedFormatVersion = 1;

        private readonly Dictionary<string, PlaytestKey> _byKey;

        public PlaytestKeyCatalog(IReadOnlyList<PlaytestGroup> groups)
        {
            Groups = groups;
            _byKey = new Dictionary<string, PlaytestKey>();
            foreach (var key in groups.SelectMany(group => group.Keys))
            {
                if (_byKey.ContainsKey(key.Key))
                {
                    throw new DataFormatException($"{FilePath}: key '{key.Key}' appears twice.");
                }

                _byKey.Add(key.Key, key);
            }
        }

        public IReadOnlyList<PlaytestGroup> Groups { get; }

        public IEnumerable<PlaytestKey> AllKeys => Groups.SelectMany(group => group.Keys);

        public bool TryGet(string key, out PlaytestKey playtestKey)
        {
            return _byKey.TryGetValue(key, out playtestKey);
        }

        public static PlaytestKeyCatalog Load(IDataSource source, Tuning tuning)
        {
            return Parse(source.ReadText(FilePath), tuning);
        }

        /// <summary>목록을 읽고, 모든 키가 tuning의 숫자 키이며 지금 값이 슬라이더 범위 안인지 검사한다.</summary>
        public static PlaytestKeyCatalog Parse(string text, Tuning tuning)
        {
            var root = JsonAccess.Parse(text, FilePath);
            if (root.Get("formatVersion").Int() != SupportedFormatVersion)
            {
                throw root.Get("formatVersion").Error($"must be {SupportedFormatVersion}");
            }

            var groups = new List<PlaytestGroup>();
            foreach (var groupJson in root.Get("groups").Items())
            {
                var keys = new List<PlaytestKey>();
                foreach (var keyJson in groupJson.Get("keys").Items())
                {
                    string key = keyJson.Get("key").String();
                    float min = keyJson.Get("min").Float();
                    float max = keyJson.Get("max").Float();
                    if (!tuning.Contains(key) || tuning.GetRaw(key).Kind != JsonKind.Number)
                    {
                        throw keyJson.Get("key").Error($"'{key}' is not a number key of tuning.json");
                    }

                    float value = tuning.GetFloat(key);
                    if (!(min < max) || value < min || value > max)
                    {
                        throw keyJson.Error($"range [{min}, {max}] must be increasing and contain the current value {value}");
                    }

                    keys.Add(new PlaytestKey(key, min, max));
                }

                groups.Add(new PlaytestGroup(groupJson.Get("id").String(), groupJson.Get("title").String(), keys));
            }

            return new PlaytestKeyCatalog(groups);
        }
    }

    /// <summary>
    /// 플레이 검증 덮어쓰기 (playtest.json): 체감 검증 키의 값만 tuning.json 위에 덮어쓴다.
    /// 정본(tuning.json·spec/tuning.md)은 바꾸지 않으며, 확정은 <see cref="TuningPromotion"/>으로 따로 한다.
    /// </summary>
    public sealed class PlaytestOverrides
    {
        public const string FileName = "playtest.json";
        public const int SupportedFormatVersion = 1;

        private readonly SortedDictionary<string, double> _values = new SortedDictionary<string, double>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, double> Values => _values;

        public int Count => _values.Count;

        public void Set(string key, double value)
        {
            _values[key] = value;
        }

        public bool Remove(string key)
        {
            return _values.Remove(key);
        }

        public void Clear()
        {
            _values.Clear();
        }

        public PlaytestOverrides Clone()
        {
            var copy = new PlaytestOverrides();
            foreach (var pair in _values)
            {
                copy.Set(pair.Key, pair.Value);
            }

            return copy;
        }

        /// <summary>읽기: {"formatVersion": 1, "values": {"flight.speed": 70}}. 빈 문자열이면 덮어쓰기 없음.</summary>
        public static PlaytestOverrides Parse(string text)
        {
            var overrides = new PlaytestOverrides();
            if (string.IsNullOrWhiteSpace(text))
            {
                return overrides;
            }

            var root = JsonAccess.Parse(text, FileName);
            if (root.Get("formatVersion").Int() != SupportedFormatVersion)
            {
                throw root.Get("formatVersion").Error($"must be {SupportedFormatVersion}");
            }

            foreach (var pair in root.Get("values").Value.Members)
            {
                if (pair.Value.Kind != JsonKind.Number)
                {
                    throw new DataFormatException($"{FileName}.values.{pair.Key}: must be a number");
                }

                overrides.Set(pair.Key, pair.Value.NumberValue);
            }

            return overrides;
        }

        /// <summary>쓰기: 키 순서 고정(비교·기록이 쉽게).</summary>
        public string ToJson()
        {
            var builder = new StringBuilder();
            builder.Append("{\n  \"formatVersion\": ").Append(SupportedFormatVersion).Append(",\n  \"values\": {");
            bool first = true;
            foreach (var pair in _values)
            {
                builder.Append(first ? "\n" : ",\n");
                builder.Append("    \"").Append(pair.Key).Append("\": ").Append(Format(pair.Value));
                first = false;
            }

            builder.Append(first ? "}\n}\n" : "\n  }\n}\n");
            return builder.ToString();
        }

        /// <summary>
        /// 덮어쓴 Tuning을 만든다. 목록에 없는 키, 숫자가 아닌 키는 덮어쓰지 않고 <paramref name="rejected"/>에 이유를 담는다.
        /// 범위 밖 값도 받는다(사람이 일부러 넘겨 볼 수 있다). 다만 0 이하가 될 수 없는 키를 지키는 일은 각 설정의 검사에 맡긴다.
        /// </summary>
        public Tuning Apply(Tuning tuning, PlaytestKeyCatalog catalog, out IReadOnlyList<string> applied, out IReadOnlyList<string> rejected)
        {
            var values = new Dictionary<string, JsonValue>();
            foreach (string key in tuning.Keys)
            {
                values[key] = tuning.GetRaw(key);
            }

            var appliedKeys = new List<string>();
            var rejectedKeys = new List<string>();
            foreach (var pair in _values)
            {
                if (!catalog.TryGet(pair.Key, out _))
                {
                    rejectedKeys.Add($"{pair.Key}: not a playtest key ({PlaytestKeyCatalog.FilePath})");
                    continue;
                }

                values[pair.Key] = JsonValue.FromNumber(pair.Value);
                appliedKeys.Add(pair.Key);
            }

            applied = appliedKeys;
            rejected = rejectedKeys;
            return new Tuning(values);
        }

        public static string Format(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>플레이 검증 기록 한 줄 (playtest-log.jsonl): 언제, 어느 스테이지에서, 무엇을 무엇으로 바꿨나.</summary>
    public static class PlaytestLog
    {
        public const string FileName = "playtest-log.jsonl";

        /// <summary>바뀐 키만 적는다. 바뀐 것이 없으면 null.</summary>
        public static string Line(DateTime utc, string levelId, PlaytestOverrides before, PlaytestOverrides after, Tuning baseTuning)
        {
            var changes = new List<string>();
            foreach (string key in before.Values.Keys.Union(after.Values.Keys).OrderBy(key => key, StringComparer.Ordinal))
            {
                double from = before.Values.TryGetValue(key, out var b) ? b : baseTuning.GetFloat(key);
                double to = after.Values.TryGetValue(key, out var a) ? a : baseTuning.GetFloat(key);
                if (Math.Abs(from - to) > 1e-9)
                {
                    changes.Add($"\"{key}\": [{PlaytestOverrides.Format(from)}, {PlaytestOverrides.Format(to)}]");
                }
            }

            if (changes.Count == 0)
            {
                return null;
            }

            string time = utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            return $"{{\"time\": \"{time}\", \"level\": \"{levelId}\", \"changes\": {{{string.Join(", ", changes)}}}}}";
        }
    }

    /// <summary>
    /// 확정("정본으로 올리기"): 덮어쓴 값을 data/tuning.json과 spec/tuning.md에 옮긴다. 텍스트를 줄 단위로 고쳐 나머지 서식·주석을 지킨다.
    /// 옮긴 뒤에는 Core 테스트(문서·json 일치)와 봇으로 회귀를 확인한다.
    /// </summary>
    public static class TuningPromotion
    {
        private static readonly Regex FirstNumber = new Regex(@"\d+(?:\.\d+)?");

        /// <summary>tuning.json의 `"key": 숫자` 한 줄을 바꾼다. 키가 없거나 숫자 줄이 아니면 예외.</summary>
        public static string ReplaceInTuningJson(string json, string key, double value)
        {
            var pattern = new Regex($"(\"{Regex.Escape(key)}\"\\s*:\\s*)-?\\d+(?:\\.\\d+)?");
            if (pattern.Matches(json).Count != 1)
            {
                throw new DataFormatException($"tuning.json: numeric line for '{key}' not found exactly once");
            }

            return pattern.Replace(json, match => match.Groups[1].Value + PlaytestOverrides.Format(value), 1);
        }

        /// <summary>spec/tuning.md 표에서 `| key | 값 | ...` 행의 값 칸 첫 숫자를 바꾼다(단위·기호는 그대로).</summary>
        public static string ReplaceInSpec(string markdown, string key, double value)
        {
            string newline = markdown.Contains("\r\n") ? "\r\n" : "\n";
            string[] lines = markdown.Replace("\r\n", "\n").Split('\n');
            int found = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string[] cells = lines[i].Split('|');
                if (cells.Length < 4 || cells[1].Trim() != key)
                {
                    continue;
                }

                if (!FirstNumber.IsMatch(cells[2]))
                {
                    throw new DataFormatException($"spec/tuning.md: value cell of '{key}' has no number");
                }

                cells[2] = FirstNumber.Replace(cells[2], PlaytestOverrides.Format(value), 1);
                lines[i] = string.Join("|", cells);
                found++;
            }

            if (found != 1)
            {
                throw new DataFormatException($"spec/tuning.md: row for '{key}' found {found} times");
            }

            return string.Join(newline, lines);
        }
    }

    /// <summary>
    /// 조정 패널의 편집 상태 (UI와 분리해 테스트한다): 정본 값 위에 작업 중인 덮어쓰기를 들고, 한 칸씩 올리고 내리고 되돌린다.
    /// 한 칸 = 슬라이더 범위의 1/<see cref="StepsPerRange"/>. 범위 밖으로는 넘기지 않는다(넘겨 보려면 playtest.json을 직접 고친다).
    /// </summary>
    public sealed class PlaytestSession
    {
        public const int StepsPerRange = 20;

        private readonly Tuning _baseTuning;

        public PlaytestSession(Tuning baseTuning, PlaytestKeyCatalog catalog, PlaytestOverrides saved)
        {
            _baseTuning = baseTuning;
            Catalog = catalog;
            Saved = saved.Clone();
            Working = saved.Clone();
        }

        public PlaytestKeyCatalog Catalog { get; }

        /// <summary>마지막으로 저장된 덮어쓰기 (지금 플레이 중인 값).</summary>
        public PlaytestOverrides Saved { get; private set; }

        /// <summary>패널에서 고치는 중인 덮어쓰기.</summary>
        public PlaytestOverrides Working { get; private set; }

        public bool HasUnsavedChanges => !SameValues(Saved, Working);

        public double BaseValue(string key) => _baseTuning.GetFloat(key);

        public double Value(string key) => Working.Values.TryGetValue(key, out var value) ? value : BaseValue(key);

        public bool IsOverridden(string key) => Working.Values.ContainsKey(key);

        public static double StepOf(PlaytestKey key) => (key.Max - key.Min) / (double)StepsPerRange;

        /// <summary>한 칸 올리거나(+1) 내린다(−1). 정본 값으로 돌아오면 덮어쓰기에서 뺀다.</summary>
        public void Step(PlaytestKey key, int direction)
        {
            double next = Value(key.Key) + (Math.Sign(direction) * StepOf(key));
            next = Math.Round(Math.Min(key.Max, Math.Max(key.Min, next)), 4);
            Set(key.Key, next);
        }

        public void Set(string key, double value)
        {
            if (Math.Abs(value - BaseValue(key)) < 1e-6)
            {
                Working.Remove(key);
            }
            else
            {
                Working.Set(key, value);
            }
        }

        public void Reset(string key) => Working.Remove(key);

        public void ResetAll() => Working.Clear();

        /// <summary>프리셋 등 다른 덮어쓰기를 작업 상태로 불러온다(목록에 없는 키는 버린다).</summary>
        public void LoadWorking(PlaytestOverrides overrides)
        {
            Working = new PlaytestOverrides();
            foreach (var pair in overrides.Values)
            {
                if (Catalog.TryGet(pair.Key, out _))
                {
                    Set(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>작업 상태를 확정한다. 기록 한 줄(바뀐 것이 없으면 null)을 돌려준다.</summary>
        public string Commit(DateTime utc, string levelId)
        {
            string line = PlaytestLog.Line(utc, levelId, Saved, Working, _baseTuning);
            Saved = Working.Clone();
            return line;
        }

        private static bool SameValues(PlaytestOverrides a, PlaytestOverrides b)
        {
            return a.Count == b.Count && a.Values.All(pair => b.Values.TryGetValue(pair.Key, out var other) && Math.Abs(other - pair.Value) < 1e-9);
        }
    }
}
