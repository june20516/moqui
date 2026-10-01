using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>시뮬레이션 구성: 충돌 월드, 시작 위치, 인간(없을 수 있음), 레벨 시드.</summary>
    public sealed class SimulationSetup
    {
        public SimulationSetup(CollisionWorld world, Vector3 playerSpawn, HumanDefinition human = null, ulong seed = 0)
        {
            World = world;
            PlayerSpawn = playerSpawn;
            Human = human;
            Seed = seed;
        }

        public CollisionWorld World { get; }

        public Vector3 PlayerSpawn { get; }

        public HumanDefinition Human { get; }

        public ulong Seed { get; }
    }
}
