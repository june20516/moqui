using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Unity.Presentation.Sandbox
{
    /// <summary>
    /// Sandbox_Flight용 화이트박스 방. 단위는 cm(spec/00): 바닥 500 × 400, 높이 250. 바닥 중심이 원점이다.
    /// 레벨 데이터 포맷(M7) 전까지 비행·카메라를 눈으로 확인하는 용도로만 쓴다.
    /// </summary>
    public static class SandboxFlightWorld
    {
        public const float RoomWidth = 500f;
        public const float RoomHeight = 250f;
        public const float RoomDepth = 400f;
        public const float WallThickness = 10f;

        public static readonly Vector3 PlayerSpawn = new Vector3(0f, 100f, -120f);

        public static CollisionWorld Create()
        {
            var world = new CollisionWorld();
            float halfWidth = RoomWidth * 0.5f;
            float halfDepth = RoomDepth * 0.5f;
            float halfThickness = WallThickness * 0.5f;
            const ShapeFlags Wall = ShapeFlags.Obstacle | ShapeFlags.Attachable;

            world.Add(CollisionShape.Box("floor", new Vector3(0f, -halfThickness, 0f), new Vector3(halfWidth, halfThickness, halfDepth), Wall));
            world.Add(CollisionShape.Box("ceiling", new Vector3(0f, RoomHeight + halfThickness, 0f), new Vector3(halfWidth, halfThickness, halfDepth), Wall));
            world.Add(CollisionShape.Box("wall_front", new Vector3(0f, RoomHeight * 0.5f, halfDepth + halfThickness), new Vector3(halfWidth, RoomHeight * 0.5f, halfThickness), Wall));
            world.Add(CollisionShape.Box("wall_back", new Vector3(0f, RoomHeight * 0.5f, -halfDepth - halfThickness), new Vector3(halfWidth, RoomHeight * 0.5f, halfThickness), Wall));
            world.Add(CollisionShape.Box("wall_left", new Vector3(-halfWidth - halfThickness, RoomHeight * 0.5f, 0f), new Vector3(halfThickness, RoomHeight * 0.5f, halfDepth), Wall));
            world.Add(CollisionShape.Box("wall_right", new Vector3(halfWidth + halfThickness, RoomHeight * 0.5f, 0f), new Vector3(halfThickness, RoomHeight * 0.5f, halfDepth), Wall));

            world.Add(CollisionShape.Box("table_top", new Vector3(-80f, 72f, 40f), new Vector3(60f, 2f, 40f), Wall));
            world.Add(CollisionShape.Box("table_leg_a", new Vector3(-135f, 35f, 5f), new Vector3(3f, 35f, 3f), Wall));
            world.Add(CollisionShape.Box("table_leg_b", new Vector3(-25f, 35f, 5f), new Vector3(3f, 35f, 3f), Wall));
            world.Add(CollisionShape.Box("table_leg_c", new Vector3(-135f, 35f, 75f), new Vector3(3f, 35f, 3f), Wall));
            world.Add(CollisionShape.Box("table_leg_d", new Vector3(-25f, 35f, 75f), new Vector3(3f, 35f, 3f), Wall));
            world.Add(CollisionShape.Box("sofa", new Vector3(150f, 22f, 120f), new Vector3(80f, 22f, 40f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, -0.3f), Wall));
            world.Add(CollisionShape.Sphere("lamp_shade", new Vector3(200f, 150f, -150f), 25f, Wall));
            world.Add(CollisionShape.Capsule("lamp_pole", new Vector3(200f, 0f, -150f), new Vector3(200f, 125f, -150f), 2f, Wall));
            return world;
        }
    }
}
