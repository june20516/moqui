using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;

namespace Moqui.Unity.Presentation.Sandbox
{
    /// <summary>
    /// Sandbox_Human: 화이트박스 방 + 소파에 앉아 +Z를 보는 캡슐 인간 (M2 확인용).
    /// 인간 정의는 레벨 데이터 포맷(M7) 전까지 여기 코드로 둔다. 단위 cm.
    /// </summary>
    public static class SandboxHumanWorld
    {
        public const ulong Seed = 20261001UL;

        public static readonly Vector3 HumanPosition = new Vector3(0f, 0f, -100f);

        /// <summary>인간 뒤 오른쪽: 시야·비행 소음 밖.</summary>
        public static readonly Vector3 PlayerSpawn = new Vector3(150f, 150f, -180f);

        public static SimulationSetup CreateSetup()
        {
            return new SimulationSetup(CreateWorld, PlayerSpawn, CreateHuman(), Seed);
        }

        public static CollisionWorld CreateWorld()
        {
            var world = SandboxFlightWorld.Create();
            const ShapeFlags Furniture = ShapeFlags.Obstacle | ShapeFlags.Attachable;
            world.Add(CollisionShape.Box("human_sofa_seat", HumanPosition + new Vector3(0f, 20f, 5f), new Vector3(70f, 20f, 35f), Furniture));
            world.Add(CollisionShape.Box("human_sofa_back", HumanPosition + new Vector3(0f, 60f, -38f), new Vector3(70f, 40f, 8f), Furniture));
            return world;
        }

        public static HumanDefinition CreateHuman()
        {
            const float headHeight = 115f;
            const float shoulderHeight = 95f;
            const float shoulderHalfWidth = 20f;
            var parts = new[]
            {
                new BodyPartDefinition("head", BodyPartKind.Head, new Vector3(0, headHeight - 3f, 0), new Vector3(0, headHeight + 3f, 0), 10f, SkinSiteType.Cheek),
                new BodyPartDefinition("neck", BodyPartKind.Neck, new Vector3(0, 96, -3), new Vector3(0, 104, -2), 5f, SkinSiteType.Neck),
                new BodyPartDefinition("torso", BodyPartKind.Torso, new Vector3(0, 55, -8), new Vector3(0, 88, -6), 16f, null),
                new BodyPartDefinition("upperArmL", BodyPartKind.UpperArm, new Vector3(-shoulderHalfWidth, shoulderHeight, -6), new Vector3(-24, 66, -2), 5f, null),
                new BodyPartDefinition("forearmL", BodyPartKind.Forearm, new Vector3(-24, 66, -2), new Vector3(-20, 60, 26), 4f, SkinSiteType.Forearm),
                new BodyPartDefinition("upperArmR", BodyPartKind.UpperArm, new Vector3(shoulderHalfWidth, shoulderHeight, -6), new Vector3(24, 66, -2), 5f, null),
                new BodyPartDefinition("forearmR", BodyPartKind.Forearm, new Vector3(24, 66, -2), new Vector3(20, 60, 26), 4f, SkinSiteType.Forearm),
                new BodyPartDefinition("thighL", BodyPartKind.Thigh, new Vector3(-10, 48, 0), new Vector3(-10, 48, 42), 7f, null),
                new BodyPartDefinition("calfL", BodyPartKind.Calf, new Vector3(-10, 46, 44), new Vector3(-10, 8, 48), 5f, SkinSiteType.Calf),
                new BodyPartDefinition("thighR", BodyPartKind.Thigh, new Vector3(10, 48, 0), new Vector3(10, 48, 42), 7f, null),
                new BodyPartDefinition("calfR", BodyPartKind.Calf, new Vector3(10, 46, 44), new Vector3(10, 8, 48), 5f, SkinSiteType.Calf),
            };
            var shoulders = new[] { new Vector3(-shoulderHalfWidth, shoulderHeight, -6), new Vector3(shoulderHalfWidth, shoulderHeight, -6) };
            return new HumanDefinition("human", HumanPosition, 0f, parts, "head", shoulders, new[] { 0f, 35f, 0f, -35f }, CreateActions());
        }

        /// <summary>spec/07 거실 예시 동작 (휴대폰 스크롤, 자세 고치기, 다리 떨기, 휴지 뽑기). 큰 동작은 튕겨남을 일으킨다.</summary>
        public static HumanActionDefinition[] CreateActions()
        {
            return new[]
            {
                new HumanActionDefinition("phoneScroll", 4f, 2f, new[]
                {
                    new PartMotionDefinition("forearmR", Vector3.Zero, new Vector3(0f, 3f, 0f)),
                }),
                new HumanActionDefinition("shiftPosture", 2f, 1.5f, new[]
                {
                    new PartMotionDefinition("torso", new Vector3(0f, 0f, 2f), new Vector3(3f, 2f, 4f)),
                    new PartMotionDefinition("neck", new Vector3(3f, 2f, 4f), new Vector3(3f, 2f, 4f)),
                    new PartMotionDefinition("head", new Vector3(3f, 2f, 4f), new Vector3(3f, 2f, 4f)),
                }),
                new HumanActionDefinition("legBounce", 2f, 0.6f, new[]
                {
                    new PartMotionDefinition("calfL", Vector3.Zero, new Vector3(0f, 0f, 14f)),
                }),
                new HumanActionDefinition("grabTissue", 1f, 0.6f, new[]
                {
                    new PartMotionDefinition("upperArmR", Vector3.Zero, new Vector3(8f, 0f, 8f)),
                    new PartMotionDefinition("forearmR", new Vector3(8f, 0f, 8f), new Vector3(25f, 10f, 15f)),
                }),
            };
        }
    }
}
