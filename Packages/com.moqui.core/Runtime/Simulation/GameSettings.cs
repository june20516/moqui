using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>tuning 값을 Core 시스템이 쓰는 명시적 DTO로 옮긴다. 키 이름은 spec/tuning.md와 같다.</summary>
    public sealed class GameSettings
    {
        private GameSettings(Tuning tuning)
        {
            Player = new PlayerSettings(tuning);
            Flight = new FlightSettings(tuning);
        }

        public PlayerSettings Player { get; }

        public FlightSettings Flight { get; }

        public static GameSettings FromTuning(Tuning tuning)
        {
            return new GameSettings(tuning);
        }
    }

    public sealed class PlayerSettings
    {
        public PlayerSettings(Tuning tuning)
        {
            CollisionRadius = tuning.GetFloat("player.collisionRadius");
        }

        public float CollisionRadius { get; }
    }

    public sealed class FlightSettings
    {
        public FlightSettings(Tuning tuning)
        {
            Speed = tuning.GetFloat("flight.speed");
            VerticalSpeed = tuning.GetFloat("flight.verticalSpeed");
            PrecisionSpeedMul = tuning.GetFloat("flight.precisionSpeedMul");
            AccelTime = tuning.GetFloat("flight.accelTime");
            DecelTime = tuning.GetFloat("flight.decelTime");
        }

        public float Speed { get; }

        public float VerticalSpeed { get; }

        public float PrecisionSpeedMul { get; }

        public float AccelTime { get; }

        public float DecelTime { get; }
    }
}
