using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Json.Schema;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Data
{
    public class LevelDataTests
    {
        private const float TableTolerance = 5f;

        private static readonly string[] AllLevels = { "stage01", "stage02", "stage03" };

        // JsonSchema.Net은 $id로 스키마를 전역 등록하므로 같은 스키마를 두 번 읽지 않는다.
        private static readonly System.Lazy<JsonSchema> RoomSchema = new System.Lazy<JsonSchema>(() => JsonSchema.FromText(File.ReadAllText(Path.Combine(RepoPaths.Data, "schema", "room.schema.json"))));
        private static readonly System.Lazy<JsonSchema> LevelSchema = new System.Lazy<JsonSchema>(() => JsonSchema.FromText(File.ReadAllText(Path.Combine(RepoPaths.Data, "schema", "level.schema.json"))));

        private static LevelLoader Loader => new LevelLoader(FileSystemDataSource.ForRepoData());

        private static IEnumerable<string> RoomFiles => Directory.GetFiles(Path.Combine(RepoPaths.Data, "rooms"), "*.json");

        private static IEnumerable<string> LevelFiles => Directory.GetFiles(Path.Combine(RepoPaths.Data, "levels"), "*.json");

        [Test]
        public void DataFiles_MatchJsonSchemas()
        {
            var roomSchema = RoomSchema.Value;
            var levelSchema = LevelSchema.Value;
            var failures = new List<string>();

            foreach (var (schema, file) in RoomFiles.Select(f => (roomSchema, f)).Concat(LevelFiles.Select(f => (levelSchema, f))))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var result = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
                if (!result.IsValid)
                {
                    var details = (result.Details ?? new List<EvaluationResults>()).Where(d => d.Errors != null && d.Errors.Count > 0).SelectMany(d => d.Errors.Select(e => $"{d.InstanceLocation}: {e.Value}"));
                    failures.Add($"{Path.GetFileName(file)}: {string.Join("; ", details.Take(5))}");
                }
            }

            Assert.That(RoomFiles.Count(), Is.GreaterThanOrEqualTo(1));
            Assert.That(LevelFiles.Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(failures, Is.Empty);
        }

        [Test]
        public void Schema_RejectsInvalidLevel()
        {
            var levelSchema = LevelSchema.Value;
            using var document = JsonDocument.Parse("{\"formatVersion\": 1, \"id\": \"stage99\", \"room\": \"x\", \"seed\": -1, \"playerSpawn\": [0, 0], \"human\": {}}");

            Assert.That(levelSchema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List }).IsValid, Is.False);
        }

        [TestCase("sofa_seat", 0f, 22f, 160f, 200f, 45f, 80f)]
        [TestCase("sofa_back", 0f, 65f, 195f, 200f, 40f, 10f)]
        [TestCase("tv_stand", 0f, 25f, -180f, 150f, 50f, 40f)]
        [TestCase("tv", 0f, 85f, -190f, 110f, 65f, 5f)]
        [TestCase("coffee_table_top", 0f, 40f, 40f, 100f, 4f, 50f)]
        [TestCase("bookshelf", 220f, 90f, -50f, 40f, 180f, 100f)]
        [TestCase("curtain", -245f, 125f, -120f, 10f, 230f, 120f)]
        [TestCase("air_conditioner", 180f, 220f, 195f, 90f, 30f, 25f)]
        public void LivingRoom_BoxFurniture_MatchesSpecTableWithin5u(string id, float x, float y, float z, float w, float h, float d)
        {
            var shape = Loader.LoadRoom("livingRoom").Shapes.Single(s => s.Id == id);

            Assert.That(Vector3.Distance(shape.Center, new Vector3(x, y, z)), Is.LessThanOrEqualTo(TableTolerance), "center");
            Assert.That(MaxComponent(shape.Size - new Vector3(w, h, d)), Is.LessThanOrEqualTo(TableTolerance), "size");
        }

        [Test]
        public void LivingRoom_RoundFurniture_MatchesSpecTableWithin5u()
        {
            var room = Loader.LoadRoom("livingRoom");
            var lamp = room.Shapes.Single(s => s.Id == "floor_lamp");
            var plant = room.Shapes.Single(s => s.Id == "hanging_plant");

            Assert.That(Vector3.Distance(lamp.Center, new Vector3(-200, 80, 150)), Is.LessThanOrEqualTo(TableTolerance));
            Assert.That(lamp.Radius * 2f, Is.EqualTo(30f).Within(TableTolerance));
            Assert.That(Vector3.Distance(lamp.PointA, lamp.PointB) + (lamp.Radius * 2f), Is.EqualTo(160f).Within(TableTolerance), "height");
            Assert.That(Vector3.Distance(plant.Center, new Vector3(-150, 200, 0)), Is.LessThanOrEqualTo(TableTolerance));
            Assert.That(plant.Radius * 2f, Is.EqualTo(30f).Within(TableTolerance));
            Assert.That(room.Size, Is.EqualTo(new Vector3(500, 250, 400)));
        }

        [Test]
        public void LivingRoom_ShadowZones_UnderTableBehindShelfBehindCurtain()
        {
            var zones = Loader.LoadRoom("livingRoom").Shapes.Where(s => (s.Flags & ShapeFlags.ShadowZone) != 0).Select(s => s.Id).ToList();

            Assert.That(zones, Is.EquivalentTo(new[] { "shadow_coffee_table", "shadow_bookshelf", "shadow_curtain" }));
        }

        [Test]
        public void Stage1And2_ReferenceTheSameLivingRoom()
        {
            var loader = Loader;
            var stage1 = loader.Load("stage01");
            var stage2 = loader.Load("stage02");

            Assert.That(stage1.Room.Id, Is.EqualTo("livingRoom"));
            Assert.That(stage2.Room, Is.SameAs(stage1.Room), "one room definition shared by both stages");
        }

        [TestCaseSource(nameof(AllLevels))]
        public void Level_PassesCommonLevelChecks(string levelId)
        {
            var level = Loader.Load(levelId);

            var errors = LevelValidator.Validate(level, TestSimulations.Settings);

            Assert.That(errors, Is.Empty);
        }

        [TestCaseSource(nameof(AllLevels))]
        public void Level_AllFurnitureHasObstacleFlag(string levelId)
        {
            var level = Loader.Load(levelId);
            var volumes = ShapeFlags.ShadowZone | ShapeFlags.HumidWeak | ShapeFlags.HumidStrong | ShapeFlags.Wind | ShapeFlags.Hazard;

            var furniture = level.AllShapes().Where(s => (s.Flags & volumes) == 0).ToList();

            Assert.That(furniture.Count, Is.GreaterThan(10));
            Assert.That(furniture.Where(s => (s.Flags & ShapeFlags.Solid) == 0).Select(s => s.Id), Is.Empty);
        }

        [TestCaseSource(nameof(AllLevels))]
        public void Level_DripSourcesAreHighEnough(string levelId)
        {
            var level = Loader.Load(levelId);
            var world = level.CreateWorld();

            Assert.That(level.DripSources.Where(s => !LevelChecks.DripSourceHighEnough(world, s, TestSimulations.Settings.Water)), Is.Empty);
        }

        [Test]
        public void Validator_SpawnInsideYellowZone_Reported()
        {
            string text = File.ReadAllText(Path.Combine(RepoPaths.Data, "levels", "stage02.json")).Replace("\"playerSpawn\": [-240, 120, -190]", "\"playerSpawn\": [0, 115, 0]");
            var level = LevelDefinition.Parse(text, "stage02-mutated", Loader.LoadRoom);

            var errors = LevelValidator.Validate(level, TestSimulations.Settings);

            Assert.That(errors.Any(e => e.Contains("Yellow Zone")), Is.True, string.Join("\n", errors));
        }

        [Test]
        public void Validator_FurnitureWithoutObstacleFlag_Reported()
        {
            string roomText = File.ReadAllText(Path.Combine(RepoPaths.Data, "rooms", "livingRoom.json")).Replace("{ \"id\": \"tv\", \"type\": \"box\", \"center\": [0, 85, -190], \"size\": [110, 65, 5], \"flags\": [\"obstacle\", \"attachable\"] }", "{ \"id\": \"tv\", \"type\": \"box\", \"center\": [0, 85, -190], \"size\": [110, 65, 5], \"flags\": [\"attachable\"] }");
            var room = RoomDefinition.Parse(roomText, "livingRoom-mutated");
            var level = LevelDefinition.Parse(File.ReadAllText(Path.Combine(RepoPaths.Data, "levels", "stage01.json")), "stage01", _ => room);

            var errors = LevelValidator.Validate(level, TestSimulations.Settings);

            Assert.That(errors.Any(e => e.Contains("'tv'")), Is.True, string.Join("\n", errors));
        }

        [Test]
        public void Loader_InvalidField_ErrorNamesFileAndPath()
        {
            string text = File.ReadAllText(Path.Combine(RepoPaths.Data, "levels", "stage01.json")).Replace("\"seed\": 101", "\"seed\": \"abc\"");

            var exception = Assert.Throws<DataFormatException>(() => LevelDefinition.Parse(text, "levels/stage01.json", Loader.LoadRoom));

            Assert.That(exception.Message, Does.Contain("levels/stage01.json.seed"));
        }

        [Test]
        public void Glass_BlocksMovementButNotLineOfSight()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("partition", new Vector3(0, 100, 50), new Vector3(100, 100, 1), ShapeFlags.Glass | ShapeFlags.Attachable));
            var simulation = TestSimulations.WithWorld(world, new Vector3(0, 100, 0));

            TestSimulations.Run(simulation, TestSimulations.Forward, TestSimulations.SecondsToTicks(2f));

            Assert.That(simulation.Player.Position.Z, Is.LessThan(49f), "the mosquito cannot fly through glass");
            Assert.That(world.Raycast(new Vector3(0, 100, 0), Vector3.UnitZ, 200f, ShapeFlags.Obstacle, out _), Is.False, "glass does not block sight");

            var human = TestHumans.Simulation(TestHumans.InFront(150f), world: GlassBetween());
            human.Step(PlayerCommand.None);
            Assert.That(human.CaptureSnapshot().PlayerVisibleToHuman, Is.True, "the human sees through glass");
        }

        private static CollisionWorld GlassBetween()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("glass", TestHumans.InFront(80f), new Vector3(50f, 50f, 1f), ShapeFlags.Glass));
            return world;
        }

        private static float MaxComponent(Vector3 v)
        {
            return System.Math.Max(System.Math.Abs(v.X), System.Math.Max(System.Math.Abs(v.Y), System.Math.Abs(v.Z)));
        }
    }
}
