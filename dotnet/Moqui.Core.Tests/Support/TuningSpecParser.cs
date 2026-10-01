using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Moqui.Core.Data;

namespace Moqui.Core.Tests.Support
{
    /// <summary>
    /// spec/tuning.md의 표를 읽어 tuning.json과 같은 형태의 기대값으로 바꾼다.
    /// 값 표기 규칙:
    ///   "60 u/s", "+40", "−0.05", "±85°", "5%" → 숫자 (단위·부호 기호 무시, '±'는 절댓값)
    ///   "1.0~1.5s" → {min, max}
    ///   "60 / 120 / 200", "20s / 14s" → 레벨별 배열
    ///   "(0, 0.15, 0.1)u" → 벡터 배열
    ///   "ThirdPerson" → 문자열
    /// 값 열이 여러 개인 표(site)는 열 이름을 키 접미사로 붙인다.
    /// </summary>
    public static class TuningSpecParser
    {
        private const string Number = @"[+\-]?\d+(?:\.\d+)?";

        private static readonly Dictionary<string, string> ColumnSuffixes = new Dictionary<string, string>
        {
            ["민감도"] = "sensitivity",
            ["혈액량"] = "bloodAmount",
        };

        private static readonly Regex KeyPattern = new Regex(@"^[a-zA-Z][a-zA-Z0-9]*(\.[a-zA-Z0-9]+)+$");
        private static readonly Regex IdentifierPattern = new Regex(@"^[A-Za-z]+$");
        private static readonly Regex VectorPattern = new Regex(@"^\(([^)]*)\)");
        private static readonly Regex RangePattern = new Regex($@"^({Number})\s*[a-z°%/]*\s*~\s*({Number})");
        private static readonly Regex PerLevelSeparator = new Regex(@"\s+/\s+");
        private static readonly Regex LeadingNumber = new Regex($@"^{Number}");

        public static IReadOnlyList<KeyValuePair<string, JsonValue>> Parse(string markdown)
        {
            var result = new List<KeyValuePair<string, JsonValue>>();
            string[] lines = markdown.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string[] header = SplitRow(lines[i]);
                if (header == null || header.Length < 2 || header[0] != "키")
                {
                    continue;
                }

                string[] valueColumns = header.Skip(1).ToArray();
                int valueColumnCount = valueColumns[0] == "값" ? 1 : valueColumns.Length;

                // 헤더 다음 줄은 구분선
                for (i += 2; i < lines.Length; i++)
                {
                    string[] cells = SplitRow(lines[i]);
                    if (cells == null)
                    {
                        break;
                    }

                    string key = cells[0];
                    if (!KeyPattern.IsMatch(key))
                    {
                        throw new FormatException($"Invalid tuning key '{key}' in line: {lines[i]}");
                    }

                    if (valueColumnCount == 1)
                    {
                        result.Add(new KeyValuePair<string, JsonValue>(key, ParseValue(key, cells[1])));
                        continue;
                    }

                    for (int c = 0; c < valueColumnCount; c++)
                    {
                        if (!ColumnSuffixes.TryGetValue(valueColumns[c], out string suffix))
                        {
                            throw new FormatException($"Unknown tuning column '{valueColumns[c]}'. Add it to ColumnSuffixes.");
                        }

                        string columnKey = $"{key}.{suffix}";
                        result.Add(new KeyValuePair<string, JsonValue>(columnKey, ParseValue(columnKey, cells[1 + c])));
                    }
                }
            }

            return result;
        }

        public static JsonValue ParseValue(string key, string cell)
        {
            string text = cell.Replace('−', '-').Trim();
            if (text.StartsWith("±", StringComparison.Ordinal))
            {
                text = text.Substring(1);
            }

            if (IdentifierPattern.IsMatch(text))
            {
                return JsonValue.FromString(text);
            }

            var vector = VectorPattern.Match(text);
            if (vector.Success)
            {
                var items = vector.Groups[1].Value.Split(',').Select(part => ParseNumber(key, part.Trim())).ToArray();
                return JsonValue.FromArray(items);
            }

            var range = RangePattern.Match(text);
            if (range.Success)
            {
                return JsonValue.FromObject(new Dictionary<string, JsonValue>
                {
                    ["min"] = ParseNumber(key, range.Groups[1].Value),
                    ["max"] = ParseNumber(key, range.Groups[2].Value),
                });
            }

            string[] perLevel = PerLevelSeparator.Split(text);
            if (perLevel.Length > 1 && perLevel.All(part => LeadingNumber.IsMatch(part)))
            {
                var items = perLevel.Select(part => ParseNumber(key, LeadingNumber.Match(part).Value)).ToArray();
                return JsonValue.FromArray(items);
            }

            var leading = LeadingNumber.Match(text);
            if (!leading.Success)
            {
                throw new FormatException($"Cannot parse value '{cell}' for key '{key}'.");
            }

            return ParseNumber(key, leading.Value);
        }

        private static JsonValue ParseNumber(string key, string text)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                throw new FormatException($"Cannot parse number '{text}' for key '{key}'.");
            }

            return JsonValue.FromNumber(number);
        }

        private static string[] SplitRow(string line)
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("|", StringComparison.Ordinal) || !trimmed.EndsWith("|", StringComparison.Ordinal))
            {
                return null;
            }

            return trimmed.Substring(1, trimmed.Length - 2).Split('|').Select(cell => cell.Trim()).ToArray();
        }
    }
}
