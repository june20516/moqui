using System.Collections.Generic;
using Moqui.Core.Simulation;

namespace Moqui.Unity.Presentation.Audio
{
    /// <summary>반복음 하나의 목표 상태.</summary>
    public readonly struct LoopCue
    {
        public LoopCue(string id, bool playing, float pitch = 1f, float gain = 1f)
        {
            Id = id;
            Playing = playing;
            Pitch = pitch;
            Gain = gain;
        }

        public string Id { get; }

        public bool Playing { get; }

        public float Pitch { get; }

        /// <summary>카탈로그 음량에 곱하는 값 (0~1).</summary>
        public float Gain { get; }
    }

    /// <summary>
    /// 시뮬레이션 이벤트·상태 → 사운드 ID (spec/10). 표현 전용이며 규칙 판정은 하지 않는다.
    /// 공격 예고음(sfx_telegraph)은 HUD가 낸다 (HudPresenter).
    /// </summary>
    public sealed class AudioCues
    {
        /// <summary>날갯소리 피치: 정지 시 / 최고 속도에서 더하는 값.</summary>
        public const float WingPitchIdle = 0.85f;
        public const float WingPitchRange = 0.45f;

        /// <summary>바람 세기(바람 속도 ÷ fan.windSpeed)가 이 값을 넘으면 바람에 밀리는 것으로 본다 (M13).</summary>
        public const float WindAudibleStrength = 0.05f;

        /// <summary>바람 반복음: 가장 약할 때 음량·피치와 최대 세기에서 더하는 값.</summary>
        public const float WindGainMin = 0.3f;
        public const float WindPitchMin = 0.8f;
        public const float WindPitchRange = 0.4f;

        private AttackPhase _lastAttackPhase = AttackPhase.Idle;
        private int _lastToxinTier;
        private bool _decoyWasActive;
        private int _lastDropCount;
        private float _lastBreathPhase = -1f;
        private bool _wasInWind;

        /// <summary>이벤트 하나에 대응하는 효과음 (없으면 null).</summary>
        public static string FromEvent(SimulationEvent simulationEvent)
        {
            switch (simulationEvent)
            {
                case AwarenessStateChanged changed when changed.To == AwarenessState.Frenzy:
                    return AudioIds.Frenzy;
                case PlayerAttached _:
                    return AudioIds.Attach;
                case PlayerDetached _:
                    return AudioIds.Detach;
                case PlayerDislodged _:
                    return AudioIds.Dislodge;
                case DozeWakeTelegraph _:
                    return AudioIds.Wake;
                case PlayerTrapped _:
                case PlayerWebbed _:
                    return AudioIds.DropTrap;
                case PlayerEscapedDrop _:
                    return AudioIds.Escape;
                case SprayReleased _:
                    return AudioIds.Spray;
                default:
                    return null;
            }
        }

        public static int ToxinTier(float toxin, ToxinSettings settings)
        {
            if (toxin >= settings.Tier3)
            {
                return 3;
            }

            if (toxin >= settings.Tier2)
            {
                return 2;
            }

            return toxin >= settings.Tier1 ? 1 : 0;
        }

