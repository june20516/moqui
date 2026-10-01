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

        public static HumanDefinition Seated(float facingYaw = 0f, float[] idleLookYaws = null)
        {
            var parts = new[]
            {
                new BodyPartDefinition("head", BodyPartKind.Head, new Vector3(0, HeadHeight - 3f, 0), new Vector3(0, HeadHeight + 3f, 0), HeadRadius, true),
                new BodyPartDefinition("torso", BodyPartKind.Torso, new Vector3(0, 55, -5), new Vector3(0, 90, -5), 16f, false),
                new BodyPartDefinition("upperArmL", BodyPartKind.UpperArm, new Vector3(-ShoulderHalfWidth, ShoulderHeight, -5), new Vector3(-24, 65, 0), 5f, false),
                new BodyPartDefinition("forearmL", BodyPartKind.Forearm, new Vector3(-24, 65, 0), new Vector3(-20, 60, 28), 4f, true),
                new BodyPartDefinition("upperArmR", BodyPartKind.UpperArm, new Vector3(ShoulderHalfWidth, ShoulderHeight, -5), new Vector3(24, 65, 0), 5f, false),
                new BodyPartDefinition("forearmR", BodyPartKind.Forearm, new Vector3(24, 65, 0), new Vector3(20, 60, 28), 4f, true),
                new BodyPartDefinition("thighL", BodyPartKind.Thigh, new Vector3(-10, 48, 0), new Vector3(-10, 48, 40), 7f, false),
                new BodyPartDefinition("calfL", BodyPartKind.Calf, new Vector3(-10, 48, 40), new Vector3(-10, 5, 45), 5f, true),
                new BodyPartDefinition("thighR", BodyPartKind.Thigh, new Vector3(10, 48, 0), new Vector3(10, 48, 40), 7f, false),
                new BodyPartDefinition("calfR", BodyPartKind.Calf, new Vector3(10, 48, 40), new Vector3(10, 5, 45), 5f, true),
            };
            var shoulders = new[] { new Vector3(-ShoulderHalfWidth, ShoulderHeight, -5), new Vector3(ShoulderHalfWidth, ShoulderHeight, -5) };
            return new HumanDefinition("human", Vector3.Zero, facingYaw, parts, "head", shoulders, idleLookYaws ?? new[] { 0f });
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
