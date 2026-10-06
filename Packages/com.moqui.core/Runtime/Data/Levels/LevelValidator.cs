using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;

namespace Moqui.Core.Data.Levels
{
    /// <summary>
    /// spec/07 공통 규칙의 레벨 데이터 검사. 위반 목록을 돌려준다 (비어 있으면 통과).
    /// 경로 존재("시작 위치에서 각 SkinSite까지 숨을 곳을 경유하는 경로")는 클리어 봇으로 증명한다 (tech/verification.md §3).
    /// </summary>
    public static class LevelValidator
    {
        private const ShapeFlags VolumeFlags = ShapeFlags.ShadowZone | ShapeFlags.HumidWeak | ShapeFlags.HumidStrong | ShapeFlags.Wind | ShapeFlags.Hazard;
        private const int CueSampleRings = 3;
        private const int CueSamplesPerRing = 16;

        public static IReadOnlyList<string> Validate(LevelDefinition level, GameSettings settings)
        {
            var errors = new List<string>();
            CheckShapeFlags(level, errors);

            var simulation = new GameSimulation(settings, level.CreateSetup());
            var world = simulation.World;
            var human = simulation.Human;
            CheckSpawn(level, settings, world, human, errors);
            CheckDripSources(level, settings, world, errors);
            CheckHumanData(level, errors);
            CheckEscapeCover(settings, world, human, errors);
            if (human != null)
            {
                errors.AddRange(WalkRouteErrors(human, world, settings.Walk));
            }

            return errors;
        }

        /// <summary>
        /// 걷는 인간의 경로 (spec/02 §9): 경로점과 경로점 사이 구간에서 골반 높이 구(human.walkRadius)가 가구와 겹치지 않아야 한다.
        /// </summary>
        public static IEnumerable<string> WalkRouteErrors(Human human, CollisionWorld world, WalkSettings walk)
        {
            var route = human.Definition.Walk?.Route;
            if (route == null)
            {
                yield break;
            }

            float height = human.RootPosition.Y;
            var hits = new List<CollisionShape>();
            for (int i = 0; i < route.Count; i++)
            {
                var point = new Vector3(route[i].X, height, route[i].Y);
                hits.Clear();
                world.Overlap(point, walk.Radius, ShapeFlags.Obstacle, hits);
                if (hits.Exists(shape => !human.Owns(shape)))
                {
                    yield return $"{human.Id}: walk point {i} overlaps furniture";
                }

                Vector3 from = i == 0 ? new Vector3(human.RootPosition.X, height, human.RootPosition.Z) : new Vector3(route[i - 1].X, height, route[i - 1].Y);
                Vector3 delta = point - from;
                float distance = delta.Length();
                if (distance > 1e-3f && world.SphereSweep(from, walk.Radius, delta / distance, distance, ShapeFlags.Obstacle, out _, ShapeFlags.Body))
                {
                    yield return $"{human.Id}: walk segment to point {i} is blocked";
                }
            }
        }

        /// <summary>정적 가구 형상은 obstacle 또는 glass 플래그를 가져야 한다 (spec/03, spec/07).</summary>
        private static void CheckShapeFlags(LevelDefinition level, List<string> errors)
        {
            foreach (var shape in level.AllShapes())
            {
                bool isVolume = (shape.Flags & VolumeFlags) != 0;
                bool isSolid = (shape.Flags & ShapeFlags.Solid) != 0;
                if (!isVolume && !isSolid)
                {
                    errors.Add($"{level.Id}: shape '{shape.Id}' is furniture without obstacle/glass flag");
                }
            }
        }

        /// <summary>시작 위치: 장애물과 겹치지 않고, 인간의 Yellow Zone과 비행 소음 반경 밖이다.</summary>
        private static void CheckSpawn(LevelDefinition level, GameSettings settings, CollisionWorld world, Human human, List<string> errors)
        {
            Vector3 spawn = level.PlayerSpawn;
            if (world.AnyOverlap(spawn, settings.Player.CollisionRadius, ShapeFlags.Solid))
            {
                errors.Add($"{level.Id}: player spawn overlaps a solid shape");
            }

            if (human == null)
            {
                return;
            }

            if (human.DistanceToNearestEar(spawn) < settings.Noise.FlightRadius)
            {
                errors.Add($"{level.Id}: player spawn is inside the flight noise radius");
            }

            foreach (float yaw in GazeYaws(human))
            {
                human.HeadYaw = yaw;
                human.HeadPitch = human.Definition.RestPitch;
                Vector3 toSpawn = spawn - human.HeadCenter;
                float angle = AngleDegrees(human.HeadForward, toSpawn);
                if (toSpawn.Length() <= settings.Vision.YellowRange && angle <= settings.Vision.YellowHalfAngle)
                {
                    errors.Add($"{level.Id}: player spawn is inside the Yellow Zone (head yaw {yaw})");
                }
            }

            human.HeadYaw = 0f;
        }

