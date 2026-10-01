using System;
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
        private readonly AttachSystem _attach;
        private readonly SphereMover _mover;
        private readonly FallingBodySystem _fallingBodies;
        private readonly HumanSystem _humanSystem;
        private readonly List<FallingBody> _bodies = new List<FallingBody>();
        private readonly List<SimulationEvent> _events = new List<SimulationEvent>();
        private readonly List<ZoneSnapshot> _shadowZones = new List<ZoneSnapshot>();

        public GameSimulation(GameSettings settings, CollisionWorld world, Vector3 playerSpawn)
            : this(settings, new SimulationSetup(world, playerSpawn))
        {
        }

        public GameSimulation(GameSettings settings, SimulationSetup setup)
        {
            Settings = settings;
            Setup = setup;
            World = setup.CreateWorld();
            Seed = setup.Seed;
            Player = new Player(setup.PlayerSpawn, settings.Player.CollisionRadius, settings.Stamina.Max);
            _mover = new SphereMover(World);
            _flight = new FlightSystem(settings.Flight);
            _stamina = new StaminaSystem(settings.Stamina, settings.Hiding);
            _dash = new DashSystem(settings.Dash, settings.Flight, _stamina, _mover);
            _attach = new AttachSystem(settings.Attach, settings.HumanMotion, settings.Flight, World, _mover);
            _fallingBodies = new FallingBodySystem(settings.World);
            Suck = new SuckSystem(settings.Suck, settings.Sites, settings.BiteMark);
            Water = new WaterSystem(settings.Water, World, _fallingBodies, _dash, setup.DripSources);
            Humidity = new HumiditySystem(settings.Humid, settings.Water, settings.Hiding, World, Water);
            foreach (var shape in World.Shapes)
            {
                if (shape.Matches(ShapeFlags.ShadowZone))
                {
                    _shadowZones.Add(new ZoneSnapshot(shape));
                }
            }
            if (setup.Human != null)
            {
                Human = new Human(setup.Human, World);
                _humanSystem = new HumanSystem(settings, World, setup.Seed);
                _humanSystem.Initialize(Human, 0);
            }
        }

        public GameSettings Settings { get; }

        public SimulationSetup Setup { get; }

        public CollisionWorld World { get; }

        public ulong Seed { get; }

        public Player Player { get; }

        /// <summary>레벨의 인간. 없으면 null (샌드박스).</summary>
        public Human Human { get; }

        public HumanSystem HumanSystem => _humanSystem;

        public SuckSystem Suck { get; }

        /// <summary>현재 대시 스태미나 비용 (HUD 스태미나 눈금, spec/08).</summary>
        public float DashCost => _dash.Cost(Player);

        /// <summary>지금 착지 입력을 하면 붙을 수 있는가 (HUD 착지 프롬프트, spec/08).</summary>
        public bool CanAttach => _attach.HasTarget(Player);

        public WaterSystem Water { get; }

        public HumiditySystem Humidity { get; }

        public StageOutcome Outcome { get; private set; }

        /// <summary>다음에 실행할 틱 번호. Step이 끝날 때 1 증가한다.</summary>
        public int Tick { get; private set; }

        public float ElapsedSeconds => Tick * DeltaTime;

        /// <summary>직전 Step에서 발생한 이벤트.</summary>
        public IReadOnlyList<SimulationEvent> Events => _events;

        /// <summary>마지막 틱의 입력 커맨드 (튜토리얼 판정, spec/08).</summary>
        public PlayerCommand LastCommand { get; private set; }

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
            LastCommand = command;

            // 승리하면 입력을 막고 세계를 멈춘다 (spec/04 §6). 패배는 사망 연출 동안 세계가 계속 움직인다.
            if (Outcome == StageOutcome.Cleared)
            {
                Tick++;
                return;
            }

            if (Human != null)
            {
                _humanSystem.StepMotion(Human, Tick);
            }

            if (Player.State != PlayerState.Dead)
            {
                StepPlayer(command);
            }

            UpdateHidden();
            Water.Step(Player, Tick, DeltaTime, _events);
            Humidity.Step(Player, DeltaTime);

            foreach (var body in _bodies)
            {
                _fallingBodies.Step(body, DeltaTime);
            }

            _stamina.Update(Player, Tick, DeltaTime);
            if (Human != null)
            {
                _humanSystem.Step(Human, Player, Tick, DeltaTime, _events);
            }

            Suck.Step(Player, Human, command, Tick, DeltaTime, _events);
            UpdateOutcome();
            Tick++;
        }

        /// <summary>재시도: 같은 구성으로 처음부터 시작하는 새 시뮬레이션 (spec/04 §7).</summary>
        public GameSimulation Retry()
        {
            return new GameSimulation(Settings, Setup);
        }

        public SimulationSnapshot CaptureSnapshot()
        {
            var drops = new List<Vector3>();
            foreach (var drop in Water.Drops)
            {
                drops.Add(drop.Position);
            }

            var human = Human != null ? new HumanSnapshot(Human) : null;
            return new SimulationSnapshot(Tick, Outcome, new PlayerSnapshot(Player), human, drops, Water.TrappedHeightRemaining(Player), _shadowZones, Array.Empty<ZoneSnapshot>());
        }

        private void StepPlayer(in PlayerCommand command)
        {
            Player.Yaw = command.LookYaw;
            Player.PrecisionHeld = command.PrecisionHeld;
            Player.SpeedMultiplier = _stamina.SpeedMultiplier(Player) * Suck.SpeedMultiplier(Player.BloodGauge);
            Player.DashDistanceMultiplier = Suck.DashMultiplier(Player.BloodGauge);
            Player.StaminaRegenMultiplier = 1f;
            Player.DashCostAdd = 0f;
            Humidity.ApplyWetEffects(Player);

            switch (Player.State)
            {
                case PlayerState.Attached:
                    _attach.StepAttached(Player, command, Tick, DeltaTime, _events);
                    if (Player.State == PlayerState.Attached)
                    {
                        _attach.TryDislodge(Player, Tick, _events);
                    }

                    return;
                case PlayerState.Dislodged:
                    // 경직 동안 입력을 무시하고 밀린 속도로 감속만 한다 (spec/02 §6).
                    if (Tick < Player.StunEndTick)
                    {
                        Fly(new PlayerCommand { LookYaw = command.LookYaw });
                        return;
                    }

                    Player.State = PlayerState.Flying;
                    break;
                case PlayerState.Trapped:
                    // 이동 입력은 무시하고 탈출(Dash) 입력만 센다 (spec/05). 탈출하면 같은 틱에 위로 대시를 시작한다.
                    Water.StepTrapped(Player, command, Tick, _events);
                    if (Player.State == PlayerState.Dashing)
                    {
                        _dash.Step(Player);
                    }

                    return;
            }

            if (_attach.TryAttach(Player, command, Tick, _events))
            {
                return;
            }

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

        /// <summary>승패 판정 (architecture §4.4 Outcome): 흡혈 게이지 100% → Cleared(1회), 사망 → Died.</summary>
        private void UpdateOutcome()
        {
            if (Outcome != StageOutcome.InProgress)
            {
                return;
            }

            if (Player.State == PlayerState.Dead)
            {
                Outcome = StageOutcome.Died;
            }
            else if (Player.BloodGauge >= SuckSystem.GaugeMax)
            {
                Outcome = StageOutcome.Cleared;
                int frenzyCount = Human?.FrenzyCount ?? 0;
                int biteMarks = Human?.BiteMarkCount ?? 0;
                _events.Add(new StageCleared(Tick, new StageResult(Tick + 1, frenzyCount, biteMarks)));
            }
        }

        /// <summary>숨은 상태 = 플레이어 충돌 구 중심이 Shadow Zone 볼륨 안 (spec/03, D-034).</summary>
        private void UpdateHidden()
        {
            Player.IsHidden = World.AnyOverlap(Player.Position, 0f, ShapeFlags.ShadowZone);
        }

        private void Fly(in PlayerCommand command)
        {
            Vector3 previousVelocity = Player.Velocity;
            _flight.UpdateVelocity(Player, command, DeltaTime);

            // 틱 안에서 속도가 선형으로 변한다고 보고 평균 속도로 적분한다. 외력(바람)은 관성 없이 그대로 더한다.
            Vector3 averageVelocity = (previousVelocity + Player.Velocity) * 0.5f;
            Vector3 displacement = (averageVelocity + Player.ExternalVelocity) * DeltaTime;
            var move = _mover.Move(Player.Position, Player.CollisionRadius, displacement, Player.Velocity, ShapeFlags.Solid);
            Player.Position = move.Position;
            Player.Velocity = move.Velocity;
        }
    }
}
