using System;
using System.Collections.Generic;
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
        private const float FleeMargin = 25f;

        /// <summary>손 경로(어깨 → 목표)가 이 거리(+ 손·몸 반경) 안을 지나면 위협 (D-052).</summary>
        private const float PathFleeMargin = 10f;

        /// <summary>휘두르는 손 경로에서 이 거리(+ 손·몸 반경)까지는 먼저 벗어난다.</summary>
        private const float StrikeAvoidMargin = 30f;

        private readonly Data.Tuning _tuning;
        private GameSettings _settings;

        /// <param name="tuning">기본 tuning. 시나리오의 스킬 구성을 반영해 실행마다 GameSettings를 만든다 (D-048).</param>
        public ScenarioRunner(Data.Tuning tuning)
        {
            _tuning = tuning;
        }

        /// <param name="onTick">틱마다 부르는 관찰 콜백 (진단, Unity 봇 재생). 선택.</param>
        public ScenarioResult Run(LevelDefinition level, ScenarioDefinition scenario, ulong seed, Action<GameSimulation, string> onTick = null)
        {
            var pilot = CreatePilot(scenario);
            var simulation = new GameSimulation(_settings, level.CreateSetup(scenario.Skills, seed));
            DeathCause? cause = null;

            while (simulation.Outcome == StageOutcome.InProgress && simulation.Tick < scenario.Expect.MaxTicks)
            {
                simulation.Step(pilot.Next(simulation));
                var died = simulation.Events.OfType<PlayerDied>().FirstOrDefault();
                if (died != null)
                {
                    cause = died.Cause;
                }

                pilot.Observe(simulation);
                onTick?.Invoke(simulation, pilot.Describe());
            }

            return new ScenarioResult(seed, simulation.Outcome, cause, simulation.Tick, simulation.Human?.FrenzyCount ?? 0, simulation.Human?.BiteMarkCount ?? 0, pilot.Flees);
        }

        /// <summary>
        /// 틱 단위로 봇을 조종한다 (Unity 안에서 시나리오를 재생할 때, verification §2 통합 스모크).
        /// 시뮬레이션은 스킬 반영 tuning(`SkillEffects.Apply(tuning, scenario.Skills)`)과 `level.CreateSetup(scenario.Skills, seed)`로 만들어야 헤드리스 결과와 같다.
        /// </summary>
        public Pilot CreatePilot(ScenarioDefinition scenario)
        {
            _settings = GameSettings.FromTuning(Meta.SkillEffects.Apply(_tuning, scenario.Skills));
            return new Pilot(this, scenario);
        }

        /// <summary>시나리오 하나의 진행 상태와 봇 판단. 틱마다 Next로 명령을 받고, Step 뒤 Observe를 부른다.</summary>
        public sealed class Pilot
        {
            private readonly ScenarioRunner _runner;
            private readonly ScenarioDefinition _scenario;
            private readonly RunState _state;

            internal Pilot(ScenarioRunner runner, ScenarioDefinition scenario)
            {
                _runner = runner;
                _scenario = scenario;
                _state = new RunState(scenario);
            }

            public int Flees => _state.Flees;

            public PlayerCommand Next(GameSimulation simulation)
            {
                return _runner.CompensateToxin(simulation, _runner.NextCommand(simulation, _scenario, _state));
            }

            /// <summary>방금 진행한 틱의 결과를 보고 도망칠지 정한다.</summary>
            public void Observe(GameSimulation simulation)
            {
                if (_scenario.Flee && !_state.Fleeing && ShouldFlee(simulation))
                {
                    _state.StartFlee();
                }
            }

            public string Describe() => _state.Describe(_scenario);
        }

        /// <summary>
        /// 중독 반전 단계(spec/06)를 아는 플레이어처럼 이동 입력을 미리 뒤집는다. 끊김·랜덤은 어쩔 수 없이 받는다.
        /// </summary>
        private PlayerCommand CompensateToxin(GameSimulation simulation, PlayerCommand command)
        {
            if (simulation.Player.Toxin >= _settings.Toxin.Tier2)
            {
                command.Move = -command.Move;
                command.Vertical = -command.Vertical;
            }

            return command;
        }

        /// <summary>
        /// 손 경로가 자기 근처를 지나는 공격 예고이거나 보였으면 도망친다. 취한 인간의 무작위 휘두르기처럼 먼 곳을 노리는 예고는 무시한다.
        /// </summary>
        private static bool ShouldFlee(GameSimulation simulation)
        {
            // 판정은 손이 지나가는 경로다 (D-052): 어깨 → 목표 손 경로가 내 근처를 지나면 위협으로 본다.
            var player = simulation.Player.Position;
            bool threatened = simulation.Events.OfType<AttackTelegraphStarted>()
                .Any(telegraph => BodyKinematics.DistanceToSegment(player, simulation.Human.NearestShoulder(telegraph.Target), telegraph.Target)
                    <= telegraph.Radius + simulation.Player.CollisionRadius + PathFleeMargin);
            return threatened || (simulation.Human != null && simulation.Human.PlayerVisible);
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

                    return BotPilot.FlyTo(player, step.Point, step.Precise);
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
            if (state.HideRoute == null)
            {
                state.HideRoute = scenario.HideRoutes.OrderBy(route => Vector3.Distance(route[0], player.Position)).First();
                state.HideIndex = 0;
            }

            Vector3 target = state.HideRoute[state.HideIndex];

            // 손이 휘둘러지는 동안 그 경로(어깨 → 목표) 가까이에 있으면 은신 경로보다 먼저 경로에서 멀어진다 (D-052).
            var attack = simulation.Human?.Attack;
            if (attack != null && (attack.Phase == AttackPhase.Telegraph || attack.Phase == AttackPhase.Active) && attack.Kind != AttackKind.Spray)
            {
                Vector3 shoulder = simulation.Human.NearestShoulder(attack.Target);
                Vector3 closest = ClosestOnSegment(player.Position, shoulder, attack.Target);
                if (Vector3.Distance(player.Position, closest) <= attack.Radius + player.CollisionRadius + StrikeAvoidMargin)
                {
                    Vector3 away = player.Position - closest;
                    away = away.LengthSquared() > 1e-4f ? Vector3.Normalize(away) : Vector3.UnitY;
                    command = BotPilot.FlyTo(player, player.Position + (away * StrikeAvoidMargin));
                    return false;
                }
            }

            command = BotPilot.FlyTo(player, target);
            if (!BotPilot.Arrived(player, target))
            {
                return false;
            }

            if (state.HideIndex < state.HideRoute.Count - 1)
            {
                state.HideIndex++;
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
                state.HideRoute = null;
            }

            return done;
        }

        private static Vector3 ClosestOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float lengthSquared = ab.LengthSquared();
            float t = lengthSquared > 1e-8f ? Math.Clamp(Vector3.Dot(point - a, ab) / lengthSquared, 0f, 1f) : 0f;
            return a + (ab * t);
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

            public IReadOnlyList<Vector3> HideRoute { get; set; }

            public int HideIndex { get; set; }

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
                    return $"flee->{(HideRoute != null ? HideRoute[HideIndex].ToString() : "?")}";
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
                if (_scenario.HideRoutes.Count == 0)
                {
                    return;
                }

                Fleeing = true;
                Flees++;
                HideRoute = null;
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
