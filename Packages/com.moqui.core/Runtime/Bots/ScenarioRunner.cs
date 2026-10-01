using System;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Data.Levels;
using Moqui.Core.Simulation;

namespace Moqui.Core.Bots
{
    /// <summary>시나리오 한 번 실행한 결과.</summary>
    public sealed class ScenarioResult
    {
        public ScenarioResult(ulong seed, StageOutcome outcome, DeathCause? deathCause, int ticks, int frenzies, int biteMarks, int flees)
        {
            Seed = seed;
            Outcome = outcome;
            DeathCause = deathCause;
            Ticks = ticks;
            Frenzies = frenzies;
            BiteMarks = biteMarks;
            Flees = flees;
        }

        public ulong Seed { get; }

        public StageOutcome Outcome { get; }

        public DeathCause? DeathCause { get; }

        public int Ticks { get; }

        public int Frenzies { get; }

        public int BiteMarks { get; }

        public int Flees { get; }

        public bool Meets(ScenarioExpectation expect)
        {
            return Outcome == expect.Outcome
                && (!expect.DeathCause.HasValue || DeathCause == expect.DeathCause)
                && (!expect.MaxFrenzies.HasValue || Frenzies <= expect.MaxFrenzies.Value)
                && (!expect.MaxBiteMarks.HasValue || BiteMarks <= expect.MaxBiteMarks.Value)
                && Ticks <= expect.MaxTicks;
        }

        public override string ToString()
        {
            string cause = DeathCause.HasValue ? $"({DeathCause})" : string.Empty;
            return $"seed {Seed}: {Outcome}{cause} at {Ticks * GameSimulation.DeltaTime:F1}s, frenzies {Frenzies}, marks {BiteMarks}, flees {Flees}";
        }
    }

    /// <summary>
    /// 헤드리스 시나리오 재생 (tech/verification.md §3). 레벨을 시드로 만들고 단계를 실행해 결과를 낸다.
    /// </summary>
    public sealed class ScenarioRunner
    {
        private const float CalmMargin = 1f;
        private const float ApproachGap = 1f;

        private readonly GameSettings _settings;

        public ScenarioRunner(GameSettings settings)
        {
            _settings = settings;
        }

        /// <param name="onTick">틱마다 부르는 관찰 콜백 (진단, Unity 봇 재생). 선택.</param>
        public ScenarioResult Run(LevelDefinition level, ScenarioDefinition scenario, ulong seed, Action<GameSimulation, string> onTick = null)
        {
            var setup = new SimulationSetup(level.CreateWorld, level.PlayerSpawn, level.Human, seed, level.DripSources);
            var simulation = new GameSimulation(_settings, setup);
            var state = new RunState(scenario);
            DeathCause? cause = null;

            while (simulation.Outcome == StageOutcome.InProgress && simulation.Tick < scenario.Expect.MaxTicks)
            {
                simulation.Step(NextCommand(simulation, scenario, state));
                var died = simulation.Events.OfType<PlayerDied>().FirstOrDefault();
                if (died != null)
                {
                    cause = died.Cause;
                }

                if (scenario.Flee && !state.Fleeing && ShouldFlee(simulation))
                {
                    state.StartFlee();
                }

                onTick?.Invoke(simulation, state.Describe(scenario));
            }

            return new ScenarioResult(seed, simulation.Outcome, cause, simulation.Tick, simulation.Human?.FrenzyCount ?? 0, simulation.Human?.BiteMarkCount ?? 0, state.Flees);
        }

        private static bool ShouldFlee(GameSimulation simulation)
        {
            return simulation.Events.OfType<AttackTelegraphStarted>().Any() || (simulation.Human != null && simulation.Human.PlayerVisible);
        }

        private PlayerCommand NextCommand(GameSimulation simulation, ScenarioDefinition scenario, RunState state)
        {
            var player = simulation.Player;
            if (state.Fleeing)
            {
                if (Hide(simulation, scenario, state, out var fleeCommand))
                {
                    state.EndFlee();
                }

                return fleeCommand;
            }

            if (state.InStart && state.Index >= scenario.Start.Count)
            {
                state.LeaveStart();
            }

            if (!state.InStart && state.Index >= scenario.Steps.Count)
            {
                if (!scenario.Loop)
                {
                    return PlayerCommand.None;
                }

                state.Restart();
            }

            var step = state.InStart ? scenario.Start[state.Index] : scenario.Steps[state.Index];
            switch (step.Kind)
            {
                case ScenarioStepKind.FlyTo:
                    if (BotPilot.Arrived(player, step.Point))
                    {
                        state.Advance();
                    }

                    return BotPilot.FlyTo(player, step.Point);
                case ScenarioStepKind.AttachSite:
                    if (player.State == PlayerState.Attached)
                    {
                        state.Advance();
                        return PlayerCommand.None;
                    }

                    Vector3 approach = ApproachPoint(simulation, step.Part, state);
                    return player.State == PlayerState.Flying && BotPilot.Arrived(player, approach)
                        ? new PlayerCommand { AttachPressed = true }
                        : BotPilot.FlyTo(player, approach);
                case ScenarioStepKind.Suck:
                    if (player.State != PlayerState.Attached || !simulation.Human.TryGetSite(player.Anchor.Shape, out _))
                    {
                        state.StartFlee();
                        return PlayerCommand.None;
                    }

                    float session = player.SuckSession?.Amount ?? 0f;
                    if (session >= step.SessionAmount || player.BloodGauge >= step.GaugeTarget)
                    {
                        state.Advance();
                        return PlayerCommand.None;
                    }

                    return new PlayerCommand { SuckHeld = true };
                case ScenarioStepKind.Detach:
                    if (player.State != PlayerState.Attached)
                    {
                        state.Advance();
                        return PlayerCommand.None;
                    }

                    return new PlayerCommand { AttachPressed = true };
                case ScenarioStepKind.Hide:
                    if (Hide(simulation, scenario, state, out var hideCommand))
                    {
                        state.Advance();
                    }

                    return hideCommand;
                case ScenarioStepKind.Wait:
                    if (state.Tick(step.Seconds))
                    {
                        state.Advance();
                    }

                    return PlayerCommand.None;
                case ScenarioStepKind.Hover:
                    if (BotPilot.Arrived(player, step.Point) && state.Tick(step.Seconds))
                    {
                        state.Advance();
                    }

                    return BotPilot.FlyTo(player, step.Point);
                default:
                    throw new InvalidOperationException($"Unknown step {step.Kind}");
            }
        }

