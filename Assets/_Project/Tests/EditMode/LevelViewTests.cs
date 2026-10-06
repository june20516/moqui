using System.Linq;
using Moqui.Core.Collision;
using Moqui.Core.Data.Levels;
using Moqui.Unity.Data;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Stage;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    /// <summary>레벨 데이터의 형상 ID와 시각 오브젝트가 1:1인지 확인한다 (spec/07).</summary>
    public class LevelViewTests
    {
        private const float Tolerance = 1e-3f;
        [TestCaseSource(typeof(StageCatalogs), nameof(StageCatalogs.LevelIdList))]
        public void Build_Level_OneVisualPerShapeIdWithMatchingPose(string levelId)
        {
            LevelDefinition level = new LevelLoader(new UnityDataSource()).Load(levelId);
            CollisionWorld world = level.CreateWorld();
            var parent = new GameObject("Test");
            try
            {
                var objects = LevelView.Build(level, world, parent.transform, null);

                var expectedIds = level.AllShapes().Select(shape => shape.Id).ToList();
                Assert.That(objects.Keys, Is.EquivalentTo(expectedIds));
                Transform root = parent.transform.GetChild(0);
                Assert.That(root.childCount, Is.EqualTo(expectedIds.Count), "no extra visuals");
                foreach (string id in expectedIds)
                {
                    world.TryGet(id, out var shape);
                    Transform visual = objects[id].transform;
                    Assert.That(visual.name, Is.EqualTo(id));
                    Assert.That(visual.GetComponent<Collider>(), Is.Null, $"{id}: Unity collider must not exist");
                    Assert.That(Vector3.Distance(visual.position, shape.Center.ToUnity()), Is.LessThan(Tolerance), $"{id}: center");
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
