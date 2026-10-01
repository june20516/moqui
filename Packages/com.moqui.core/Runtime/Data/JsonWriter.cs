using System.Globalization;
using System.Text;

namespace Moqui.Core.Data
{
    /// <summary>진단 메시지용 한 줄 JSON 직렬화.</summary>
    public static class JsonWriter
    {
        public static string Write(JsonValue value)
        {
            var builder = new StringBuilder();
            Append(builder, value);
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, JsonValue value)
        {
            switch (value.Kind)
            {
                case JsonKind.Null:
                    builder.Append("null");
                    break;
                case JsonKind.Bool:
                    builder.Append(value.BoolValue ? "true" : "false");
                    break;
                case JsonKind.Number:
                    builder.Append(value.NumberValue.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case JsonKind.String:
                    AppendString(builder, value.StringValue);
                    break;
                case JsonKind.Array:
                    builder.Append('[');
                    for (int i = 0; i < value.Items.Count; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(", ");
                        }

                        Append(builder, value.Items[i]);
                    }

                    builder.Append(']');
                    break;
                case JsonKind.Object:
                    builder.Append('{');
                    bool first = true;
                    foreach (var pair in value.Members)
                    {
                        if (!first)
                        {
                            builder.Append(", ");
                        }

                        first = false;
                        AppendString(builder, pair.Key);
                        builder.Append(": ");
                        Append(builder, pair.Value);
                    }

                    builder.Append('}');
                    break;
            }
        }

        private static void AppendString(StringBuilder builder, string text)
        {
            builder.Append('"');
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
        }
    }
}
