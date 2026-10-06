using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Meta;
using Moqui.Core.Random;

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
        private readonly List<Human> _humans = new List<Human>();
        private readonly List<HumanSystem> _humanSystems = new List<HumanSystem>();
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
            Suck = new SuckSystem(settings.Suck, settings.Sites, settings.BiteMark, settings.SuckEvent);
            Water = new WaterSystem(settings.Water, World, _fallingBodies, _dash, setup.DripSources);
            Humidity = new HumiditySystem(settings.Humid, settings.Water, settings.Hiding, World, Water);
            _dash.ChainLevel = setup.Skills.Level(SkillCatalog.ChainVortex);
            Decoy = new DecoySystem(settings.Decoy, World, setup.Skills.ActiveLevel(SkillCatalog.DecoyCharm));
            Fans = new FanSystem(settings.Fan, setup.Gimmicks.Fans);
            Lights = new LightSystem(settings.Light, setup.Gimmicks.Lights);
            Toxin = new ToxinSystem(settings.Toxin, Fans, setup.Gimmicks.Coils, setup.Gimmicks.SprayDispensers, SeedStreams.Create(setup.Seed, SeedStreams.Debuff));
            foreach (var shape in World.Shapes)
            {
                if (shape.Matches(ShapeFlags.ShadowZone))
                {
                    _shadowZones.Add(new ZoneSnapshot(shape));
                }
            }
            if (setup.Human != null)
            {
                Human = AddHuman(setup.Human, setup.Seed);
                _humanSystem = _humanSystems[0];
                for (int i = 0; i < setup.Companions.Count; i++)
                {
                    AddHuman(setup.Companions[i], SeedStreams.Derive(setup.Seed, $"companion{i + 1}"));
                }

                if (Human.IsDrunk)
                {
                    // 취한 타겟 (spec/06): 흡혈 속도·가려움 배율.
                    Suck.RateMultiplier *= settings.Drunk.SuckRateMul;
                    Suck.ItchMultiplier *= settings.Drunk.ItchRateMul;
                }
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

        /// <summary>모든 인간 (주 인간이 첫째, M14 두 사람).</summary>
        public IReadOnlyList<Human> Humans => _humans;

        public HumanSystem HumanSystemOf(Human human) => _humanSystems[_humans.IndexOf(human)];

        private Human AddHuman(HumanDefinition definition, ulong seed)
        {
            var human = new Human(definition, World, Settings.Body);
            human.ToolLength = Settings.Attack.SwatterLength;
            human.UpdatePose();
            var system = new HumanSystem(Settings, World, seed);
            system.Initialize(human, 0);
            _humans.Add(human);
            _humanSystems.Add(system);
            return human;
        }

        /// <summary>그 형상을 가진 인간 (없으면 null).</summary>
        private Human OwnerOf(CollisionShape shape)
        {
            foreach (var human in _humans)
            {
                if (human.Owns(shape))
                {
                    return human;
                }
            }

            return null;
        }

        public SuckSystem Suck { get; }

        /// <summary>선풍기 (spec/06).</summary>
        public FanSystem Fans { get; }

        /// <summary>조명 스위치 (spec/06, M14).</summary>
        public LightSystem Lights { get; }

        /// <summary>모기약 연무·모기향·중독 (spec/06).</summary>
        public ToxinSystem Toxin { get; }

        /// <summary>액티브 스킬 미끼 마법. 장착하지 않았으면 IsAvailable = false.</summary>
        public DecoySystem Decoy { get; }

        /// <summary>현재 대시 스태미나 비용 (HUD 스태미나 눈금, spec/08).</summary>
        public float DashCost => _dash.Cost(Player);

        /// <summary>지금 대시를 시작할 수 있는가 (1인칭 대시 표식, gulf §5).</summary>
        public bool CanDashNow => Player.State == PlayerState.Flying && _dash.CanDash(Player, Tick);

        /// <summary>지금 착지 입력을 하면 붙을 수 있는가 (HUD 착지 프롬프트, spec/08).</summary>
        public bool CanAttach => _attach.HasTarget(Player);

        /// <summary>지금 F를 누르면 붙을 지점 (착지 표시, gulf §2).</summary>
        public bool TryGetAttachTarget(out Vector3 point, out Vector3 normal) => _attach.TryGetTarget(Player, out point, out normal, out _);

        /// <summary>지금 F를 누르면 붙을 형상 (맨살 겨눔 큐 wand.aim, gulf §3). 없으면 null.</summary>
        public CollisionShape AttachTargetShape => _attach.TryGetTarget(Player, out _, out _, out var shape) ? shape : null;

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

            for (int i = 0; i < _humans.Count; i++)
            {
                _humanSystems[i].StepMotion(_humans[i], Tick);
            }

            if (Player.State != PlayerState.Dead)
            {
                // 중독 디버프는 입력 단계에서 건다 (spec/06, spec/01 입력 필터).
                StepPlayer(Toxin.Filter(command, Player, Tick));
            }

            UpdateWeb();
            UpdateNetGap();
            Decoy.Step(Player, command, Tick, _events);
            UpdateHidden();
            Water.Step(Player, Tick, DeltaTime, _events);
            Humidity.Step(Player, DeltaTime);

            foreach (var body in _bodies)
            {
                _fallingBodies.Step(body, DeltaTime);
            }

            _stamina.Update(Player, Tick, DeltaTime);
            for (int i = 0; i < _humans.Count; i++)
            {
                _humanSystems[i].Step(_humans[i], Player, Tick, DeltaTime, _events);
            }

            ShareAlarm();

            foreach (var released in _events.OfType<SprayReleased>().ToList())
            {
                Toxin.Spawn(released.Position, Tick);
            }

            Toxin.StepClouds(Tick, DeltaTime);
            Toxin.UpdateGauge(Player, Tick, DeltaTime, _events);
            Suck.Step(Player, _humans, command, Tick, DeltaTime, _events);
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
            var decoy = Decoy.IsAvailable ? new DecoySnapshot(Decoy.IsActive(Tick), Decoy.Position, Decoy.CooldownRemaining(Tick)) : null;
            var gimmicks = new GimmickSnapshot(Fans.Snapshot(Tick), Toxin.Snapshot(Tick), Toxin.Coils, Toxin.StutterActive(Tick), Toxin.RandomActive(Tick));
            return new SimulationSnapshot(Tick, Outcome, new PlayerSnapshot(Player), human, drops, Water.TrappedHeightRemaining(Player), _shadowZones, Array.Empty<ZoneSnapshot>(), decoy, gimmicks);
        }

        private void StepPlayer(in PlayerCommand command)
        {
            Player.Yaw = command.LookYaw;
            Player.PrecisionHeld = command.PrecisionHeld;
            Player.SuckHeld = command.SuckHeld;
            Player.SpeedMultiplier = _stamina.SpeedMultiplier(Player) * Suck.SpeedMultiplier(Player.BloodGauge);
            Player.DashDistanceMultiplier = Suck.DashMultiplier(Player.BloodGauge);
            Player.StaminaRegenMultiplier = 1f;
            Player.DashCostAdd = 0f;
            Player.NoiseRadiusMultiplier = Fans.NoiseMultiplier(Player.Position, Tick);

            // 바람은 입력과 별개의 외력이다 (관성 규칙 미적용, spec/06). 부착 중에는 받지 않는다.
            Player.ExternalVelocity = Player.State == PlayerState.Attached ? Vector3.Zero : Fans.WindAt(Player.Position, Tick);
            Humidity.ApplyWetEffects(Player);

            switch (Player.State)
            {
                case PlayerState.Attached:
                    // 부착 중 대시: 붙은 표면의 법선 방향으로 떨어져 나가며 대시한다 (spec/01, D-051). 대시할 수 없으면 그대로 붙어 있다.
                    if (command.DashPressed && _dash.CanDash(Player, Tick))
                    {
                        // 흡혈 중 대시 = 꽂은 지팡이를 억지로 뽑는 긴급 탈출. 대가로 그 부위가 가려워진다 (gulf §1, D-066).
                        if (AttachSystem.IsSucking(Player, command))
                        {
                            var site = Player.SuckSession.Site;
                            site.Itch = Math.Min(ReactionSystem.GaugeMax, site.Itch + Settings.Suck.YankItch);
                            _events.Add(new WandYanked(Tick, site.PartId, site.Itch));
                        }

                        Player.Anchor.Resolve(out _, out Vector3 surfaceNormal);
                        _attach.Detach(Player, Tick, _events);
                        _dash.Start(Player, surfaceNormal, Tick, _events);
                        _dash.Step(Player);
                        return;
                    }

                    _attach.StepAttached(Player, command, Tick, DeltaTime, _events);
                    if (Player.State == PlayerState.Attached)
                    {
                        _attach.TryDislodge(Player, Tick, _events, GripMultiplier(), CarriedVelocity());
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
                case PlayerState.Webbed:
                    return;
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

            _dash.TryStart(Player, command, Tick, _events);
            if (Player.State == PlayerState.Dashing)
            {
                _dash.Step(Player);
            }
            else if (Player.State == PlayerState.Flying)
            {
                Fly(command);
            }
        }

        /// <summary>걷는 인간에 붙어 있으면 몸 전체 이동으로 생긴 부착점 속도 (spec/02 §9).</summary>
        private Vector3 CarriedVelocity()
        {
            var owner = OwnerOf(Player.Anchor.Shape);
            return owner != null ? owner.CarriedVelocityAt(Player.Position, DeltaTime) : Vector3.Zero;
        }

        /// <summary>
        /// 흡혈 중 "부위가 움직임" 이벤트에서 Suck을 누른 채 버티면 튕김 기준 속도에 suckEvent.gripMul을 곱한다 (D-056).
        /// </summary>
        private float GripMultiplier()
        {
            var owner = Player.Anchor != null ? OwnerOf(Player.Anchor.Shape) : null;
            bool shifting = owner != null && owner.SuckEvent.Is(SuckEventKind.Shift, SuckEventPhase.Active);
            return shifting && Player.SuckHeld && Player.SuckSession != null ? Settings.SuckEvent.GripMul : 1f;
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
                int frenzyCount = _humans.Sum(human => human.FrenzyCount);
                int biteMarks = _humans.Sum(human => human.BiteMarkCount);
                _events.Add(new StageCleared(Tick, new StageResult(Tick + 1, frenzyCount, biteMarks)));
            }
        }

        /// <summary>거미줄 (spec/06): 닿으면 움직이지 못하고 web.struggleTime 뒤 Web 원인으로 사망한다. 탈출할 수 없다.</summary>
        private void UpdateWeb()
        {
            if (Player.State == PlayerState.Dead)
            {
                return;
            }

            if (Player.State == PlayerState.Webbed)
            {
                if (Tick - Player.WebbedTick >= SimulationTime.ToTicks(Settings.WebStruggleTime))
                {
                    Player.State = PlayerState.Dead;
                    _events.Add(new PlayerDied(Tick, DeathCause.Web, Player.Position));
                }

                return;
            }

            if (Player.State != PlayerState.Attached && World.AnyOverlap(Player.Position, Player.CollisionRadius, ShapeFlags.Hazard))
            {
                Player.State = PlayerState.Webbed;
                Player.WebbedTick = Tick;
                Player.Velocity = Vector3.Zero;
                Player.ExternalVelocity = Vector3.Zero;
                _events.Add(new PlayerWebbed(Tick, Player.Position));
            }
        }

        /// <summary>숨은 상태 = 플레이어 충돌 구 중심이 Shadow Zone 볼륨 안 (spec/03, D-034).</summary>
        private bool _wasInNetGap;

        /// <summary>
        /// 광분 전염 (spec/02 두 사람, M14): 한 사람이 광분하면 광분하지 않은 다른 사람의 경계를 human.alarmShare까지 올리고
        /// 그 사람이 본 자극 위치를 쳐다보게 한다.
        /// </summary>
        private void ShareAlarm()
        {
            foreach (var changed in _events.OfType<AwarenessStateChanged>().ToList())
            {
                if (changed.To != AwarenessState.Frenzy)
                {
                    continue;
                }

                var source = _humans.FirstOrDefault(human => human.Id == changed.HumanId);
                foreach (var other in _humans)
                {
                    if (other == source || other.State == AwarenessState.Frenzy || source == null)
                    {
                        continue;
                    }

                    other.Causes.Add(AwarenessCause.Alarm, Settings.HumanMotion.AlarmShare - other.Awareness);
                    other.Awareness = Math.Max(other.Awareness, Settings.HumanMotion.AlarmShare);
                    other.HasStimulus = true;
                    other.LastStimulusPosition = source.HasStimulus ? source.LastStimulusPosition : source.LastSeenPosition;
                    other.LastStimulusTick = Tick;
                }
            }
        }

        /// <summary>
        /// 모기장 틈 (spec/06, M14): 비행 중 틈 볼륨에 들어서는 순간 정밀 비행이 아니면 그물을 스치는 소음을 낸다.
        /// </summary>
        private void UpdateNetGap()
        {
            bool inGap = Player.State != PlayerState.Dead && World.AnyOverlap(Player.Position, Player.CollisionRadius, ShapeFlags.NetGap);
            if (inGap && !_wasInNetGap && !Player.PrecisionHeld)
            {
                var net = Settings.Net;
                _events.Add(new NoiseEmitted(Tick, NoiseSource.Net, Player.Position, net.RustleRadius, net.RustleAwareness));
            }

            _wasInNetGap = inGap;
        }

        /// <summary>은신 (spec/03): Shadow Zone 안이고 켜진 조명 영역 밖이다 (불이 켜지면 그림자가 사라진다, spec/06 M14).</summary>
        private void UpdateHidden()
        {
            Lights.Step(_humans, Tick);
            Player.InLight = Lights.IsLit(Player.Position);
            Player.IsHidden = World.AnyOverlap(Player.Position, 0f, ShapeFlags.ShadowZone) && !Player.InLight;
        }

        private void Fly(in PlayerCommand command)
        {
            Vector3 previousVelocity = Player.Velocity;
            _flight.UpdateVelocity(Player, command, DeltaTime);
            Vector3 intendedVelocity = Player.Velocity;

            // 틱 안에서 속도가 선형으로 변한다고 보고 평균 속도로 적분한다. 외력(바람)은 관성 없이 그대로 더한다.
            Vector3 averageVelocity = (previousVelocity + Player.Velocity) * 0.5f;
            Vector3 displacement = (averageVelocity + Player.ExternalVelocity) * DeltaTime;
            var move = _mover.Move(Player.Position, Player.CollisionRadius, displacement, Player.Velocity, ShapeFlags.Solid);
            Player.Position = move.Position;
            Player.Velocity = move.Velocity;

            // 정밀 비행으로 표면 쪽으로 날다 닿으면 그대로 내려앉는다 (gulf §2).
            _attach.TryAutoLand(Player, command, intendedVelocity, Tick, _events);
        }
    }
}
