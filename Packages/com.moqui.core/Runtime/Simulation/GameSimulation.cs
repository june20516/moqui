using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 고정 틱 시뮬레이션 (tech/architecture.md §4.1). Step 한 번이 1틱(1/60초)이다.
    /// 시간은 틱 수로만 계산하며, 같은 설정·같은 시드·같은 커맨드 열이면 같은 결과를 낸다.
    /// </summary>
    public sealed class GameSimulation
    {
        public const int TickRate = 60;
        public const float DeltaTime = 1f / TickRate;

        private readonly FlightSystem _flight;
        private readonly StaminaSystem _stamina;
        private readonly DashSystem _dash;
        private readonly SphereMover _mover;
        private readonly FallingBodySystem _fallingBodies;
        private readonly HumanSystem _humanSystem;
        private readonly List<FallingBody> _bodies = new List<FallingBody>();
        private readonly List<SimulationEvent> _events = new List<SimulationEvent>();

        public GameSimulation(GameSettings settings, CollisionWorld world, Vector3 playerSpawn)
            : this(settings, new SimulationSetup(world, playerSpawn))
        {
        }

        public GameSimulation(GameSettings settings, SimulationSetup setup)
        {
            Settings = settings;
            World = setup.World;
            Seed = setup.Seed;
            Player = new Player(setup.PlayerSpawn, settings.Player.CollisionRadius, settings.Stamina.Max);
            _mover = new SphereMover(World);
            _flight = new FlightSystem(settings.Flight);
            _stamina = new StaminaSystem(settings.Stamina, settings.Hiding);
            _dash = new DashSystem(settings.Dash, settings.Flight, _stamina, _mover);
            _fallingBodies = new FallingBodySystem(settings.World);
            if (setup.Human != null)
            {
                Human = new Human(setup.Human, World);
                _humanSystem = new HumanSystem(settings, World, setup.Seed);
            }
        }

        public GameSettings Settings { get; }

        public CollisionWorld World { get; }

        public ulong Seed { get; }

        public Player Player { get; }

        /// <summary>레벨의 인간. 없으면 null (샌드박스).</summary>
        public Human Human { get; }

        public HumanSystem HumanSystem => _humanSystem;

        /// <summary>다음에 실행할 틱 번호. Step이 끝날 때 1 증가한다.</summary>
        public int Tick { get; private set; }

        public float ElapsedSeconds => Tick * DeltaTime;

        /// <summary>직전 Step에서 발생한 이벤트.</summary>
        public IReadOnlyList<SimulationEvent> Events => _events;

        /// <summary>스킬 와류 제어 3레벨 (spec/09). 대각선 대시를 허용한다.</summary>
        public bool DiagonalDashUnlocked { get; set; }

        public IReadOnlyList<FallingBody> FallingBodies => _bodies;

        public void AddFallingBody(FallingBody body)
        {
            _bodies.Add(body);
        }

        public void Step(PlayerCommand command)
        {
            _events.Clear();
            if (Player.State != PlayerState.Dead)
            {
                StepPlayer(command);
            }

            foreach (var body in _bodies)
            {
                _fallingBodies.Step(body, DeltaTime);
            }

            _stamina.Update(Player, Tick, DeltaTime);
            if (Human != null)
            {
                _humanSystem.Step(Human, Player, Tick, DeltaTime, _events);
            }

            Tick++;
        }

        public SimulationSnapshot CaptureSnapshot()
        {
            return new SimulationSnapshot(Tick, new PlayerSnapshot(Player), Human != null ? new HumanSnapshot(Human) : null);
        }

        private void StepPlayer(in PlayerCommand command)
        {
            Player.Yaw = command.LookYaw;
            Player.PrecisionHeld = command.PrecisionHeld;
            Player.SpeedMultiplier = _stamina.SpeedMultiplier(Player);

            _dash.TryStart(Player, command, Tick, DiagonalDashUnlocked, _events);
            if (Player.State == PlayerState.Dashing)
            {
                _dash.Step(Player);
            }
            else if (Player.State == PlayerState.Flying)
            {
                Fly(command);
            }
        }

        private void Fly(in PlayerCommand command)
        {
            Vector3 previousVelocity = Player.Velocity;
            _flight.UpdateVelocity(Player, command, DeltaTime);

            // 틱 안에서 속도가 선형으로 변한다고 보고 평균 속도로 적분한다. 외력(바람)은 관성 없이 그대로 더한다.
            Vector3 averageVelocity = (previousVelocity + Player.Velocity) * 0.5f;
            Vector3 displacement = (averageVelocity + Player.ExternalVelocity) * DeltaTime;
            var move = _mover.Move(Player.Position, Player.CollisionRadius, displacement, Player.Velocity, ShapeFlags.Obstacle);
            Player.Position = move.Position;
            Player.Velocity = move.Velocity;
        }
    }
}
