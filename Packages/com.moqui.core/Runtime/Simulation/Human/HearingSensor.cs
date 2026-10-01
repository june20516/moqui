using System;
using System.Collections.Generic;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 귀 기준 청각 (spec/02 §2). 소리 거리는 가까운 귀까지의 거리이다.
    /// 공중에 있는 동안 비행 소음, 귀 근접 구역 추가 증가, 대시 소음 핑을 처리한다.
    /// </summary>
    public sealed class HearingSensor
    {
        private readonly NoiseSettings _noise;
        private readonly HearingSettings _hearing;

        public HearingSensor(NoiseSettings noise, HearingSettings hearing)
        {
            _noise = noise;
            _hearing = hearing;
        }

        public static bool IsAirborne(Player player)
        {
            return player.State == PlayerState.Flying || player.State == PlayerState.Dashing || player.State == PlayerState.Dislodged;
        }

        public float FlightNoiseRadius(Player player)
        {
            return player.PrecisionHeld ? _noise.FlightRadius * _noise.PrecisionRadiusMul : _noise.FlightRadius;
        }

        /// <summary>귀 거리 distance에서의 비행 소음 경계 증가율: flightAwarenessRate × lerp(nearMul, farMul, distance / radius).</summary>
        public float FlightNoiseRate(float distance, float radius)
        {
            float t = Math.Clamp(distance / radius, 0f, 1f);
            return _noise.FlightAwarenessRate * (_hearing.NearMul + ((_hearing.FarMul - _hearing.NearMul) * t));
        }

        public void Sense(Human human, Player player, IReadOnlyList<SimulationEvent> events, ref HumanPerception perception)
        {
            if (player.State != PlayerState.Dead && IsAirborne(player))
            {
                float distance = human.DistanceToNearestEar(player.Position);
                float radius = FlightNoiseRadius(player);
                if (radius > 0f && distance < radius)
                {
                    perception.HearingRate += FlightNoiseRate(distance, radius);
                    perception.AddStimulus(player.Position);
                }

                if (distance < _hearing.EarZoneRadius)
                {
                    perception.HearingRate += _hearing.EarZoneRate;
                    perception.InEarZone = true;
                    perception.AddStimulus(player.Position);
                }
            }

            foreach (var simulationEvent in events)
            {
                if (simulationEvent is NoiseEmitted noise && human.DistanceToNearestEar(noise.Position) <= noise.Radius)
                {
                    perception.InstantGain += noise.Awareness;
                    perception.AddStimulus(noise.Position);
                }
            }
        }
    }
}
