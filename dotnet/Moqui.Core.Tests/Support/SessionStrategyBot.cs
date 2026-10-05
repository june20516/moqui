using System;
using System.Linq;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Simulation;

namespace Moqui.Core.Tests.Support
{
    /// <summary>
    /// 설계 검증용 봇 (spec/04 수용 기준: 긴 세션 2회 vs 짧은 세션 6회).
    /// 비행 커맨드만 써서 숨는 곳 ↔ 피부 부위를 오가며, 세션마다 정해진 양을 빤다.
    /// 접근할 때는 인간 시선에서 가장 먼 부위를 고르고, 시선 원뿔 근처면 기다린다.
    /// 예고가 뜨거나 인간에게 보이면 즉시 이탈해 가까운 Shadow Zone으로 숨고, 경계가 더 내려가지 않을 때까지 기다린다.
    /// 무대는 CreateWorld: 인간 양쪽 허리 옆에 Shadow Zone (spec/07: 모든 SkinSite 근처에 숨을 곳 보장).
    /// </summary>
    public sealed class SessionStrategyBot
    {
        private const float ArriveDistance = 0.8f;
        private const float PrecisionDistance = 12f;
        private const float VerticalDeadZone = 0.5f;
        private const float ApproachGap = 1f;
        private const float RestMargin = 1f;
        private const float GazeMargin = 15f;
        private const float RadiansToDegrees = 180f / MathF.PI;

        /// <summary>인간 오른쪽 허리 옆 Shadow Zone 중심 (spec/07 "도망칠 곳 보장"). 귀까지 77u라 비행 소음 밖.</summary>
        public static readonly Vector3 HideSpot = new Vector3(40f, 45f, 15f);

        public static readonly Vector3 ShadowHalfSize = new Vector3(10f, 15f, 25f);

        public static readonly Vector3 LeftHideSpot = new Vector3(-40f, 45f, 15f);

        private static readonly string[] Sites = { "forearmR", "calfR", "forearmL", "calfL" };

        /// <summary>설계 검증 무대: 인간 양쪽 허리 옆 Shadow Zone 두 개.</summary>
        public static CollisionWorld CreateWorld()
        {
            var world = new CollisionWorld();
            world.Add(CollisionShape.Box("shadowRight", HideSpot, ShadowHalfSize, ShapeFlags.ShadowZone));
            world.Add(CollisionShape.Box("shadowLeft", LeftHideSpot, ShadowHalfSize, ShapeFlags.ShadowZone));
            return world;
        }

        private readonly GameSimulation _simulation;
        private readonly float _amountPerSession;
        private Phase _phase = Phase.Rest;
        private string _site;

        /// <summary>접근 지점으로 곧장 가는 길이 몸에 막히면 거쳐 가는 점.</summary>
        private Vector3? _waypoint;

        public SessionStrategyBot(GameSimulation simulation, int sessions)
        {
            _simulation = simulation;
            _amountPerSession = SuckSystem.GaugeMax / sessions;
        }

        private enum Phase
        {
            Approach,
            Suck,
            Retreat,
            Rest,
        }

        public int Flees { get; private set; }

        private Vector3 CurrentHideSpot => _site != null && _site.EndsWith("L", StringComparison.Ordinal) ? LeftHideSpot : HideSpot;

        public StrategyResult Run(int maxTicks)
        {
            double awarenessSum = 0;
            int ticks = 0;
            while (ticks < maxTicks && _simulation.Outcome == StageOutcome.InProgress)
            {
                _simulation.Step(NextCommand());
                awarenessSum += _simulation.Human.Awareness;
                ticks++;
                bool freezing = _simulation.Player.State == PlayerState.Attached && Moqui.Core.Bots.BotPilot.ShouldFreeze(_simulation.Human);
                if (_simulation.Events.OfType<AttackTelegraphStarted>().Any() || (_simulation.Human.PlayerVisible && !freezing))
                {
                    Flee();
                }
            }

            return new StrategyResult(_simulation.Outcome, ticks, awarenessSum / Math.Max(1, ticks), _simulation.Human.BiteMarkCount, Flees);
        }

        private PlayerCommand NextCommand()
        {
            var player = _simulation.Player;
            switch (_phase)
            {
                case Phase.Approach:
                    Vector3 approach = ApproachPoint(_site);
                    if (GazeAngle(approach) < _simulation.Settings.Vision.YellowHalfAngle + GazeMargin)
                    {
                        Flee();
                        return FlyTo(CurrentHideSpot);
                    }

                    if (player.State == PlayerState.Flying && Vector3.Distance(player.Position, approach) < ArriveDistance)
                    {
                        _phase = Phase.Suck;
                        return new PlayerCommand { AttachPressed = true };
                    }

                    return FlyTo(Route(approach));
                case Phase.Suck:
                    if (player.State != PlayerState.Attached)
                    {
                        _phase = Phase.Approach;
                        return PlayerCommand.None;
                    }

                    var session = player.SuckSession;
                    float remaining = SuckSystem.GaugeMax - player.BloodGauge;
                    if (session != null && session.Amount >= Math.Min(_amountPerSession, remaining) - 1e-3f)
                    {
                        _phase = Phase.Retreat;
                        return new PlayerCommand { AttachPressed = true };
                    }

                    // 시선 이벤트: 흡혈을 멈추고 얼어 있는다 (D-056).
                    return Moqui.Core.Bots.BotPilot.ShouldFreeze(_simulation.Human) ? PlayerCommand.None : new PlayerCommand { SuckHeld = true };
                case Phase.Retreat:
                    if (Vector3.Distance(player.Position, CurrentHideSpot) < ArriveDistance)
                    {
                        _phase = Phase.Rest;
                        return PlayerCommand.None;
                    }

                    return FlyTo(CurrentHideSpot);
                default:
                    var human = _simulation.Human;
                    float floor = _simulation.Settings.BiteMark.Floor(human.BiteMarkCount);
                    bool settled = human.State == AwarenessState.Safe || human.Awareness <= floor + RestMargin;
                    if (settled && human.State != AwarenessState.Frenzy && !human.Attack.IsBusy && TryPickSite(out string site))
                    {
                        _site = site;
                        _waypoint = null;
                        _phase = Phase.Approach;
                    }

                    return PlayerCommand.None;
            }
        }

