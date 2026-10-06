using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Random;

namespace Moqui.Core.Simulation
{
    /// <summary>걷는 인간의 경로 (레벨 데이터 human.walk, spec/02 §9).</summary>
    public sealed class HumanWalkDefinition
    {
        public HumanWalkDefinition(IReadOnlyList<Vector2> route, FloatRange pause, bool loop)
        {
            if (route == null || route.Count == 0)
            {
                throw new ArgumentException("A walk route needs at least one point.", nameof(route));
            }

            Route = route;
            Pause = pause;
            Loop = loop;
        }

        /// <summary>경로점 (월드 x, z). 몸 루트 높이는 그대로 둔다.</summary>
        public IReadOnlyList<Vector2> Route { get; }

        /// <summary>경로점에서 멈추는 시간 (s).</summary>
        public FloatRange Pause { get; }

        /// <summary>끝 경로점 다음에 처음으로 돌아가는가. 아니면 마지막 점에 머문다.</summary>
        public bool Loop { get; }
    }

    /// <summary>걷기 수치 (spec/02 §9, tuning human.walk*).</summary>
    public sealed class WalkSettings
    {
        public WalkSettings(Tuning tuning)
        {
            WalkSpeed = tuning.GetFloat("human.walkSpeed");
            TurnSpeed = tuning.GetFloat("human.walkTurnSpeed");
            ChaseSpeed = tuning.GetFloat("human.chaseSpeed");
            Radius = tuning.GetFloat("human.walkRadius");
            StepLength = tuning.GetFloat("human.stepLength");
            LegSwing = tuning.GetFloat("human.legSwing");
        }

        public float WalkSpeed { get; }

        public float TurnSpeed { get; }

        public float ChaseSpeed { get; }

        public float Radius { get; }

        public float StepLength { get; }

        public float LegSwing { get; }
    }

    /// <summary>
    /// 걷는 인간 (spec/02 §9): 평온이면 경로를 돌고, 의심이면 멈추고, 광분 중 손이 닿지 않으면 마지막으로 본 위치로 쫓아간다.
    /// 몸을 먼저 돌리고(정면과 45° 안이면) 걷는다. 골반 높이 구 sweep으로 가구를 통과하지 않는다. 무작위 동작보다 먼저(플레이어 이동 전) 부른다.
    /// </summary>
    public sealed class HumanWalkSystem
    {
        /// <summary>경로점에 닿았다고 보는 거리 (u).</summary>
        public const float ArriveDistance = 5f;

        /// <summary>이 각 안이면 돌면서 걷는다 (°).</summary>
        public const float WalkWhileTurningAngle = 45f;

        private const float DegreesToRadians = MathF.PI / 180f;

        private readonly WalkSettings _settings;
        private readonly CollisionWorld _world;
        private readonly IRandom _random;

        public HumanWalkSystem(WalkSettings settings, CollisionWorld world, IRandom random)
        {
            _settings = settings;
            _world = world;
            _random = random;
        }

        public void Step(Human human, int tick, float deltaTime)
        {
            human.BeginMotionTick();
            var walk = human.Definition.Walk;
            if (walk == null)
            {
                return;
            }

            float speed = 0f;
            Vector3? target = null;
            if (!human.Attack.IsBusy)
            {
                target = Target(human, walk, tick, out speed);
            }

            human.IsChasing = target.HasValue && human.State == AwarenessState.Frenzy;
            float moved = target.HasValue ? MoveToward(human, target.Value, speed, deltaTime) : 0f;
            if (moved > 0f)
            {
                human.WalkPhase += moved / _settings.StepLength * MathF.PI;
                human.LegSwing = MathF.Sin(human.WalkPhase) * _settings.LegSwing;
            }
            else
            {
                // 멈추면 다리가 제자리로 돌아온다.
                human.LegSwing *= 0.8f;
            }

            human.UpdatePose();
        }

        private Vector3? Target(Human human, HumanWalkDefinition walk, int tick, out float speed)
        {
            speed = _settings.WalkSpeed;
            switch (human.State)
            {
                case AwarenessState.Frenzy:
                    speed = _settings.ChaseSpeed;
                    if (!human.HasSeenPlayer || HumanAttackSystem.CanReach(human, human.LastSeenPosition))
                    {
                        return null;
                    }

                    return new Vector3(human.LastSeenPosition.X, human.RootPosition.Y, human.LastSeenPosition.Z);
                case AwarenessState.Suspicious:
                    return null;
            }

            if (tick < human.WalkPauseEndTick)
            {
                return null;
            }

            Vector2 point = walk.Route[human.WalkWaypoint];
            var waypoint = new Vector3(point.X, human.RootPosition.Y, point.Y);
            if (Horizontal(waypoint - human.RootPosition).Length() > ArriveDistance)
            {
                return waypoint;
            }

            // 경로점에 닿았다: 잠깐 멈추고 다음 점으로.
            human.WalkPauseEndTick = tick + SimulationTime.ToTicks(_random.Range(walk.Pause.Min, walk.Pause.Max));
            int next = human.WalkWaypoint + 1;
            human.WalkWaypoint = next < walk.Route.Count ? next : walk.Loop ? 0 : human.WalkWaypoint;
            return null;
        }

        /// <summary>목표 쪽으로 몸을 돌리고 걷는다. 실제로 이동한 거리를 돌려준다.</summary>
        private float MoveToward(Human human, Vector3 target, float speed, float deltaTime)
        {
            Vector3 toTarget = Horizontal(target - human.RootPosition);
            float distance = toTarget.Length();
            if (distance < 1e-3f)
            {
                return 0f;
            }

            float desiredYaw = MathF.Atan2(toTarget.X, toTarget.Z) / DegreesToRadians;
            float delta = WrapDegrees(desiredYaw - human.BodyYaw);
            float turn = Math.Clamp(delta, -_settings.TurnSpeed * deltaTime, _settings.TurnSpeed * deltaTime);
            human.SetBodyYaw(human.BodyYaw + turn);
            if (MathF.Abs(delta - turn) > WalkWhileTurningAngle)
            {
                return 0f;
            }

            Vector3 direction = toTarget / distance;
            float step = Math.Min(speed * deltaTime, distance);
            if (_world.SphereSweep(human.RootPosition, _settings.Radius, direction, step, ShapeFlags.Obstacle, out var hit, ShapeFlags.Body))
            {
                step = Math.Max(0f, hit.Distance - SphereMover.Skin);
            }

            human.RootPosition += direction * step;
            return step;
        }

        private static Vector3 Horizontal(Vector3 vector) => new Vector3(vector.X, 0f, vector.Z);

        private static float WrapDegrees(float degrees)
        {
            degrees %= 360f;
            if (degrees > 180f)
            {
                degrees -= 360f;
            }
            else if (degrees < -180f)
            {
                degrees += 360f;
            }

            return degrees;
        }
    }
}