        /// <summary>가장 가까운 숨는 곳으로 가서, 경계가 평온(또는 자국 하한 근처)으로 내려가고 공격이 끝나면 참.</summary>
        private bool Hide(GameSimulation simulation, ScenarioDefinition scenario, RunState state, out PlayerCommand command)
        {
            var player = simulation.Player;
            if (state.HideTarget == null)
            {
                state.HideTarget = scenario.HideSpots.OrderBy(spot => Vector3.Distance(spot, player.Position)).First();
            }

            Vector3 target = state.HideTarget.Value;
            command = BotPilot.FlyTo(player, target);
            if (!BotPilot.Arrived(player, target))
            {
                return false;
            }

            var human = simulation.Human;
            if (human == null)
            {
                return true;
            }

            float floor = _settings.BiteMark.Floor(human.BiteMarkCount);
            bool calm = human.State == AwarenessState.Safe || (human.State != AwarenessState.Frenzy && human.Awareness <= floor + CalmMargin);
            bool done = calm && !human.Attack.IsBusy;
            if (done)
            {
                state.HideTarget = null;
            }

            return done;
        }

        /// <summary>부위 표면에서 봇 쪽으로 1u 떨어진 점. 회차마다 처음 계산한 쪽을 유지한다.</summary>
        private Vector3 ApproachPoint(GameSimulation simulation, string partId, RunState state)
        {
            var shape = simulation.Human.Shapes[partId];
            if (state.ApproachSide == null)
            {
                // 캡슐 축 방향 성분을 빼서 부위 가운데 바깥면으로 접근한다. 끝으로 가면 이웃 부위(옷 입은 위팔 등)에 붙을 수 있다.
                Vector3 side = simulation.Player.Position - shape.Center;
                if (shape.Type == ShapeType.Capsule)
                {
                    Vector3 axis = Vector3.Normalize(shape.PointB - shape.PointA);
                    side -= axis * Vector3.Dot(side, axis);
                }

                state.ApproachSide = side;
            }

            var surface = ShapeGeometry.Closest(shape, shape.Center + state.ApproachSide.Value);
            return surface.Point + (surface.Normal * ApproachGap);
        }

        private sealed class RunState
        {
            private readonly ScenarioDefinition _scenario;
            private int _timerTicks;

            public RunState(ScenarioDefinition scenario)
            {
                _scenario = scenario;
            }

            public int Index { get; private set; }

            /// <summary>start 단계를 실행하는 중인가 (처음 한 번).</summary>
            public bool InStart { get; private set; } = true;

            public void LeaveStart()
            {
                InStart = false;
                Restart();
            }

            public bool Fleeing { get; private set; }

            public int Flees { get; private set; }

            public Vector3? HideTarget { get; set; }

            public Vector3? ApproachSide { get; set; }

            public void Advance()
            {
                Index++;
                _timerTicks = 0;
                ApproachSide = null;
            }

            public void Restart()
            {
                Index = 0;
                _timerTicks = 0;
                ApproachSide = null;
            }

            public string Describe(ScenarioDefinition scenario)
            {
                if (Fleeing)
                {
                    return $"flee->{HideTarget}";
                }

                var steps = InStart ? scenario.Start : scenario.Steps;
                return Index < steps.Count ? $"{(InStart ? "start" : "loop")}[{Index}] {steps[Index].Kind} {steps[Index].Part}" : "done";
            }

            /// <summary>단계 타이머를 1틱 진행하고 seconds가 지났으면 참.</summary>
            public bool Tick(float seconds)
            {
                _timerTicks++;
                return _timerTicks >= SimulationTime.ToTicks(seconds);
            }

            public void StartFlee()
            {
                if (_scenario.HideSpots.Count == 0)
                {
                    return;
                }

                Fleeing = true;
                Flees++;
                HideTarget = null;
            }

            public void EndFlee()
            {
                Fleeing = false;
                InStart = false;
                Restart();
            }
        }
    }
}
