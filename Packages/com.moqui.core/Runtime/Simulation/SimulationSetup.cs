using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Meta;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 시뮬레이션 구성: 충돌 월드, 시작 위치, 인간(없을 수 있음), 레벨 시드.
    /// 재시도(spec/04 §7)는 같은 구성으로 시뮬레이션을 새로 만든다. 상태가 시뮬레이션 인스턴스에만 있으므로
    /// 게이지·자국·경계·위치가 빠짐없이 초기화된다. 이를 위해 월드는 팩토리로 받는다.
    /// </summary>
    public sealed class SimulationSetup
    {
        private readonly Func<CollisionWorld> _worldFactory;

        public SimulationSetup(Func<CollisionWorld> worldFactory, Vector3 playerSpawn, HumanDefinition human = null, ulong seed = 0, IReadOnlyList<Vector3> dripSources = null, SkillLoadout skills = null)
        {
            Skills = skills ?? SkillLoadout.None;
            _worldFactory = worldFactory ?? throw new ArgumentNullException(nameof(worldFactory));
            PlayerSpawn = playerSpawn;
            Human = human;
            Seed = seed;
            DripSources = dripSources ?? Array.Empty<Vector3>();
        }

        /// <summary>이미 만든 월드로 구성한다. 인간 캡슐이 월드에 등록되므로 이 구성은 한 번만 쓸 수 있다 (재시도 불가).</summary>
        public SimulationSetup(CollisionWorld world, Vector3 playerSpawn, HumanDefinition human = null, ulong seed = 0, IReadOnlyList<Vector3> dripSources = null, SkillLoadout skills = null)
            : this(SingleUse(world), playerSpawn, human, seed, dripSources, skills)
        {
        }

        public Vector3 PlayerSpawn { get; }

        /// <summary>
        /// 스킬 레벨과 장착 액티브 (spec/09). 수치 효과는 SkillEffects.Apply로 만든 GameSettings에 이미 들어 있고,
        /// 시뮬레이션은 수치가 아닌 효과(대각선 대시, 연속 와류, 미끼)만 여기서 읽는다.
        /// </summary>
        public SkillLoadout Skills { get; }

        public HumanDefinition Human { get; }

        public ulong Seed { get; }

        /// <summary>물방울 발생원 위치 (spec/05 §1, 레벨 데이터 dripSources[]).</summary>
        public IReadOnlyList<Vector3> DripSources { get; }

        public CollisionWorld CreateWorld()
        {
            return _worldFactory();
        }

        private static Func<CollisionWorld> SingleUse(CollisionWorld world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            bool used = false;
            return () =>
            {
                if (used)
                {
                    throw new InvalidOperationException("This setup was built from a world instance and cannot be reused. Pass a world factory to support retry.");
                }

                used = true;
                return world;
            };
        }
    }
}
