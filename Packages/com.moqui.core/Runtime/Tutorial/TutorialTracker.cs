using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Moqui.Core.Data;
using Moqui.Core.Simulation;

namespace Moqui.Core.Tutorial
{
    /// <summary>튜토리얼 진행 수치 (spec/tuning.md tutorial).</summary>
    public sealed class TutorialSettings
    {
        public TutorialSettings(Tuning tuning)
        {
            HoldSeconds = tuning.GetFloat("tutorial.holdSeconds");
            LookDegrees = tuning.GetFloat("tutorial.lookDegrees");
            InfoTimeout = tuning.GetFloat("tutorial.infoTimeout");
            Co2ReachRange = tuning.GetFloat("senses.heatRange");
        }

        /// <summary>이동·정밀 비행 안내를 끝내는 입력 유지 시간.</summary>
        public float HoldSeconds { get; }

        /// <summary>시점 안내를 끝내는 누적 회전 각도.</summary>
        public float LookDegrees { get; }

        /// <summary>설명형 안내(Stage 2)가 행동 없이도 넘어가는 시간.</summary>
        public float InfoTimeout { get; }

        /// <summary>CO₂를 따라가 피부 부위에 이만큼 다가가면 완료 (체온이 보이는 거리).</summary>
        public float Co2ReachRange { get; }
    }

    /// <summary>
    /// 튜토리얼 안내 진행 (spec/08 §튜토리얼, spec/07 안내 순서). 현재 안내의 행동만 판정하고, 하면 다음으로 넘어간다.
    /// 틱마다 Observe를 부른다. 판정은 시뮬레이션 이벤트·상태와 그 틱의 입력 커맨드로 한다 (D-042).
    /// </summary>
    public sealed class TutorialTracker
    {
        private static readonly HashSet<string> ActionSteps = new HashSet<string>
        {
            "move", "look", "precision", "dash", "attach", "hide", "co2", "suck", "detach",
        };

        /// <summary>행동으로 끝나지만 InfoTimeout이 지나면 넘어가는 설명형 안내.</summary>
        private static readonly HashSet<string> InfoSteps = new HashSet<string>
        {
            "visionZones", "frenzyHide", "reactionDodge", "flightMode",
        };

        private readonly IReadOnlyList<string> _steps;
        private readonly TutorialSettings _settings;
        private float _stepSeconds;
        private float _heldSeconds;
        private float _lookedDegrees;
        private float? _lastYaw;
        private float? _lastPitch;
        private int _lastDashTick = Player.NeverTick;

        public TutorialTracker(IReadOnlyList<string> steps, TutorialSettings settings, int startIndex = 0)
        {
            var unknown = steps.Where(step => !IsKnownStep(step)).ToList();
            if (unknown.Count > 0)
            {
                throw new ArgumentException($"Unknown tutorial steps: {string.Join(", ", unknown)}", nameof(steps));
            }

            _steps = steps;
            _settings = settings;
            CompletedCount = Math.Max(0, startIndex);
        }

        public int CompletedCount { get; private set; }

        public bool IsComplete => CompletedCount >= _steps.Count;

        /// <summary>지금 보여 줄 안내 ID. 모두 끝났으면 null.</summary>
        public string CurrentStep => IsComplete ? null : _steps[CompletedCount];

        public static bool IsKnownStep(string step)
        {
            return ActionSteps.Contains(step) || InfoSteps.Contains(step);
        }

        public void Observe(GameSimulation simulation)
        {
            string step = CurrentStep;
            if (step == null)
            {
                return;
            }

            _stepSeconds += GameSimulation.DeltaTime;
            bool done = IsDone(step, simulation) || (InfoSteps.Contains(step) && _stepSeconds >= _settings.InfoTimeout);
            TrackLook(simulation.LastCommand);
            _lastDashTick = simulation.Player.LastDashStartTick;
            if (done)
            {
                CompletedCount++;
                _stepSeconds = 0f;
                _heldSeconds = 0f;
                _lookedDegrees = 0f;
            }
        }

        private bool IsDone(string step, GameSimulation simulation)
        {
            var player = simulation.Player;
            var command = simulation.LastCommand;
            var human = simulation.Human;
            bool moving = command.Move.LengthSquared() > 0f || command.Vertical != 0f;
            switch (step)
            {
                case "move":
                    return Hold(moving && player.State == PlayerState.Flying);
                case "look":
                    return _lookedDegrees + LookDelta(command) >= _settings.LookDegrees;
                case "precision":
                    return Hold(moving && command.PrecisionHeld && player.State == PlayerState.Flying);
                case "dash":
                    return player.LastDashStartTick != _lastDashTick && player.LastDashStartTick != Player.NeverTick;
                case "attach":
                    return simulation.Events.OfType<PlayerAttached>().Any();
                case "hide":
                    return player.IsHidden;
                case "co2":
                    return human != null && human.SkinSites.Any(site => Vector3.Distance(site.Shape.Center, player.Position) <= _settings.Co2ReachRange);
                case "suck":
                    return player.SuckSession != null;
                case "detach":
                case "reactionDodge":
                    return simulation.Events.OfType<PlayerDetached>().Any();
                case "visionZones":
                    return human != null && (human.PlayerVisible || human.PlayerOccluded);
                case "frenzyHide":
                    return human != null && human.State == AwarenessState.Frenzy && player.IsHidden;
                default:
                    return false;
            }
        }

        private bool Hold(bool active)
        {
            _heldSeconds = active ? _heldSeconds + GameSimulation.DeltaTime : _heldSeconds;
            return _heldSeconds >= _settings.HoldSeconds - (GameSimulation.DeltaTime * 0.5f);
        }

        private float LookDelta(PlayerCommand command)
        {
            if (!_lastYaw.HasValue)
            {
                return 0f;
            }

            return Math.Abs(DeltaAngle(_lastYaw.Value, command.LookYaw)) + Math.Abs(command.LookPitch - _lastPitch.Value);
        }

        private void TrackLook(PlayerCommand command)
        {
            _lookedDegrees += LookDelta(command);
            _lastYaw = command.LookYaw;
            _lastPitch = command.LookPitch;
        }

        private static float DeltaAngle(float from, float to)
        {
            float delta = (to - from) % 360f;
            if (delta > 180f)
            {
                delta -= 360f;
            }
            else if (delta < -180f)
            {
                delta += 360f;
            }

            return delta;
        }
    }
}
