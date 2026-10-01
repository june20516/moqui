using System;
using System.Numerics;
using Moqui.Core.Collision;
using Moqui.Core.Data;
using Moqui.Core.Simulation;

namespace Moqui.Core.Tests.Support
{
    public static class TestSimulations
    {
        private static readonly Lazy<Tuning> RepoTuning = new Lazy<Tuning>(() => TuningLoader.Load(FileSystemDataSource.ForRepoData()));

        public static Tuning Tuning => RepoTuning.Value;

        public static GameSettings Settings => GameSettings.FromTuning(Tuning);

        public static GameSimulation Empty(Vector3 spawn = default)
        {
            return new GameSimulation(Settings, new CollisionWorld(), spawn);
        }

        public static GameSimulation WithWorld(CollisionWorld world, Vector3 spawn)
        {
            return new GameSimulation(Settings, world, spawn);
        }

        public static int SecondsToTicks(float seconds)
        {
            return (int)MathF.Round(seconds * GameSimulation.TickRate);
        }

        public static void Run(GameSimulation simulation, PlayerCommand command, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                simulation.Step(command);
            }
        }

        public static PlayerCommand Forward => new PlayerCommand { Move = new Vector2(0f, 1f) };
    }
}
