using Moqui.Core.Collision;
using Moqui.Unity.Presentation;
using Moqui.Unity.Presentation.Sandbox;
using NUnit.Framework;
using UnityEngine;

namespace Moqui.Unity.Tests
{
    public class WorldViewTests
    {
        private const float Tolerance = 1e-3f;

        [Test]
        public void Build_SandboxWorld_OneVisualPerObstacleWithMatchingBounds()
        {
            CollisionWorld world = SandboxFlightWorld.Create();
            var parent = new GameObject("Test");
            try
            {
                Transform root = WorldView.Build(world, parent.transform);

                var obstacles = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(world.Shapes, shape => shape.Matches(ShapeFlags.Obstacle)));
                Assert.That(root.childCount, Is.EqualTo(obstacles.Count));
                Assert.That(root.Find("table_shadow"), Is.Null, "shadow volumes are not drawn");
                foreach (var shape in obstacles)
                {
                    Transform visual = root.Find(shape.Id);
                    Assert.That(visual, Is.Not.Null, shape.Id);
                    Assert.That(visual.GetComponent<Collider>(), Is.Null, $"{shape.Id}: Unity collider must not exist");
                    Assert.That(Vector3.Distance(visual.position, shape.Center.ToUnity()), Is.LessThan(Tolerance), $"{shape.Id}: center");
                    if (shape.Type == ShapeType.Box)
                    {
                        Assert.That(Vector3.Distance(visual.localScale, (shape.HalfExtents * 2f).ToUnity()), Is.LessThan(Tolerance), $"{shape.Id}: size");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void SandboxWorld_PlayerSpawn_IsClearOfObstacles()
        {
            CollisionWorld world = SandboxFlightWorld.Create();

            Assert.That(world.AnyOverlap(SandboxFlightWorld.PlayerSpawn, 1f, ShapeFlags.Obstacle), Is.False);
        }
    }
}
