using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;
using NUnit.Framework;

namespace Moqui.Core.Tests.Collision
{
    public class CollisionWorldTests
    {
        private const float Tolerance = 1e-3f;

        [Test]
        public void Raycast_BoxFace_ReturnsDistanceAndNormal()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(10, 0, 0), new Vector3(1, 5, 5), ShapeFlags.Obstacle));

            bool found = world.Raycast(Vector3.Zero, Vector3.UnitX, 100f, ShapeFlags.Obstacle, out var hit);

            Assert.That(found, Is.True);
            Assert.That(hit.Distance, Is.EqualTo(9f).Within(Tolerance));
            AssertVector(hit.Normal, -Vector3.UnitX);
            AssertVector(hit.Point, new Vector3(9, 0, 0));
        }

        [Test]
        public void Raycast_MaskExcludesShape_Misses()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("zone", new Vector3(10, 0, 0), new Vector3(1, 5, 5), ShapeFlags.ShadowZone));

            Assert.That(world.Raycast(Vector3.Zero, Vector3.UnitX, 100f, ShapeFlags.Obstacle, out _), Is.False);
        }

        [Test]
        public void Raycast_BeyondMaxDistance_Misses()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Sphere("ball", new Vector3(10, 0, 0), 1f, ShapeFlags.Obstacle));

            Assert.That(world.Raycast(Vector3.Zero, Vector3.UnitX, 8.5f, ShapeFlags.Obstacle, out _), Is.False);
        }

        [Test]
        public void Raycast_MultipleShapes_ReturnsNearest()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Sphere("far", new Vector3(20, 0, 0), 1f, ShapeFlags.Obstacle));
            world.Add(CollisionShape.Sphere("near", new Vector3(10, 0, 0), 1f, ShapeFlags.Obstacle));

            world.Raycast(Vector3.Zero, Vector3.UnitX, 100f, ShapeFlags.Obstacle, out var hit);

            Assert.That(hit.Shape.Id, Is.EqualTo("near"));
            Assert.That(hit.Distance, Is.EqualTo(9f).Within(Tolerance));
        }

        [Test]
        public void Raycast_RotatedBox_HitsRotatedFace()
        {
            var world = new CollisionWorld();
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4f);
            world.Add(CollisionShape.Box("diamond", new Vector3(10, 0, 0), new Vector3(1, 1, 1), rotation, ShapeFlags.Obstacle));

            world.Raycast(Vector3.Zero, Vector3.UnitX, 100f, ShapeFlags.Obstacle, out var hit);

            // 45° 회전한 정육면체는 모서리가 원점 쪽을 향하므로 중심에서 √2 앞에서 닿는다.
            Assert.That(hit.Distance, Is.EqualTo(10f - MathF.Sqrt(2f)).Within(Tolerance));
        }

        [Test]
        public void SphereSweep_BoxFace_StopsRadiusBeforeFace()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(10, 0, 0), new Vector3(1, 5, 5), ShapeFlags.Obstacle));

            world.SphereSweep(Vector3.Zero, 0.4f, Vector3.UnitX, 100f, ShapeFlags.Obstacle, out var hit);

            Assert.That(hit.Distance, Is.EqualTo(8.6f).Within(Tolerance));
            AssertVector(hit.Normal, -Vector3.UnitX);
        }

        [Test]
        public void SphereSweep_BoxEdge_HitsRoundedEdge()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("box", Vector3.Zero, new Vector3(1, 1, 1), ShapeFlags.Obstacle));
            Vector3 direction = Vector3.Normalize(new Vector3(-1, -1, 0));

            world.SphereSweep(new Vector3(3, 3, 0), 0.5f, direction, 100f, ShapeFlags.Obstacle, out var hit);

            float expected = (3f - 1f - (0.5f / MathF.Sqrt(2f))) * MathF.Sqrt(2f);
            Assert.That(hit.Distance, Is.EqualTo(expected).Within(Tolerance));
            AssertVector(hit.Normal, Vector3.Normalize(new Vector3(1, 1, 0)));
            AssertVector(hit.Point, new Vector3(1, 1, 0));
        }

        [Test]
        public void SphereSweep_Capsule_StopsAtCombinedRadius()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Capsule("arm", new Vector3(10, -5, 0), new Vector3(10, 5, 0), 2f, ShapeFlags.Obstacle));

            world.SphereSweep(Vector3.Zero, 0.5f, Vector3.UnitX, 100f, ShapeFlags.Obstacle, out var hit);

            Assert.That(hit.Distance, Is.EqualTo(7.5f).Within(Tolerance));
            AssertVector(hit.Normal, -Vector3.UnitX);
        }

        [Test]
        public void SphereSweep_CapsuleEndCap_HitsHemisphere()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Capsule("arm", new Vector3(0, 0, 10), new Vector3(0, 0, 20), 2f, ShapeFlags.Obstacle));

            world.SphereSweep(Vector3.Zero, 0.5f, Vector3.UnitZ, 100f, ShapeFlags.Obstacle, out var hit);

            Assert.That(hit.Distance, Is.EqualTo(7.5f).Within(Tolerance));
            AssertVector(hit.Normal, -Vector3.UnitZ);
        }

        [Test]
        public void SphereSweep_StartsOverlapping_ReportsStartedInside()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("wall", new Vector3(1, 0, 0), new Vector3(1, 5, 5), ShapeFlags.Obstacle));

            world.SphereSweep(new Vector3(-0.2f, 0, 0), 0.4f, Vector3.UnitX, 10f, ShapeFlags.Obstacle, out var hit);

            Assert.That(hit.StartedInside, Is.True);
            Assert.That(hit.Distance, Is.EqualTo(0f));
            AssertVector(hit.Normal, -Vector3.UnitX);
        }

        [Test]
        public void Overlap_ReturnsOnlyIntersectingShapes()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Sphere("touching", new Vector3(1.4f, 0, 0), 1f, ShapeFlags.Obstacle));
            world.Add(CollisionShape.Sphere("apart", new Vector3(3f, 0, 0), 1f, ShapeFlags.Obstacle));
            world.Add(CollisionShape.Box("zone", Vector3.Zero, new Vector3(5, 5, 5), ShapeFlags.ShadowZone));
            var results = new List<CollisionShape>();

            int count = world.Overlap(Vector3.Zero, 0.5f, ShapeFlags.Obstacle, results);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo("touching"));
        }

        [Test]
        public void ClosestSurface_OutsideBox_ReturnsDistanceAndNormal()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("table", new Vector3(0, -1, 0), new Vector3(5, 1, 5), ShapeFlags.Attachable));

            bool found = world.ClosestSurface(new Vector3(1, 1.5f, 2), 2f, ShapeFlags.Attachable, out var surface);

            Assert.That(found, Is.True);
            Assert.That(surface.Distance, Is.EqualTo(1.5f).Within(Tolerance));
            AssertVector(surface.Normal, Vector3.UnitY);
            AssertVector(surface.Point, new Vector3(1, 0, 2));
        }

        [Test]
        public void ClosestSurface_InsideBox_ReturnsNegativeDistance()
        {
            var shape = CollisionShape.Box("box", Vector3.Zero, new Vector3(2, 2, 2), ShapeFlags.Obstacle);

            var surface = ShapeGeometry.Closest(shape, new Vector3(1.5f, 0, 0));

            Assert.That(surface.Distance, Is.EqualTo(-0.5f).Within(Tolerance));
            AssertVector(surface.Normal, Vector3.UnitX);
        }

        [Test]
        public void ClosestSurface_BeyondMaxDistance_NotFound()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Sphere("ball", Vector3.Zero, 1f, ShapeFlags.Attachable));

            Assert.That(world.ClosestSurface(new Vector3(5, 0, 0), 2f, ShapeFlags.Attachable, out _), Is.False);
        }

        [Test]
        public void Add_DuplicateId_Throws()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Sphere("a", Vector3.Zero, 1f, ShapeFlags.Obstacle));

            Assert.Throws<ArgumentException>(() => world.Add(CollisionShape.Sphere("a", Vector3.One, 1f, ShapeFlags.Obstacle)));
        }

        [Test]
        public void Remove_ExistingShape_NoLongerHit()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Sphere("ball", new Vector3(5, 0, 0), 1f, ShapeFlags.Obstacle));

            world.Remove("ball");

            Assert.That(world.Raycast(Vector3.Zero, Vector3.UnitX, 100f, ShapeFlags.All, out _), Is.False);
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance), $"expected {expected} but was {actual}");
        }
    }
}