        /// <summary>시선에서 가장 먼 부위. 그 부위도 시선 원뿔 근처면 고르지 않는다.</summary>
        private bool TryPickSite(out string site)
        {
            float halfAngle = _simulation.Settings.Vision.YellowHalfAngle;
            var best = Sites.Select(id => (id, angle: GazeAngle(ApproachPoint(id)))).OrderByDescending(s => s.angle).First();
            site = best.id;
            return best.angle >= halfAngle + GazeMargin;
        }

        private float GazeAngle(Vector3 point)
        {
            var human = _simulation.Human;
            Vector3 toPoint = Vector3.Normalize(point - human.HeadCenter);
            float cos = Vector3.Dot(toPoint, Vector3.Normalize(human.HeadForward));
            return MathF.Acos(Math.Clamp(cos, -1f, 1f)) * RadiansToDegrees;
        }

        private void Flee()
        {
            if (_phase == Phase.Retreat || _phase == Phase.Rest)
            {
                return;
            }

            Flees++;
            _phase = Phase.Retreat;
        }

        /// <summary>곧장 가는 길이 막히면 막히지 않는 경유점(위쪽·같은 쪽 은신처·앞쪽)을 거쳐 간다.</summary>
        private Vector3 Route(Vector3 target)
        {
            Vector3 position = _simulation.Player.Position;
            if (_waypoint.HasValue)
            {
                if (Vector3.Distance(position, _waypoint.Value) >= ArriveDistance * 2f)
                {
                    return _waypoint.Value;
                }

                _waypoint = null;
            }

            if (_simulation.Player.State != PlayerState.Flying || PathClear(position, target))
            {
                return target;
            }

            Vector3[] candidates =
            {
                target + (Vector3.UnitY * 40f),
                CurrentHideSpot,
                new Vector3(target.X, target.Y, 50f),
                new Vector3(target.X, target.Y + 40f, 50f),
            };
            foreach (var candidate in candidates)
            {
                if (PathClear(position, candidate) && PathClear(candidate, target))
                {
                    _waypoint = candidate;
                    return candidate;
                }
            }

            return target;
        }

        private bool PathClear(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float distance = delta.Length();
            return distance < 1e-3f
                || !_simulation.World.SphereSweep(from, _simulation.Player.CollisionRadius, delta / distance, distance, ShapeFlags.Solid, out _);
        }

        private Vector3 ApproachPoint(string partId)
        {
            var shape = _simulation.Human.Shapes[partId];
            Vector3 outward = partId.EndsWith("L", StringComparison.Ordinal) ? -Vector3.UnitX : Vector3.UnitX;
            var surface = ShapeGeometry.Closest(shape, shape.Center + (outward * 50f));
            return surface.Point + (surface.Normal * ApproachGap);
        }

        private PlayerCommand FlyTo(Vector3 target)
        {
            var player = _simulation.Player;
            if (player.State == PlayerState.Attached)
            {
                // 이동 입력으로 이탈한다 (spec/03).
                return new PlayerCommand { Vertical = 1f };
            }

            Vector3 delta = target - player.Position;
            var horizontal = new Vector2(delta.X, delta.Z);
            float distance = delta.Length();
            if (distance < ArriveDistance)
            {
                return PlayerCommand.None;
            }

            float yaw = MathF.Atan2(delta.X, delta.Z) * RadiansToDegrees;
            float vertical = MathF.Abs(delta.Y) > VerticalDeadZone ? MathF.Sign(delta.Y) : 0f;
            return new PlayerCommand
            {
                LookYaw = yaw,
                Move = horizontal.Length() > VerticalDeadZone ? new Vector2(0f, 1f) : Vector2.Zero,
                Vertical = vertical,
                PrecisionHeld = distance < PrecisionDistance,
            };
        }
    }

    public readonly struct StrategyResult
    {
        public StrategyResult(StageOutcome outcome, int ticks, double averageAwareness, int biteMarks, int flees)
        {
            Outcome = outcome;
            Ticks = ticks;
            AverageAwareness = averageAwareness;
            BiteMarks = biteMarks;
            Flees = flees;
        }

        public StageOutcome Outcome { get; }

        public int Ticks { get; }

        public double AverageAwareness { get; }

        public int BiteMarks { get; }

        public int Flees { get; }
    }
}
