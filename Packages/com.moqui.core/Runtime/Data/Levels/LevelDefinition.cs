using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;

namespace Moqui.Core.Data.Levels
{
    /// <summary>
    /// 레벨 데이터 (data/levels/stageNN.json, tech/architecture.md §5): 방 참조 + 인간 + 기믹 + 시작 위치 + 시드 + 튜토리얼.
    /// 선풍기 본체(장애물)와 거미줄(Hazard)은 충돌 형상으로 월드에 들어가고, 바람·모기향·분사기는 GimmickSetup으로 시뮬레이션에 간다 (D-047).
    /// </summary>
    public sealed class LevelDefinition
    {
        public const int SupportedFormatVersion = 1;

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

        /// <summary>선풍기 (바람 기준점·방향).</summary>
        public IReadOnlyList<FanDefinition> Fans { get; private set; }

        /// <summary>조명 스위치 (spec/06, M14).</summary>
        public IReadOnlyList<LightDefinition> Lights { get; private set; }

        /// <summary>선풍기 본체 형상 (장애물, 붙을 수 있음).</summary>
        public IReadOnlyList<ShapeDefinition> FanBodies { get; private set; }

        /// <summary>거미줄 형상 (Hazard, spec/06).</summary>
        public IReadOnlyList<ShapeDefinition> Webs { get; private set; }

        /// <summary>함께 있는 다른 인간들 (M14 두 사람).</summary>
        public IReadOnlyList<HumanDefinition> Companions { get; private set; }

        /// <summary>모기장 그물(유리처럼 충돌, 시야 통과)과 틈 볼륨 (spec/06, M14).</summary>
        public IReadOnlyList<ShapeDefinition> Nets { get; private set; }

        /// <summary>모기향 (id, 위치).</summary>
        public IReadOnlyList<(string Id, Vector3 Position)> Coils { get; private set; }

        /// <summary>자동 분사기 (id, 연무가 생기는 위치).</summary>
        public IReadOnlyList<(string Id, Vector3 Position)> SprayDispensers { get; private set; }

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

            foreach (var body in FanBodies)
            {
                yield return body;
            }

            foreach (var web in Webs)
            {
                yield return web;
            }

            foreach (var net in Nets)
            {
                yield return net;
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

        /// <param name="seed">시드를 바꿔 같은 레벨을 다르게 돌릴 때 (봇의 고정 시드 목록). 없으면 레벨 시드.</param>
        public SimulationSetup CreateSetup(SkillLoadout skills = null, ulong? seed = null)
        {
            var gimmicks = new GimmickSetup(Fans, Coils.Select(coil => coil.Position).ToList(), SprayDispensers.Select(dispenser => dispenser.Position).ToList(), Lights);
            return new SimulationSetup(CreateWorld, PlayerSpawn, Human, seed ?? Seed, DripSources, skills, gimmicks, Companions);
        }

        public static LevelDefinition Parse(string text, string file, Func<string, RoomDefinition> loadRoom)
        {
            var root = JsonAccess.Parse(text, file);
            if (root.Get("formatVersion").Int() != SupportedFormatVersion)
            {
                throw root.Get("formatVersion").Error($"must be {SupportedFormatVersion}");
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

            var fans = new List<FanDefinition>();
            var fanBodies = new List<ShapeDefinition>();
            foreach (var item in root.OptionalItems("fans"))
            {
                string id = item.Get("id").String();
                Vector3 position = item.Get("position").Vector3();
                FanKind kind = item.Has("kind") ? item.Get("kind").Enum<FanKind>() : FanKind.Fan;
                FanSchedule schedule = null;
                if (item.Has("schedule"))
                {
                    var scheduleJson = item.Get("schedule");
                    schedule = new FanSchedule(scheduleJson.Get("on").Float(), scheduleJson.Get("off").Float(), scheduleJson.Has("offset") ? scheduleJson.Get("offset").Float() : 0f);
                }

                fans.Add(new FanDefinition(id, position, item.Get("yaw").Float(), item.Has("pitch") ? item.Get("pitch").Float() : 0f, kind, schedule));
                fanBodies.Add(ShapeDefinition.Box(id, position, item.Get("bodySize").Vector3(), ShapeFlags.Obstacle | ShapeFlags.Attachable));
            }

            var nets = new List<ShapeDefinition>();
            foreach (var item in root.OptionalItems("nets"))
            {
                nets.Add(ShapeDefinition.Box(item.Get("id").String(), item.Get("center").Vector3(), item.Get("size").Vector3(), ShapeFlags.Glass | ShapeFlags.Net | ShapeFlags.Attachable));
            }

            foreach (var item in root.OptionalItems("netGaps"))
            {
                nets.Add(ShapeDefinition.Box(item.Get("id").String(), item.Get("center").Vector3(), item.Get("size").Vector3(), ShapeFlags.NetGap));
            }

            var webs = new List<ShapeDefinition>();
            foreach (var item in root.OptionalItems("webs"))
            {
                Vector3 rotation = item.Has("rotation") ? item.Get("rotation").Vector3() : Vector3.Zero;
                webs.Add(ShapeDefinition.Box(item.Get("id").String(), item.Get("center").Vector3(), item.Get("size").Vector3(), ShapeFlags.Hazard, rotation));
            }

            var coils = new List<(string, Vector3)>();
            foreach (var item in root.OptionalItems("coils"))
            {
                coils.Add((item.Get("id").String(), item.Get("position").Vector3()));
            }

            var dispensers = new List<(string, Vector3)>();
            foreach (var item in root.OptionalItems("sprayDispensers"))
            {
                dispensers.Add((item.Get("id").String(), item.Get("position").Vector3()));
            }

            var companions = new List<HumanDefinition>();
            foreach (var item in root.OptionalItems("companions"))
            {
                companions.Add(HumanDataParser.Parse(item));
            }

            var lights = new List<LightDefinition>();
            foreach (var item in root.OptionalItems("lights"))
            {
                FanSchedule schedule = null;
                if (item.Has("schedule"))
                {
                    var scheduleJson = item.Get("schedule");
                    schedule = new FanSchedule(scheduleJson.Get("on").Float(), scheduleJson.Get("off").Float(), scheduleJson.Has("offset") ? scheduleJson.Get("offset").Float() : 0f);
                }

                var area = item.Get("area");
                lights.Add(new LightDefinition(
                    item.Get("id").String(),
                    item.Get("position").Vector3(),
                    area.Get("center").Vector3(),
                    area.Get("size").Vector3() * 0.5f,
                    schedule,
                    item.Has("onWhenAlert") && item.Get("onWhenAlert").Bool()));
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
                Lights = lights,
                Companions = companions,
                DripSources = dripSources,
                HumidZones = humidZones,
                Fans = fans,
                FanBodies = fanBodies,
                Webs = webs,
                Nets = nets,
                Coils = coils,
                SprayDispensers = dispensers,
                Tutorial = tutorial,
            };
        }
    }
}
