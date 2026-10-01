using System;
using System.Collections.Generic;
using System.Numerics;

namespace Moqui.Core.Data
{
    /// <summary>
    /// JSON 값을 명시적으로 읽는 도우미 (리플렉션 직렬화 대신, tech/conventions.md §3).
    /// 오류 메시지에 파일과 경로를 담아 데이터 수정 위치를 바로 알 수 있게 한다.
    /// </summary>
    public readonly struct JsonAccess
    {
        private readonly JsonValue _value;

        public JsonAccess(JsonValue value, string path)
        {
            _value = value;
            Path = path;
        }

        public string Path { get; }

        public JsonValue Value => _value;

        public static JsonAccess Parse(string text, string file)
        {
            try
            {
                return new JsonAccess(JsonReader.Parse(text), file);
            }
            catch (DataFormatException exception)
            {
                throw new DataFormatException($"{file}: {exception.Message}");
            }
        }

        public bool Has(string name)
        {
            return _value.Kind == JsonKind.Object && _value.TryGetMember(name, out var member) && member.Kind != JsonKind.Null;
        }

        public JsonAccess Get(string name)
        {
            if (_value.Kind != JsonKind.Object)
            {
                throw Error("must be an object");
            }

            if (!_value.TryGetMember(name, out var member))
            {
                throw new DataFormatException($"{Path}.{name}: missing");
            }

            return new JsonAccess(member, $"{Path}.{name}");
        }

        public IReadOnlyList<JsonAccess> Items()
        {
            if (_value.Kind != JsonKind.Array)
            {
                throw Error("must be an array");
            }

            var items = new JsonAccess[_value.Items.Count];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new JsonAccess(_value.Items[i], $"{Path}[{i}]");
            }

            return items;
        }

        /// <summary>선택 배열: 없거나 null이면 빈 목록.</summary>
        public IReadOnlyList<JsonAccess> OptionalItems(string name)
        {
            return Has(name) ? Get(name).Items() : Array.Empty<JsonAccess>();
        }

        public string String()
        {
            if (_value.Kind != JsonKind.String)
            {
                throw Error("must be a string");
            }

            return _value.StringValue;
        }

        public float Float()
        {
            if (_value.Kind != JsonKind.Number)
            {
                throw Error("must be a number");
            }

            return (float)_value.NumberValue;
        }

        public int Int()
        {
            float number = Float();
            int integer = (int)number;
            if (integer != number)
            {
                throw Error("must be an integer");
            }

            return integer;
        }

        public ulong ULong()
        {
            if (_value.Kind != JsonKind.Number || _value.NumberValue < 0 || Math.Floor(_value.NumberValue) != _value.NumberValue)
            {
                throw Error("must be a non-negative integer");
            }

            return (ulong)_value.NumberValue;
        }

        public bool Bool()
        {
            if (_value.Kind != JsonKind.Bool)
            {
                throw Error("must be true or false");
            }

            return _value.BoolValue;
        }

        public Vector3 Vector3()
        {
            var items = Items();
            if (items.Count != 3)
            {
                throw Error("must be [x, y, z]");
            }

            return new Vector3(items[0].Float(), items[1].Float(), items[2].Float());
        }

        public FloatRange Range()
        {
            return new FloatRange(Get("min").Float(), Get("max").Float());
        }

        public TEnum Enum<TEnum>()
            where TEnum : struct
        {
            string text = String();
            foreach (TEnum candidate in (TEnum[])System.Enum.GetValues(typeof(TEnum)))
            {
                if (string.Equals(candidate.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            throw Error($"unknown value '{text}'");
        }

        public DataFormatException Error(string message)
        {
            return new DataFormatException($"{Path}: {message}");
        }
    }
}
