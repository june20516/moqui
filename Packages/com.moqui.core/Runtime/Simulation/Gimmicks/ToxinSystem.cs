using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Data;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>모기약·모기향 수치 (spec/tuning.md spray, coil). 해독 체질은 스킬 반영 Tuning에 이미 들어 있다 (D-043).</summary>
    public sealed class ToxinSettings
    {
        public ToxinSettings(Tuning tuning)
        {
            Telegraph = tuning.GetFloat("spray.telegraph");
            UseRange = tuning.GetFloat("spray.useRange");
            Cooldown = tuning.GetFloat("spray.cooldown");
            Travel = tuning.GetFloat("spray.travel");
            RadiusStart = tuning.GetFloat("spray.radiusStart");
            RadiusMax = tuning.GetFloat("spray.radiusMax");
            ExpandTime = tuning.GetFloat("spray.expandTime");
            Lifetime = tuning.GetFloat("spray.lifetime");
            WindDriftMul = tuning.GetFloat("spray.windDriftMul");
            ToxinRate = tuning.GetFloat("spray.toxinRate");
            ToxinDecay = tuning.GetFloat("spray.toxinDecay");
            Tier1 = tuning.GetFloat("spray.tier1");
            Tier2 = tuning.GetFloat("spray.tier2");
            Tier3 = tuning.GetFloat("spray.tier3");
            StutterInterval = tuning.GetFloat("spray.stutterInterval");
            StutterChanceMin = tuning.GetFloat("spray.stutterChanceMin");
            StutterChance = tuning.GetFloat("spray.stutterChance");
            StutterDuration = tuning.GetFloat("spray.stutterDuration");
            RandomInterval = tuning.GetFloat("spray.randomInterval");
            RandomDuration = tuning.GetFloat("spray.randomDuration");
            DispenserInterval = tuning.GetFloat("spray.dispenserInterval");
            CoilRange = tuning.GetFloat("coil.range");
            CoilDenseRadius = tuning.GetFloat("coil.denseRadius");
            CoilNearFloor = tuning.GetFloat("coil.nearFloor");
            CoilFarFloor = tuning.GetFloat("coil.farFloor");
            CoilLethalRadius = tuning.GetFloat("coil.lethalRadius");
            CoilCoreRate = tuning.GetFloat("coil.coreRate");
            CoilWindFloorMul = tuning.GetFloat("coil.windFloorMul");
            CoilShadowFloorMul = tuning.GetFloat("coil.shadowFloorMul");
            HiddenRecoveryMul = tuning.GetFloat("hiding.debuffRecoveryMul");
        }

        public float Telegraph { get; }

        public float UseRange { get; }

        public float Cooldown { get; }

        public float Travel { get; }

        public float RadiusStart { get; }

        public float RadiusMax { get; }

        public float ExpandTime { get; }

        public float Lifetime { get; }

        public float WindDriftMul { get; }

        public float ToxinRate { get; }

        public float ToxinDecay { get; }

        public float Tier1 { get; }

        public float Tier2 { get; }

        public float Tier3 { get; }

        public float StutterInterval { get; }

        public float StutterChanceMin { get; }

        public float StutterChance { get; }

        public float StutterDuration { get; }

        public float RandomInterval { get; }

        public float RandomDuration { get; }

        public float DispenserInterval { get; }

        public float CoilRange { get; }

        public float CoilDenseRadius { get; }

        public float CoilNearFloor { get; }

        public float CoilFarFloor { get; }

        public float CoilLethalRadius { get; }

        public float CoilCoreRate { get; }

        public float CoilWindFloorMul { get; }

        public float CoilShadowFloorMul { get; }

        public float HiddenRecoveryMul { get; }
    }

    /// <summary>모기약 연무 하나 (spec/06).</summary>
    public sealed class SprayCloud
    {
        public SprayCloud(int id, Vector3 position, int startTick)
        {
            Id = id;
            Position = position;
            StartTick = startTick;
        }

        public int Id { get; }

        public Vector3 Position { get; set; }

        public int StartTick { get; }
    }

    /// <summary>연무 스냅샷 (표현).</summary>
    public sealed class SprayCloudSnapshot
    {
        public SprayCloudSnapshot(int id, Vector3 position, float radius, float age)
        {
            Id = id;
            Position = position;
            Radius = radius;
            Age = age;
        }

        public int Id { get; }

        public Vector3 Position { get; }

        public float Radius { get; }

        /// <summary>생긴 뒤 지난 초.</summary>
        public float Age { get; }
    }

    /// <summary>
    /// 중독 (spec/06): 모기약 연무와 모기향이 중독 게이지(0~100)를 올리고, 단계별 입력 디버프(끊김·반전·랜덤)를 건다.
    /// 100이면 Spray 원인으로 사망. 모기향은 위치별 하한을 깔아 게이지가 그 아래로 내려가지 않게 한다(아래면 연무 증가율로 하한까지 오른다, D-046).
    /// </summary>
    public sealed class ToxinSystem
    {
        public const float GaugeMax = 100f;

        private readonly ToxinSettings _settings;
        private readonly FanSystem _fans;
        private readonly IReadOnlyList<Vector3> _coils;
        private readonly IReadOnlyList<Vector3> _dispensers;
        private readonly IRandom _random;
        private readonly List<SprayCloud> _clouds = new List<SprayCloud>();
        private int _nextCloudId;
        private int _stutterEndTick = Player.NeverTick;
        private int _randomEndTick = Player.NeverTick;
        private Vector2 _randomMove;
        private float _randomVertical;

        public ToxinSystem(ToxinSettings settings, FanSystem fans, IReadOnlyList<Vector3> coils, IReadOnlyList<Vector3> dispensers, IRandom random)
        {
            _settings = settings;
            _fans = fans;
            _coils = coils;
            _dispensers = dispensers;
            _random = random;
        }

        public ToxinSettings Settings => _settings;

        public IReadOnlyList<SprayCloud> Clouds => _clouds;

        public IReadOnlyList<Vector3> Coils => _coils;

        public IReadOnlyList<Vector3> Dispensers => _dispensers;

        public bool StutterActive(int tick) => tick < _stutterEndTick;

        public bool RandomActive(int tick) => tick < _randomEndTick;

        public float Radius(SprayCloud cloud, int tick)
        {
            float age = (tick - cloud.StartTick) * GameSimulation.DeltaTime;
            float t = _settings.ExpandTime > 0f ? Math.Min(1f, age / _settings.ExpandTime) : 1f;
            return _settings.RadiusStart + ((_settings.RadiusMax - _settings.RadiusStart) * t);
        }

        public bool IsAlive(SprayCloud cloud, int tick)
        {
            return tick - cloud.StartTick < SimulationTime.ToTicks(_settings.Lifetime);
        }

        public SprayCloud Spawn(Vector3 position, int tick)
        {
            var cloud = new SprayCloud(_nextCloudId++, position, tick);
            _clouds.Add(cloud);
            return cloud;
        }

        /// <summary>연무 수명·바람 떠밀림, 자동 분사기 (spec/06).</summary>
        public void StepClouds(int tick, float deltaTime)
        {
            _clouds.RemoveAll(cloud => !IsAlive(cloud, tick));
            foreach (var cloud in _clouds)
            {
                cloud.Position += _fans.WindAt(cloud.Position, tick) * (_settings.WindDriftMul * deltaTime);
            }

            int interval = SimulationTime.ToTicks(_settings.DispenserInterval);
            if (tick > 0 && interval > 0 && tick % interval == 0)
            {
                foreach (var dispenser in _dispensers)
                {
                    Spawn(dispenser, tick);
                }
            }
        }

        /// <summary>모기향 하한: denseRadius 안 nearFloor, range까지 farFloor로 선형 감소, 밖 0. 바람·Shadow Zone 안 배율 (spec/06).</summary>
        public float CoilFloor(Vector3 position, bool hidden, int tick)
        {
            float floor = 0f;
            foreach (var coil in _coils)
            {
                float distance = Vector3.Distance(position, coil);
                float value;
                if (distance <= _settings.CoilDenseRadius)
                {
                    value = _settings.CoilNearFloor;
                }
                else if (distance <= _settings.CoilRange)
                {
                    float t = (distance - _settings.CoilDenseRadius) / (_settings.CoilRange - _settings.CoilDenseRadius);
                    value = _settings.CoilNearFloor + ((_settings.CoilFarFloor - _settings.CoilNearFloor) * t);
                }
                else
                {
                    value = 0f;
                }

                floor = Math.Max(floor, value);
            }

            if (floor <= 0f)
            {
                return 0f;
            }

            if (_fans.InAnyCone(position, tick))
            {
                floor *= _settings.CoilWindFloorMul;
            }

            if (hidden)
            {
                floor *= _settings.CoilShadowFloorMul;
            }

            return floor;
        }

        public bool InCloud(Vector3 position, int tick)
        {
            foreach (var cloud in _clouds)
            {
                if (Vector3.Distance(position, cloud.Position) <= Radius(cloud, tick))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>중독 게이지를 갱신하고 100이면 사망시킨다.</summary>
        public void UpdateGauge(Player player, int tick, float deltaTime, List<SimulationEvent> events)
        {
            if (player.State == PlayerState.Dead)
            {
                return;
            }

            float floor = CoilFloor(player.Position, player.IsHidden, tick);
            player.ToxinFloor = floor;
            bool inCloud = InCloud(player.Position, tick);
            bool nearCore = IsNearCoilCore(player.Position);
            float toxin = player.Toxin;
            if (inCloud || nearCore)
            {
                // 연무 안·모기향 바로 위에서는 감소 없이 오른다 (둘 다면 합).
                toxin += ((inCloud ? _settings.ToxinRate : 0f) + (nearCore ? _settings.CoilCoreRate : 0f)) * deltaTime;
            }
            else if (toxin > floor)
            {
                float recovery = player.IsHidden ? _settings.HiddenRecoveryMul : 1f;
                toxin = Math.Max(floor, toxin - (_settings.ToxinDecay * recovery * deltaTime));
            }
            else
            {
                toxin = Math.Min(floor, toxin + (_settings.ToxinRate * deltaTime));
            }

            player.Toxin = Math.Min(GaugeMax, Math.Max(0f, toxin));
            if (player.Toxin >= GaugeMax)
            {
                player.State = PlayerState.Dead;
                player.Velocity = Vector3.Zero;
                events.Add(new PlayerDied(tick, DeathCause.Spray, player.Position));
            }
        }

        /// <summary>
        /// 입력 디버프 필터 (spec/06): 끊김(tier1 이상, 확률은 tier1→tier2에서 stutterChanceMin→stutterChance),
        /// 반전(tier2 이상, 이동·상하 입력 반대 — 대시 방향도 따라 바뀜), 랜덤(tier3 이상, 무작위 이동이 입력을 덮음). 누적 적용.
        /// 판정 난수는 시드 고정 스트림(debuff)을 쓴다.
        /// </summary>
        public PlayerCommand Filter(PlayerCommand command, Player player, int tick)
        {
            float toxin = player.Toxin;
            RollTimers(toxin, tick);

            if (toxin >= _settings.Tier2)
            {
                command.Move = -command.Move;
                command.Vertical = -command.Vertical;
            }

            if (toxin >= _settings.Tier3 && RandomActive(tick))
            {
                command.Move = _randomMove;
                command.Vertical = _randomVertical;
            }

            if (toxin >= _settings.Tier1 && StutterActive(tick))
            {
                command.Move = Vector2.Zero;
                command.Vertical = 0f;
                command.DashPressed = false;
            }

            return command;
        }

        /// <summary>끊김 확률: tier1에서 stutterChanceMin, tier2 이상에서 stutterChance, 그 사이 선형.</summary>
        public float StutterChance(float toxin)
        {
            if (toxin < _settings.Tier1)
            {
                return 0f;
            }

            float t = Math.Clamp((toxin - _settings.Tier1) / (_settings.Tier2 - _settings.Tier1), 0f, 1f);
            return _settings.StutterChanceMin + ((_settings.StutterChance - _settings.StutterChanceMin) * t);
        }

        private void RollTimers(float toxin, int tick)
        {
            int stutterInterval = Math.Max(1, SimulationTime.ToTicks(_settings.StutterInterval));
            if (toxin >= _settings.Tier1 && tick % stutterInterval == 0 && _random.Chance(StutterChance(toxin)))
            {
                _stutterEndTick = tick + SimulationTime.ToTicks(_settings.StutterDuration);
            }

            int randomInterval = Math.Max(1, SimulationTime.ToTicks(_settings.RandomInterval));
            if (toxin >= _settings.Tier3 && tick % randomInterval == 0)
            {
                float angle = _random.Range(0f, 2f * MathF.PI);
                _randomMove = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                _randomVertical = _random.Range(-1f, 1f);
                _randomEndTick = tick + SimulationTime.ToTicks(_settings.RandomDuration);
            }
        }

        private bool IsNearCoilCore(Vector3 position)
        {
            foreach (var coil in _coils)
            {
                if (Vector3.Distance(position, coil) <= _settings.CoilLethalRadius)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<SprayCloudSnapshot> Snapshot(int tick)
        {
            var list = new List<SprayCloudSnapshot>();
            foreach (var cloud in _clouds)
            {
                list.Add(new SprayCloudSnapshot(cloud.Id, cloud.Position, Radius(cloud, tick), (tick - cloud.StartTick) * GameSimulation.DeltaTime));
            }

            return list;
        }
    }
}
