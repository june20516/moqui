using System.Collections.Generic;
using System.Numerics;

namespace Moqui.Core.Data
{
    /// <summary>
    /// data/tuning.json의 평평한 키-값 (tech/architecture.md §5). 정본 문서는 spec/tuning.md이다.
    /// 값 형식: 숫자, 문자열, 범위 {min, max}, 배열(레벨별 값 또는 벡터).
    /// </summary>
    public sealed class Tuning
    {
        private const string RangeMin = "min";
        private const string RangeMax = "max";
        private const int Vector3Length = 3;

        private readonly IReadOnlyDictionary<string, JsonValue> _values;

        public Tuning(IReadOnlyDictionary<string, JsonValue> values)
        {
            _values = values;
        }

        public IEnumerable<string> Keys => _values.Keys;

        public int Count => _values.Count;

        public bool Contains(string key)
        {
            return _values.ContainsKey(key);
        }

        public JsonValue GetRaw(string key)
        {
            if (!_values.TryGetValue(key, out var value))
            {
                throw new KeyNotFoundException($"Tuning key '{key}' is missing from data/tuning.json.");
            }

            return value;
        }

        public float GetFloat(string key)
        {
            return (float)GetNumber(key, GetRaw(key));
        }

        public int GetInt(string key)
        {
            double number = GetNumber(key, GetRaw(key));
            int integer = (int)number;
            if (integer != number)
            {
                throw new DataFormatException($"Tuning key '{key}' must be an integer but was {number}.");
            }

            return integer;
        }

        public string GetString(string key)
        {
            var value = GetRaw(key);
            if (value.Kind != JsonKind.String)
            {
                throw WrongKind(key, "string", value);
            }

            return value.StringValue;
        }

        public FloatRange GetRange(string key)
        {
            var value = GetRaw(key);
            if (value.Kind != JsonKind.Object
                || !value.TryGetMember(RangeMin, out var min)
                || !value.TryGetMember(RangeMax, out var max))
            {
                throw WrongKind(key, "range {min, max}", value);
            }

            return new FloatRange((float)GetNumber(key, min), (float)GetNumber(key, max));
        }

        /// <summary>레벨별 값 배열 (예: skill.cost.A = [60, 120, 200]).</summary>
        public IReadOnlyList<float> GetFloats(string key)
        {
            var value = GetRaw(key);
            if (value.Kind != JsonKind.Array)
            {
                throw WrongKind(key, "array", value);
            }

            var result = new float[value.Items.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (float)GetNumber(key, value.Items[i]);
            }

            return result;
        }

        public Vector3 GetVector3(string key)
        {
            var values = GetFloats(key);
            if (values.Count != Vector3Length)
            {
                throw new DataFormatException($"Tuning key '{key}' must have {Vector3Length} components but had {values.Count}.");
            }

            return new Vector3(values[0], values[1], values[2]);
        }

        private static double GetNumber(string key, JsonValue value)
        {
            if (value.Kind != JsonKind.Number)
            {
                throw WrongKind(key, "number", value);
            }

            return value.NumberValue;
        }

        private static DataFormatException WrongKind(string key, string expected, JsonValue actual)
        {
            return new DataFormatException($"Tuning key '{key}' must be {expected} but was {actual}.");
        }
    }
}
