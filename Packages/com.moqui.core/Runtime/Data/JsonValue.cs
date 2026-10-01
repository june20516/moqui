using System;
using System.Collections.Generic;

namespace Moqui.Core.Data
{
    public enum JsonKind
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object,
    }

    /// <summary>파싱된 JSON 값. 데이터 로더가 명시적으로 읽어 DTO로 옮긴다.</summary>
    public sealed class JsonValue
    {
        private static readonly IReadOnlyList<JsonValue> EmptyItems = new JsonValue[0];
        private static readonly IReadOnlyDictionary<string, JsonValue> EmptyMembers = new Dictionary<string, JsonValue>();

        public static readonly JsonValue Null = new JsonValue(JsonKind.Null, false, 0, null, EmptyItems, EmptyMembers);
        public static readonly JsonValue True = new JsonValue(JsonKind.Bool, true, 0, null, EmptyItems, EmptyMembers);
        public static readonly JsonValue False = new JsonValue(JsonKind.Bool, false, 0, null, EmptyItems, EmptyMembers);

        private JsonValue(
            JsonKind kind,
            bool boolValue,
            double number,
            string text,
            IReadOnlyList<JsonValue> items,
            IReadOnlyDictionary<string, JsonValue> members)
        {
            Kind = kind;
            BoolValue = boolValue;
            NumberValue = number;
            StringValue = text;
            Items = items;
            Members = members;
        }

        public JsonKind Kind { get; }

        public bool BoolValue { get; }

        public double NumberValue { get; }

        public string StringValue { get; }

        public IReadOnlyList<JsonValue> Items { get; }

        public IReadOnlyDictionary<string, JsonValue> Members { get; }

        public static JsonValue FromNumber(double number)
        {
            return new JsonValue(JsonKind.Number, false, number, null, EmptyItems, EmptyMembers);
        }

        public static JsonValue FromString(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return new JsonValue(JsonKind.String, false, 0, text, EmptyItems, EmptyMembers);
        }

        public static JsonValue FromArray(IReadOnlyList<JsonValue> items)
        {
            return new JsonValue(JsonKind.Array, false, 0, null, items, EmptyMembers);
        }

        public static JsonValue FromObject(IReadOnlyDictionary<string, JsonValue> members)
        {
            return new JsonValue(JsonKind.Object, false, 0, null, EmptyItems, members);
        }

        public bool TryGetMember(string name, out JsonValue value)
        {
            return Members.TryGetValue(name, out value);
        }

        /// <summary>구조와 값이 같은지 비교한다. 숫자는 정확히 같아야 한다.</summary>
        public bool StructurallyEquals(JsonValue other)
        {
            if (other == null || other.Kind != Kind)
            {
                return false;
            }

            switch (Kind)
            {
                case JsonKind.Null:
                    return true;
                case JsonKind.Bool:
                    return BoolValue == other.BoolValue;
                case JsonKind.Number:
                    return NumberValue.Equals(other.NumberValue);
                case JsonKind.String:
                    return string.Equals(StringValue, other.StringValue, StringComparison.Ordinal);
                case JsonKind.Array:
                    if (Items.Count != other.Items.Count)
                    {
                        return false;
                    }

                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (!Items[i].StructurallyEquals(other.Items[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                case JsonKind.Object:
                    if (Members.Count != other.Members.Count)
                    {
                        return false;
                    }

                    foreach (var pair in Members)
                    {
                        if (!other.Members.TryGetValue(pair.Key, out var otherValue) || !pair.Value.StructurallyEquals(otherValue))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return false;
            }
        }

        public override string ToString()
        {
            return JsonWriter.Write(this);
        }
    }
}
