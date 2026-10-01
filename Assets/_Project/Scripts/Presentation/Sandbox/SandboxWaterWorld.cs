using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;

namespace Moqui.Unity.Presentation.Sandbox
{
    /// <summary>
    /// Sandbox_Water: 화이트박스 방 + 세면대 위 물방울 발생원, 샤워 칸 증기(강한 습기)와 축축한 구석(약한 습기). 단위 cm.
    /// 물 요소를 눈으로 확인하는 용도 (M6). 레벨 데이터 포맷(M7) 전까지 코드로 둔다.
    /// </summary>
    public static class SandboxWaterWorld
    {
        public static readonly Vector3 DripSource = new Vector3(0f, 230f, 60f);

        /// <summary>발생원 바로 아래, 세면대 위 공중.</summary>
        public static readonly Vector3 UnderDrip = new Vector3(0f, 140f, 60f);

        public static readonly Vector3 PlayerSpawn = new Vector3(-60f, 120f, -60f);

        public static SimulationSetup CreateSetup()
        {
            return new SimulationSetup(CreateWorld, PlayerSpawn, dripSources: new[] { DripSource });
        }

        public static CollisionWorld CreateWorld()
        {
            var world = SandboxFlightWorld.Create();
            const ShapeFlags Furniture = ShapeFlags.Obstacle | ShapeFlags.Attachable;
            world.Add(CollisionShape.Box("sink", new Vector3(0f, 40f, 60f), new Vector3(30f, 40f, 25f), Furniture));
            world.Add(CollisionShape.Box("shower_wall", new Vector3(180f, 125f, 60f), new Vector3(2f, 125f, 80f), Furniture));
            world.Add(CollisionShape.Box("shower_steam", new Vector3(215f, 125f, 60f), new Vector3(33f, 125f, 80f), ShapeFlags.HumidStrong));
            world.Add(CollisionShape.Box("damp_corner", new Vector3(-200f, 60f, 150f), new Vector3(45f, 60f, 45f), ShapeFlags.HumidWeak));
            return world;
        }
    }
}
