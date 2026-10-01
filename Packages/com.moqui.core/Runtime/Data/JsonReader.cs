using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Moqui.Core.Data
{
    /// <summary>
    /// 최소 JSON 파서 (RFC 8259). Unity와 dotnet 양쪽에서 같은 결과를 내기 위해 외부 라이브러리 없이 구현한다.
    /// </summary>
    public sealed class JsonReader
    {
        private const int UnicodeEscapeLength = 4;

        private readonly string _text;
        private int _position;

        private JsonReader(string text)
        {
            _text = text;
        }

        private bool AtEnd => _position >= _text.Length;

        private char Current => _text[_position];

        public static JsonValue Parse(string text)
        {
            if (text == null)
            {
                throw new DataFormatException("JSON text is null.");
            }

            var reader = new JsonReader(text);
            reader.SkipWhitespace();
            JsonValue value = reader.ReadValue();
            reader.SkipWhitespace();
            if (!reader.AtEnd)
            {
                throw reader.Error("Unexpected trailing content");
            }

            return value;
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private JsonValue ReadValue()
        {
            if (AtEnd)
            {
                throw Error("Unexpected end of input");
            }

            switch (Current)
            {
                case '{':
                    return ReadObject();
                case '[':
                    return ReadArray();
                case '"':
                    return JsonValue.FromString(ReadString());
                case 't':
                    ExpectLiteral("true");
                    return JsonValue.True;
                case 'f':
                    ExpectLiteral("false");
                    return JsonValue.False;
                case 'n':
                    ExpectLiteral("null");
                    return JsonValue.Null;
                default:
                    if (Current == '-' || IsDigit(Current))
                    {
                        return ReadNumber();
                    }

                    throw Error($"Unexpected character '{Current}'");
            }
        }

        private JsonValue ReadObject()
        {
            _position++;
            var members = new Dictionary<string, JsonValue>();
            SkipWhitespace();
            if (!AtEnd && Current == '}')
            {
                _position++;
                return JsonValue.FromObject(members);
            }

            while (true)
            {
                SkipWhitespace();
                if (AtEnd || Current != '"')
                {
                    throw Error("Expected object key");
                }

                string key = ReadString();
                if (members.ContainsKey(key))
                {
                    throw Error($"Duplicate key '{key}'");
                }

                SkipWhitespace();
                Expect(':');
                SkipWhitespace();
                members[key] = ReadValue();
                SkipWhitespace();
                if (AtEnd)
                {
                    throw Error("Unterminated object");
                }

                if (Current == ',')
                {
                    _position++;
                    continue;
                }

                Expect('}');
                return JsonValue.FromObject(members);
            }
        }

        private JsonValue ReadArray()
        {
            _position++;
            var items = new List<JsonValue>();
            SkipWhitespace();
            if (!AtEnd && Current == ']')
            {
                _position++;
                return JsonValue.FromArray(items);
            }

            while (true)
            {
                SkipWhitespace();
                items.Add(ReadValue());
                SkipWhitespace();
                if (AtEnd)
                {
                    throw Error("Unterminated array");
                }

                if (Current == ',')
                {
                    _position++;
                    continue;
                }

                Expect(']');
                return JsonValue.FromArray(items);
            }
        }

        private string ReadString()
        {
            Expect('"');
            var builder = new StringBuilder();
            while (true)
            {
                if (AtEnd)
                {
                    throw Error("Unterminated string");
                }

                char c = Current;
                _position++;
                if (c == '"')
                {
                    return builder.ToString();
                }

                if (c < ' ')
                {
                    throw Error("Control character in string");
                }

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (AtEnd)
                {
                    throw Error("Unterminated escape");
                }

                char escape = Current;
                _position++;
                builder.Append(DecodeEscape(escape));
            }
        }

        private char DecodeEscape(char escape)
        {
            switch (escape)
            {
                case '"':
                    return '"';
                case '\\':
                    return '\\';
                case '/':
                    return '/';
                case 'b':
                    return '\b';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case 'u':
                    return ReadUnicodeEscape();
                default:
                    throw Error($"Invalid escape '\\{escape}'");
            }
        }

        private char ReadUnicodeEscape()
        {
            if (_position + UnicodeEscapeLength > _text.Length)
            {
                throw Error("Incomplete unicode escape");
            }

            string hex = _text.Substring(_position, UnicodeEscapeLength);
            if (!int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
            {
                throw Error($"Invalid unicode escape '{hex}'");
            }

            _position += UnicodeEscapeLength;
            return (char)code;
        }

        private JsonValue ReadNumber()
        {
            int start = _position;
            if (Current == '-')
            {
                _position++;
            }

            if (AtEnd || !IsDigit(Current))
            {
                throw Error("Invalid number");
            }

            if (Current == '0')
            {
                _position++;
            }
            else
            {
                SkipDigits();
            }

            if (!AtEnd && Current == '.')
            {
                _position++;
                if (AtEnd || !IsDigit(Current))
                {
                    throw Error("Invalid number fraction");
                }

                SkipDigits();
            }

            if (!AtEnd && (Current == 'e' || Current == 'E'))
            {
                _position++;
                if (!AtEnd && (Current == '+' || Current == '-'))
                {
                    _position++;
                }

                if (AtEnd || !IsDigit(Current))
                {
                    throw Error("Invalid number exponent");
                }

                SkipDigits();
            }

            string token = _text.Substring(start, _position - start);
            return JsonValue.FromNumber(double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture));
        }

        private void SkipDigits()
        {
            while (!AtEnd && IsDigit(Current))
            {
                _position++;
            }
        }

        private void SkipWhitespace()
        {
            while (!AtEnd && (Current == ' ' || Current == '\t' || Current == '\n' || Current == '\r'))
            {
                _position++;
            }
        }

        private void Expect(char expected)
        {
            if (AtEnd || Current != expected)
            {
                throw Error($"Expected '{expected}'");
            }

            _position++;
        }

        private void ExpectLiteral(string literal)
        {
            if (string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0)
            {
                throw Error($"Expected '{literal}'");
            }

            _position += literal.Length;
        }

        private DataFormatException Error(string message)
        {
            int line = 1;
            int column = 1;
            for (int i = 0; i < _position && i < _text.Length; i++)
            {
                if (_text[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return new DataFormatException($"{message} at line {line}, column {column}.");
        }
    }
}
