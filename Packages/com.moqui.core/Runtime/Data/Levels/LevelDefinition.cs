using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;

namespace Moqui.Core.Data.Levels
{
    /// <summary>
    /// 레벨 데이터 (data/levels/stageNN.json, tech/architecture.md §5): 방 참조 + 인간 + 기믹 + 시작 위치 + 시드 + 튜토리얼.
    /// 선풍기·거미줄·모기향·분사기는 M9에서 채운다(지금은 배열이 비어 있어야 한다).
    /// </summary>
    public sealed class LevelDefinition
    {
        public const int SupportedFormatVersion = 1;

        private static readonly string[] GimmickArrays = { "fans", "webs", "coils", "sprayDispensers" };

        private LevelDefinition()
        {
        }

        public string Id { get; private set; }

        public RoomDefinition Room { get; private set; }

        public ulong Seed { get; private set; }

        public Vector3 PlayerSpawn { get; private set; }

        public HumanDefinition Human { get; private set; }

        public IReadOnlyList<Vector3> DripSources { get; private set; }

        /// <summary>습기 영역 볼륨 (humidWeak/humidStrong 플래그를 가진 박스).</summary>
        public IReadOnlyList<ShapeDefinition> HumidZones { get; private set; }

        /// <summary>튜토리얼 안내 순서 (spec/07, spec/08). 없으면 빈 목록.</summary>
        public IReadOnlyList<string> Tutorial { get; private set; }

        public static string FilePath(string id)
        {
            return $"levels/{id}.json";
        }

        /// <summary>방과 레벨의 모든 형상 (방 형상 + 레벨 볼륨).</summary>
        public IEnumerable<ShapeDefinition> AllShapes()
        {
            foreach (var shape in Room.Shapes)
            {
                yield return shape;
            }

            foreach (var zone in HumidZones)
            {
                yield return zone;
            }
        }

        public CollisionWorld CreateWorld()
        {
            var world = new CollisionWorld();
            foreach (var shape in AllShapes())
            {
                world.Add(shape.ToCollisionShape());
            }

            return world;
        }

        public SimulationSetup CreateSetup()
        {
            return new SimulationSetup(CreateWorld, PlayerSpawn, Human, Seed, DripSources);
        }

        public static LevelDefinition Parse(string text, string file, Func<string, RoomDefinition> loadRoom)
        {
            var root = JsonAccess.Parse(text, file);
            if (root.Get("formatVersion").Int() != SupportedFormatVersion)
            {
                throw root.Get("formatVersion").Error($"must be {SupportedFormatVersion}");
            }

            foreach (string name in GimmickArrays)
            {
                if (root.OptionalItems(name).Count > 0)
                {
                    throw root.Get(name).Error("is not supported yet (M9)");
                }
            }

            var dripSources = new List<Vector3>();
            foreach (var item in root.OptionalItems("dripSources"))
            {
                dripSources.Add(item.Get("position").Vector3());
            }

            var humidZones = new List<ShapeDefinition>();
            foreach (var item in root.OptionalItems("humidZones"))
            {
                var strength = item.Get("strength").String();
                var flags = strength == "strong" ? ShapeFlags.HumidStrong : strength == "weak" ? ShapeFlags.HumidWeak : throw item.Get("strength").Error("must be strong or weak");
                humidZones.Add(ShapeDefinition.Box(item.Get("id").String(), item.Get("center").Vector3(), item.Get("size").Vector3(), flags));
            }

            var tutorial = new List<string>();
            foreach (var item in root.OptionalItems("tutorial"))
            {
                tutorial.Add(item.String());
            }

            return new LevelDefinition
            {
                Id = root.Get("id").String(),
                Room = loadRoom(root.Get("room").String()),
                Seed = root.Get("seed").ULong(),
                PlayerSpawn = root.Get("playerSpawn").Vector3(),
                Human = HumanDataParser.Parse(root.Get("human")),
                DripSources = dripSources,
                HumidZones = humidZones,
                Tutorial = tutorial,
            };
        }
    }
}
