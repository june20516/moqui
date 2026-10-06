using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Meta;
using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Data
{
    /// <summary>
    /// 방 분위기 조명 (data/rooms "lights", spec/assets/lighting.md, M14): 스테이지에 쓰이는 방은 모두 조명이 있고,
    /// 조명은 방 안에 있으며, 참조하는 형상·텍스처와 조명 스펙 문서가 실제로 있다.
    /// </summary>
    public class RoomLightingTests
    {
        private static readonly string AssetsRoot = Path.Combine(RepoPaths.Data, "..", "spec", "assets");

        private static LevelLoader Loader => new LevelLoader(FileSystemDataSource.ForRepoData());

        /// <summary>스테이지 목록이 쓰는 방 (레벨을 추가하면 자동으로 검사된다).</summary>
        private static IEnumerable<string> UsedRooms()
        {
            var catalog = StageCatalog.Load(FileSystemDataSource.ForRepoData());
            return catalog.LevelIds.Select(id => Loader.Load(id).Room.Id).Distinct();
        }

        [TestCaseSource(nameof(UsedRooms))]
        public void UsedRoom_HasLights_InsideTheRoom_WithValidReferences(string roomId)
        {
            var room = Loader.LoadRoom(roomId);
            var shapeIds = new HashSet<string>(room.Shapes.Select(s => s.Id));
            string index = File.ReadAllText(Path.Combine(AssetsRoot, "INDEX.md"));

            Assert.That(room.Lights, Is.Not.Empty, "every played room needs ambient lights");
            Assert.That(room.Lights.Select(l => l.Id), Is.Unique);
            Vector3 half = room.Size * 0.5f;
            foreach (var light in room.Lights)
            {
                // 창밖 빛은 벽 바로 바깥(창틀 깊이)에서 들어올 수 있으므로 벽 두께만큼 여유를 둔다.
                const float WallMargin = 15f;
                Assert.That(System.MathF.Abs(light.Position.X) <= half.X + WallMargin && light.Position.Y >= 0f && light.Position.Y <= room.Size.Y + WallMargin && System.MathF.Abs(light.Position.Z) <= half.Z + WallMargin, Is.True, $"{light.Id} inside room");
                if (light.GlowShape.Length > 0)
                {
                    Assert.That(shapeIds, Does.Contain(light.GlowShape), $"{light.Id} glowShape");
                }

                if (light.Cookie.Length > 0)
                {
                    Assert.That(index, Does.Contain($"| {light.Cookie} |"), $"{light.Id} cookie texture is in the asset index");
                }
            }
        }

        [TestCaseSource(nameof(UsedRooms))]
        public void EveryRoomLight_IsSpecifiedInLightingDoc(string roomId)
        {
            string doc = File.ReadAllText(Path.Combine(AssetsRoot, "lighting.md"));
            foreach (var light in Loader.LoadRoom(roomId).Lights)
            {
                Assert.That(doc, Does.Contain($"`{AssetId(light.Id)}`"), $"{roomId}/{light.Id}");
            }
        }

        /// <summary>쓰는 방은 환경광 색이 있고, 부위를 따라가는 조명은 그 방을 쓰는 레벨 인간의 실제 부위를 가리킨다 (gulf §13).</summary>
        [Test]
        public void UsedRooms_HaveAmbient_AttachPartsExistOnTheLevelHuman()
        {
            var source = FileSystemDataSource.ForRepoData();
            var settings = Moqui.Core.Simulation.GameSettings.FromTuning(TuningLoader.Load(source));
            foreach (string levelId in StageCatalog.Load(source).LevelIds)
            {
                var level = Loader.Load(levelId);
                Assert.That(level.Room.Ambient, Is.Not.Null, $"{level.Room.Id} ambient");
                var simulation = new Moqui.Core.Simulation.GameSimulation(settings, level.CreateSetup());
                foreach (var light in level.Room.Lights.Where(l => l.AttachPart.Length > 0))
                {
                    Assert.That(simulation.Human.Shapes.ContainsKey(light.AttachPart), Is.True, $"{levelId}: {light.Id} follows {light.AttachPart}");
                }
            }
        }

        [Test]
        public void SpotWithoutDirection_IsRejected()
        {
            const string Text = "{\"formatVersion\": 1, \"id\": \"x\", \"size\": [10, 10, 10], \"shapes\": [], " +
                "\"lights\": [{\"id\": \"a\", \"type\": \"spot\", \"position\": [0, 1, 0], \"direction\": [0, 0, 0], \"color\": [1, 1, 1], \"intensity\": 1, \"range\": 5}]}";

            Assert.Throws<DataFormatException>(() => RoomDefinition.Parse(Text, "rooms/x.json"));
        }

        [Test]
        public void RoomWithoutLights_ParsesToEmptyList()
        {
            Assert.That(Loader.LoadRoom("livingRoom").Lights, Is.Empty);
        }

        /// <summary>데이터 ID → 애셋 ID: tv_screen → light-tv-screen.</summary>
        public static string AssetId(string lightId)
        {
            return "light-" + lightId.Replace('_', '-');
        }
    }
}
