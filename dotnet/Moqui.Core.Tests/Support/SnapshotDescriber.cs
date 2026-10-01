using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Moqui.Core.Tests.Support
{
    /// <summary>
    /// 스냅샷의 모든 공개 속성을 재귀적으로 문자열로 펼친다. 스냅샷에 필드가 추가되면 자동으로 비교 대상이 된다.
    /// </summary>
    public static class SnapshotDescriber
    {
        public static string Describe(object value)
        {
            var builder = new StringBuilder();
            Append(builder, value, "snapshot");
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, object value, string path)
        {
            if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum || value.GetType().Namespace == "System.Numerics")
            {
                builder.Append(path).Append('=').Append(System.Convert.ToString(value, CultureInfo.InvariantCulture)).Append('\n');
                return;
            }

            if (value is IEnumerable sequence)
            {
                int index = 0;
                foreach (object item in sequence)
                {
                    Append(builder, item, $"{path}[{index++}]");
                }

                return;
            }

            foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name))
            {
                Append(builder, property.GetValue(value), $"{path}.{property.Name}");
            }
        }
    }
}