        private static void CheckDripSources(LevelDefinition level, GameSettings settings, CollisionWorld world, List<string> errors)
        {
            foreach (var source in level.DripSources)
            {
                if (!LevelChecks.DripSourceHighEnough(world, source, settings.Water))
                {
                    errors.Add($"{level.Id}: drip source {source} is lower than {settings.Water.MinSourceHeight}u above its landing surface");
                }
            }
        }

        private static void CheckHumanData(LevelDefinition level, List<string> errors)
        {
            var human = level.Human;
            if (human == null)
            {
                errors.Add($"{level.Id}: level has no human");
                return;
            }

            bool hasSite = false;
            foreach (var part in human.Parts)
            {
                hasSite |= part.IsSkin;
            }

            if (!hasSite)
            {
                errors.Add($"{level.Id}: human has no skin site");
            }

            if (human.Actions.Count == 0)
            {
                errors.Add($"{level.Id}: human has no random actions");
            }
        }

        /// <summary>각 SkinSite에서 hiding.cueRange 안에 Shadow Zone 또는 인간 눈에서 가려지는 지점이 있다 (도망칠 곳 보장).</summary>
        private static void CheckEscapeCover(GameSettings settings, CollisionWorld world, Human human, List<string> errors)
        {
            if (human == null)
            {
                return;
            }

            float range = settings.Hiding.CueRange;
            foreach (var site in human.SkinSites)
            {
                Vector3 center = site.Shape.Center;
                if (!HasShadowZoneWithin(world, center, range) && !HasHiddenPointWithin(world, human, center, range, settings.Player.CollisionRadius))
                {
                    errors.Add($"{human.Id}.{site.PartId}: no Shadow Zone or line-of-sight cover within {range}u");
                }
            }
        }

        private static bool HasShadowZoneWithin(CollisionWorld world, Vector3 point, float range)
        {
            foreach (var shape in world.Shapes)
            {
                if (shape.Matches(ShapeFlags.ShadowZone) && ShapeGeometry.Closest(shape, point).Distance <= range)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>site 주변 구 껍질 위의 점 중, 비어 있고 인간 눈에서 장애물에 가려지는 점이 있는가.</summary>
        private static bool HasHiddenPointWithin(CollisionWorld world, Human human, Vector3 site, float range, float playerRadius)
        {
            Vector3 eye = human.HeadCenter;
            for (int ring = 1; ring <= CueSampleRings; ring++)
            {
                float radius = range * ring / CueSampleRings;
                for (int i = 0; i < CueSamplesPerRing; i++)
                {
                    float azimuth = MathF.PI * 2f * i / CueSamplesPerRing;
                    for (int elevation = -1; elevation <= 1; elevation++)
                    {
                        float tilt = elevation * MathF.PI / 6f;
                        var direction = new Vector3(MathF.Cos(azimuth) * MathF.Cos(tilt), MathF.Sin(tilt), MathF.Sin(azimuth) * MathF.Cos(tilt));
                        Vector3 point = site + (direction * radius);
                        if (world.AnyOverlap(point, playerRadius, ShapeFlags.Solid))
                        {
                            continue;
                        }

                        Vector3 toPoint = point - eye;
                        if (world.Raycast(eye, toPoint, toPoint.Length(), ShapeFlags.Obstacle, out _, ShapeFlags.Body))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static IEnumerable<float> GazeYaws(Human human)
        {
            foreach (float yaw in human.Definition.IdleLookYaws)
            {
                yield return yaw;
            }

            var glance = human.Definition.Traits.Glance;
            if (glance != null)
            {
                yield return glance.Angle;
                yield return -glance.Angle;
            }
        }

        private static float AngleDegrees(Vector3 a, Vector3 b)
        {
            float lengths = a.Length() * b.Length();
            return lengths <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector3.Dot(a, b) / lengths, -1f, 1f)) * 180f / MathF.PI;
        }
    }
}
