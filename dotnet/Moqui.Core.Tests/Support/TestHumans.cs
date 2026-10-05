using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;

namespace Moqui.Core.Tests.Support
{
    /// <summary>테스트용 앉은 인간. 원점에 앉아 +Z(yaw 0)를 본다. 머리 중심 높이 HeadHeight, 단위 cm.</summary>
    public static class TestHumans
    {
        public const float HeadHeight = 115f;
        public const float HeadRadius = 10f;
        public const float ShoulderHeight = 95f;
        public const float ShoulderHalfWidth = 20f;
        public const ulong DefaultSeed = 42UL;

        public static Vector3 Head => new Vector3(0f, HeadHeight, 0f);

        /// <summary>느린 동작: 오른쪽 팔뚝 끝을 3u 들었다 내림 (최고 속도 약 4.7u/s, 튕겨남 없음).</summary>
        public static HumanActionDefinition Scroll => new HumanActionDefinition("scroll", 1f, 2f, new[]
        {
            new PartMotionDefinition("forearmR", Vector3.Zero, new Vector3(0f, 3f, 0f)),
        });

        /// <summary>빠른 동작: 오른팔을 크게 뻗음 (팔뚝 끝 최고 속도 약 190u/s, 튕겨남).</summary>
        public static HumanActionDefinition GrabTissue => new HumanActionDefinition("grabTissue", 1f, 0.5f, new[]
        {
            new PartMotionDefinition("upperArmR", Vector3.Zero, new Vector3(8f, 0f, 8f)),
            new PartMotionDefinition("forearmR", new Vector3(8f, 0f, 8f), new Vector3(25f, 10f, 15f)),
        });

        /// <summary>확률 반응과 흡혈 중 이벤트를 끈다 (움직임만 검증할 때). 스킬 배율과 같은 공개 배율을 0으로 둔다.</summary>
        public static void DisableReactions(GameSimulation simulation)
        {
            simulation.HumanSystem.Reactions.ExtraMultiplier = 0f;
            simulation.HumanSystem.Reactions.LandingSkillMultiplier = 0f;
            simulation.HumanSystem.SuckEvents.Enabled = false;
        }

        public static HumanDefinition Seated(float facingYaw = 0f, float[] idleLookYaws = null, HumanActionDefinition[] actions = null, HumanTraits traits = null)
        {
            var parts = new[]
            {
                new BodyPartDefinition("head", BodyPartKind.Head, new Vector3(0, HeadHeight - 3f, 0), new Vector3(0, HeadHeight + 3f, 0), HeadRadius, SkinSiteType.Cheek),
                new BodyPartDefinition("torso", BodyPartKind.Torso, new Vector3(0, 55, -5), new Vector3(0, 90, -5), 16f, null),
                new BodyPartDefinition("upperArmL", BodyPartKind.UpperArm, new Vector3(-ShoulderHalfWidth, ShoulderHeight, -5), new Vector3(-24, 65, 0), 5f, null),
                new BodyPartDefinition("forearmL", BodyPartKind.Forearm, new Vector3(-24, 65, 0), new Vector3(-20, 60, 28), 4f, SkinSiteType.Forearm),
                new BodyPartDefinition("upperArmR", BodyPartKind.UpperArm, new Vector3(ShoulderHalfWidth, ShoulderHeight, -5), new Vector3(24, 65, 0), 5f, null),
                new BodyPartDefinition("forearmR", BodyPartKind.Forearm, new Vector3(24, 65, 0), new Vector3(20, 60, 28), 4f, SkinSiteType.Forearm),
                new BodyPartDefinition("thighL", BodyPartKind.Thigh, new Vector3(-10, 48, 0), new Vector3(-10, 48, 40), 7f, null),
                new BodyPartDefinition("calfL", BodyPartKind.Calf, new Vector3(-10, 48, 40), new Vector3(-10, 5, 45), 5f, SkinSiteType.Calf),
                new BodyPartDefinition("thighR", BodyPartKind.Thigh, new Vector3(10, 48, 0), new Vector3(10, 48, 40), 7f, null),
                new BodyPartDefinition("calfR", BodyPartKind.Calf, new Vector3(10, 48, 40), new Vector3(10, 5, 45), 5f, SkinSiteType.Calf),
            };
            var shoulders = new[] { new Vector3(-ShoulderHalfWidth, ShoulderHeight, -5), new Vector3(ShoulderHalfWidth, ShoulderHeight, -5) };
            return new HumanDefinition("human", Vector3.Zero, facingYaw, parts, "head", shoulders, idleLookYaws ?? new[] { 0f }, actions, traits);
        }

        public static GameSimulation Simulation(Vector3 playerSpawn, CollisionWorld world = null, HumanDefinition human = null, ulong seed = DefaultSeed)
        {
            var setup = new SimulationSetup(world ?? new CollisionWorld(), playerSpawn, human ?? Seated(), seed);
            return new GameSimulation(TestSimulations.Settings, setup);
        }

        /// <summary>
        /// 경계를 직접 설정하고 "방금 자극을 받음"으로 표시한다. 표시하지 않으면 같은 틱에 자극 없음 감소가 먼저 적용된다.
        /// </summary>
        public static void Provoke(GameSimulation simulation, float awareness)
        {
            simulation.Human.Awareness = awareness;
            simulation.Human.LastStimulusTick = simulation.Tick;
        }

        /// <summary>몸 부위 표면 바깥 1u(부착 거리 안)에 플레이어를 놓는다. 부위 정면 바깥쪽(+X 또는 −X)을 고른다.</summary>
        public static Vector3 PlaceNearPart(GameSimulation simulation, string partId)
        {
            var shape = simulation.Human.Shapes[partId];
            Vector3 outward = shape.Center.X >= 0f ? Vector3.UnitX : -Vector3.UnitX;
            var surface = ShapeGeometry.Closest(shape, shape.Center + (outward * 50f));
            simulation.Player.Position = surface.Point + (surface.Normal * 1f);
            simulation.Player.Velocity = Vector3.Zero;
            return surface.Point;
        }

        public static SkinSiteState Site(GameSimulation simulation, string partId)
        {
            simulation.Human.TryGetSite(simulation.Human.Shapes[partId], out var site);
            return site;
        }

        /// <summary>시야에 걸리지 않는 위쪽 대시 (좌우·상하 입력 없음 → 위, spec/01).</summary>
        public static PlayerCommand DashUp => new PlayerCommand { DashPressed = true };

        /// <summary>인간 정면(+Z) 머리 높이에서 distance만큼 떨어진 점.</summary>
        public static Vector3 InFront(float distance)
        {
            return Head + new Vector3(0f, 0f, distance);
        }

        /// <summary>인간 뒤쪽 멀리: 시야·비행 소음 밖.</summary>
        public static Vector3 FarBehind => Head + new Vector3(0f, 0f, -400f);
    }
}
