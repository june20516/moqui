using System.Numerics;
using Moqui.Core.Collision;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 고정 틱 시뮬레이션 (tech/architecture.md §4.1). Step 한 번이 1틱(1/60초)이다.
    /// 시간은 틱 수로만 계산하며, 같은 설정·같은 커맨드 열이면 같은 결과를 낸다.
    /// </summary>
    public sealed class GameSimulation
    {
        public const int TickRate = 60;
        public const float DeltaTime = 1f / TickRate;

        private readonly FlightSystem _flight;
        private readonly SphereMover _mover;

        public GameSimulation(GameSettings settings, CollisionWorld world, Vector3 playerSpawn)
        {
            Settings = settings;
            World = world;
            Player = new Player(playerSpawn, settings.Player.CollisionRadius);
            _flight = new FlightSystem(settings.Flight);
            _mover = new SphereMover(world);
        }

        public GameSettings Settings { get; }

        public CollisionWorld World { get; }

        public Player Player { get; }

        public int Tick { get; private set; }

        public float ElapsedSeconds => Tick * DeltaTime;

        public void Step(PlayerCommand command)
        {
            Player.Yaw = command.LookYaw;

            Vector3 previousVelocity = Player.Velocity;
            _flight.UpdateVelocity(Player, command, DeltaTime);

            // 틱 안에서 속도가 선형으로 변한다고 보고 평균 속도로 적분한다 (가감속 구간의 거리 오차를 줄인다).
            Vector3 averageVelocity = (previousVelocity + Player.Velocity) * 0.5f;
            var move = _mover.Move(Player.Position, Player.CollisionRadius, averageVelocity * DeltaTime, Player.Velocity, ShapeFlags.Obstacle);
            Player.Position = move.Position;
            Player.Velocity = move.Velocity;

            Tick++;
        }
    }
}