        /// <summary>틱마다 부른다: 이번 틱 이벤트와 상태 변화로 낼 효과음 목록.</summary>
        public List<string> OneShots(GameSimulation simulation)
        {
            var ids = new List<string>();
            foreach (var simulationEvent in simulation.Events)
            {
                string id = FromEvent(simulationEvent);
                if (id != null)
                {
                    ids.Add(id);
                }
            }

            var player = simulation.Player;
            // Step은 끝에서 Tick을 올리므로 방금 진행한 틱은 Tick - 1이다.
            if (player.LastDashStartTick == simulation.Tick - 1)
            {
                ids.Add(AudioIds.Dash);
            }

            int toxinTier = ToxinTier(player.Toxin, simulation.Settings.Toxin);
            if (toxinTier > _lastToxinTier)
            {
                ids.Add(AudioIds.Toxin);
            }

            _lastToxinTier = toxinTier;

            bool decoyActive = simulation.Decoy.IsActive(simulation.Tick);
            if (decoyActive && !_decoyWasActive)
            {
                ids.Add(AudioIds.Decoy);
            }

            _decoyWasActive = decoyActive;

            int dropCount = simulation.Water.Drops.Count;
            if (dropCount > _lastDropCount)
            {
                ids.Add(AudioIds.Drip);
            }

            _lastDropCount = dropCount;

            // 바람에 처음 밀리기 시작하면 "휙" 한 번 (M13).
            bool inWind = WindStrength(simulation) > WindAudibleStrength;
            if (inWind && !_wasInWind)
            {
                ids.Add(AudioIds.WindGust);
            }

            _wasInWind = inWind;

            var human = simulation.Human;
            if (human != null)
            {
                AddHumanCues(human, ids);
            }

            return ids;
        }

        /// <summary>프레임마다 맞출 반복음 상태.</summary>
        public static IEnumerable<LoopCue> Loops(GameSimulation simulation, MokiPose pose)
        {
            var player = simulation.Player;
            bool flying = pose == MokiPose.Idle || pose == MokiPose.Move || pose == MokiPose.Dash;
            yield return new LoopCue(AudioIds.WingLoop, flying, WingPitch(player.Velocity.Length(), simulation.Settings.Flight.Speed));
            yield return new LoopCue(AudioIds.SuckLoop, pose == MokiPose.Suck);
            yield return new LoopCue(AudioIds.Steam, player.InSteam);

            // 바람에 밀리는 동안: 세기에 따라 커지고 높아진다 (M13).
            float wind = WindStrength(simulation);
            yield return new LoopCue(AudioIds.WindLoop, wind > WindAudibleStrength, WindPitchMin + (WindPitchRange * wind), WindGainMin + ((1f - WindGainMin) * wind));

            var human = simulation.Human;
            yield return new LoopCue(AudioIds.FrenzyLoop, human != null && human.State == AwarenessState.Frenzy);
            yield return new LoopCue(AudioIds.Snore, human != null && human.IsAsleep);
        }

        /// <summary>플레이어를 미는 바람 세기 0~1 (바람 속도 ÷ fan.windSpeed).</summary>
        public static float WindStrength(GameSimulation simulation)
        {
            float windSpeed = simulation.Settings.Fan.WindSpeed;
            return windSpeed > 0f ? System.Math.Min(1f, simulation.Player.ExternalVelocity.Length() / windSpeed) : 0f;
        }

        public static float WingPitch(float speed, float flightSpeed)
        {
            float ratio = flightSpeed > 0f ? System.Math.Min(1f, speed / flightSpeed) : 0f;
            return WingPitchIdle + (WingPitchRange * ratio);
        }

        private void AddHumanCues(Human human, List<string> ids)
        {
            var attack = human.Attack;
            if (attack.Phase == AttackPhase.Active && _lastAttackPhase != AttackPhase.Active)
            {
                string hit = HitSound(attack.Kind);
                if (hit != null)
                {
                    ids.Add(hit);
                }
            }

            _lastAttackPhase = attack.Phase;

            // 숨소리: 호흡 주기가 한 바퀴 돌 때마다 (CO₂ 날숨 표현과 같은 주기). 자는 동안은 코골이 반복음이 대신한다.
            if (_lastBreathPhase >= 0f && human.BreathPhase < _lastBreathPhase && !human.IsAsleep)
            {
                ids.Add(AudioIds.Breath);
            }

            _lastBreathPhase = human.BreathPhase;
        }

        private static string HitSound(AttackKind kind)
        {
            switch (kind)
            {
                case AttackKind.Clap:
                    return AudioIds.Clap;
                case AttackKind.Spray:
                    return null;
                default:
                    return AudioIds.Slap;
            }
        }
    }
}
