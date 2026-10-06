using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>미끼 마법 수치 (spec/tuning.md skill.decoy).</summary>
    public sealed class DecoySettings
    {
        public DecoySettings(Tuning tuning)
        {
            Range = tuning.GetFloat("skill.decoy.range");
            Duration = tuning.GetFloat("skill.decoy.duration");
            NoiseRadius = tuning.GetFloat("skill.decoy.noiseRadius");
            NoiseRate = tuning.GetFloat("skill.decoy.noiseRate");
            Cooldowns = tuning.GetFloats("skill.decoy.cooldown");
        }

        public float Range { get; }

        public float Duration { get; }

        public float NoiseRadius { get; }

        /// <summary>반경 안 인간 경계 증가율 (/s).</summary>
        public float NoiseRate { get; }

        /// <summary>레벨별 쿨타임 (1레벨부터).</summary>
        public IReadOnlyList<float> Cooldowns { get; }
    }

    /// <summary>
    /// 미끼 마법 (spec/09 액티브): Skill 입력 시 조준 방향 range 지점(벽에 막히면 그 앞)에 duration 동안 날갯소리 미끼를 둔다.
    /// 미끼는 매 틱 소음 이벤트(반경 noiseRadius, 경계 noiseRate × Δt)를 내고, 인간은 이를 진짜 소음처럼 듣는다(마지막 자극 위치 = 미끼).
    /// </summary>
    public sealed class DecoySystem
    {
        private const float WallGap = 1f;

        private readonly DecoySettings _settings;
        private readonly CollisionWorld _world;
        private readonly int _level;
        private int _endTick = Player.NeverTick;
        private int _readyTick;

        public DecoySystem(DecoySettings settings, CollisionWorld world, int level)
        {
            _settings = settings;
            _world = world;
            _level = level;
        }

        public bool IsAvailable => _level > 0;

        public bool IsActive(int tick) => tick < _endTick;

        public Vector3 Position { get; private set; }

        /// <summary>다시 쓸 수 있을 때까지 남은 초 (HUD 액티브 스킬 쿨타임).</summary>
        public float CooldownRemaining(int tick) => Math.Max(0, _readyTick - tick) * GameSimulation.DeltaTime;

        public void Step(Player player, in PlayerCommand command, int tick, List<SimulationEvent> events)
        {
            if (IsAvailable && command.SkillPressed && tick >= _readyTick && player.State != PlayerState.Dead)
            {
                Place(player, command, tick);
            }

            if (IsActive(tick))
            {
                events.Add(new NoiseEmitted(tick, NoiseSource.Decoy, Position, _settings.NoiseRadius, _settings.NoiseRate * GameSimulation.DeltaTime));
            }
        }

        private void Place(Player player, in PlayerCommand command, int tick)
        {
            Vector3 aim = AimDirection(command.LookYaw, command.LookPitch);
            float distance = _settings.Range;
            if (_world.Raycast(player.Position, aim, _settings.Range, ShapeFlags.Solid, out var hit, ShapeFlags.Body))
            {
                distance = Math.Max(0f, hit.Distance - WallGap);
            }

            Position = player.Position + (aim * distance);
            _endTick = tick + SimulationTime.ToTicks(_settings.Duration);
            int cooldownIndex = Math.Min(_level, _settings.Cooldowns.Count) - 1;
            _readyTick = tick + SimulationTime.ToTicks(_settings.Cooldowns[cooldownIndex]);
        }

        /// <summary>카메라 각도(도)의 시선 방향. 피치 +는 위.</summary>
        public static Vector3 AimDirection(float yawDegrees, float pitchDegrees) => CameraBasis.Aim(yawDegrees, pitchDegrees);
    }
}
