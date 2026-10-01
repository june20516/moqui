using System.Collections.Generic;
using System.Numerics;

namespace Moqui.Core.Data.Levels
{
    /// <summary>방 데이터 (data/rooms/*.json): 벽, 가구 형상, Shadow Zone. 여러 스테이지가 공유한다 (spec/07).</summary>
    public sealed class RoomDefinition
    {
        public const int SupportedFormatVersion = 1;

        public RoomDefinition(string id, Vector3 size, IReadOnlyList<ShapeDefinition> shapes)
        {
            Id = id;
            Size = size;
            Shapes = shapes;
        }

        public string Id { get; }

        /// <summary>방 내부 크기 (X×Y×Z). 원점은 바닥 중앙.</summary>
        public Vector3 Size { get; }

        public IReadOnlyList<ShapeDefinition> Shapes { get; }

        public static string FilePath(string id)
        {
            return $"rooms/{id}.json";
        }

        public static RoomDefinition Parse(string text, string file)
        {
            var root = JsonAccess.Parse(text, file);
            if (root.Get("formatVersion").Int() != SupportedFormatVersion)
            {
                throw root.Get("formatVersion").Error($"must be {SupportedFormatVersion}");
            }

            var shapes = new List<ShapeDefinition>();
            foreach (var item in root.Get("shapes").Items())
            {
                shapes.Add(ShapeDefinition.Parse(item));
            }

            return new RoomDefinition(root.Get("id").String(), root.Get("size").Vector3(), shapes);
        }
    }
}
