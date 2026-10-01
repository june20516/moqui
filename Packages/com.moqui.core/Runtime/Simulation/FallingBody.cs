using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>중력을 받는 물체 (물방울 등, spec/00). 플레이어는 중력을 받지 않는다.</summary>
    public sealed class FallingBody
    {
        public FallingBody(string id, Vector3 position, float radius)
        {
            Id = id;
            Position = position;
            Radius = radius;
        }

        public string Id { get; }

        public Vector3 Position { get; set; }

        public Vector3 Velocity { get; set; }

        public float Radius { get; }
    }

    public sealed class FallingBodySystem
    {
        private readonly float _gravity;

        public FallingBodySystem(WorldSettings settings)
        {
            _gravity = settings.Gravity;
        }

        public void Step(FallingBody body, float deltaTime)
        {
            Vector3 previous = body.Velocity;
            body.Velocity = previous - (Vector3.UnitY * (_gravity * deltaTime));

            // 등가속도이므로 평균 속도 적분이 정확하다.
            body.Position += (previous + body.Velocity) * 0.5f * deltaTime;
        }
    }
}
