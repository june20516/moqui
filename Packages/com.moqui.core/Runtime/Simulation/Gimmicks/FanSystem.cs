using System;
using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>선풍기 수치 (spec/tuning.md fan).</summary>
    public sealed class FanSettings
    {
        public FanSettings(Tuning tuning)
        {
            WindSpeed = tuning.GetFloat("fan.windSpeed");
            Range = tuning.GetFloat("fan.range");
            HalfAngle = tuning.GetFloat("fan.halfAngle");
            OscillationAngle = tuning.GetFloat("fan.oscillationAngle");
            OscillationPeriod = tuning.GetFloat("fan.oscillationPeriod");
            NoiseMaskRadius = tuning.GetFloat("fan.noiseMaskRadius");
            NoiseMaskMul = tuning.GetFloat("fan.noiseMaskMul");
            AirconWindSpeed = tuning.GetFloat("aircon.windSpeed");
            AirconRange = tuning.GetFloat("aircon.range");
            AirconHalfAngle = tuning.GetFloat("aircon.halfAngle");
        }

        /// <summary>에어컨 바람 (M14): 속도, 도달 거리, 반각.</summary>
        public float AirconWindSpeed { get; }

        public float AirconRange { get; }

        public float AirconHalfAngle { get; }

        public float WindSpeedOf(FanDefinition fan) => fan.Kind == FanKind.AirConditioner ? AirconWindSpeed : WindSpeed;

        public float RangeOf(FanDefinition fan) => fan.Kind == FanKind.AirConditioner ? AirconRange : Range;

        public float HalfAngleOf(FanDefinition fan) => fan.Kind == FanKind.AirConditioner ? AirconHalfAngle : HalfAngle;

        public float WindSpeed { get; }

        public float Range { get; }

        public float HalfAngle { get; }

        public float OscillationAngle { get; }

        public float OscillationPeriod { get; }

        public float NoiseMaskRadius { get; }

        public float NoiseMaskMul { get; }
    }

    /// <summary>선풍기 하나의 현재 바람 원뿔 (HUD·표현·CO₂ 흩어짐용 스냅샷).</summary>
    public sealed class FanSnapshot
    {
        public FanSnapshot(string id, Vector3 position, Vector3 direction, float range, float halfAngle, FanKind kind = FanKind.Fan, bool isOn = true)
        {
            Id = id;
            Position = position;
            Direction = direction;
            Range = range;
            HalfAngle = halfAngle;
            Kind = kind;
            IsOn = isOn;
        }

        public FanKind Kind { get; }

        /// <summary>지금 바람이 나오는가 (에어컨 주기).</summary>
        public bool IsOn { get; }

        public string Id { get; }

        public Vector3 Position { get; }

        public Vector3 Direction { get; }

        public float Range { get; }

        public float HalfAngle { get; }
    }

    /// <summary>
    /// 선풍기 (spec/06): 머리가 fan.oscillationPeriod 주기로 ±fan.oscillationAngle을 오가고(사인), 바람 원뿔(반경 fan.range,
    /// 반각 fan.halfAngle) 안의 점에 바람 방향 fan.windSpeed를 준다. 마스킹 반경 안의 소음 반경은 fan.noiseMaskMul배.
    /// </summary>
    public sealed class FanSystem
    {
        private const float DegreesToRadians = MathF.PI / 180f;

        private readonly FanSettings _settings;
        private readonly IReadOnlyList<FanDefinition> _fans;

        public FanSystem(FanSettings settings, IReadOnlyList<FanDefinition> fans)
        {
            _settings = settings;
            _fans = fans;
        }

        public FanSettings Settings => _settings;

        public IReadOnlyList<FanDefinition> Fans => _fans;

        /// <summary>틱 tick에서 선풍기 머리 방향(도).</summary>
        /// <summary>지금 켜져 있는가 (주기가 없으면 늘 켜짐).</summary>
        public bool IsOn(FanDefinition fan, int tick)
        {
            return fan.Schedule == null || fan.Schedule.IsOn(tick * GameSimulation.DeltaTime);
        }

        public float HeadYaw(FanDefinition fan, int tick)
        {
            if (fan.Kind == FanKind.AirConditioner)
            {
                return fan.Yaw;
            }

            float seconds = tick * GameSimulation.DeltaTime;
            return fan.Yaw + (_settings.OscillationAngle * MathF.Sin(2f * MathF.PI * seconds / _settings.OscillationPeriod));
        }

        public Vector3 Direction(FanDefinition fan, int tick)
        {
            float yaw = HeadYaw(fan, tick) * DegreesToRadians;
            float pitch = fan.Pitch * DegreesToRadians;
            return new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
        }

        public bool InCone(FanDefinition fan, Vector3 point, int tick)
        {
            if (!IsOn(fan, tick))
            {
                return false;
            }

            Vector3 offset = point - fan.Position;
            float distance = offset.Length();
            if (distance > _settings.RangeOf(fan))
            {
                return false;
            }

            if (distance <= 0f)
            {
                return true;
            }

            float cosine = Vector3.Dot(offset / distance, Direction(fan, tick));
            return cosine >= MathF.Cos(_settings.HalfAngleOf(fan) * DegreesToRadians);
        }

        /// <summary>점에 걸리는 바람 속도의 합.</summary>
        public Vector3 WindAt(Vector3 point, int tick)
        {
            Vector3 wind = Vector3.Zero;
            foreach (var fan in _fans)
            {
                if (InCone(fan, point, tick))
                {
                    wind += Direction(fan, tick) * _settings.WindSpeedOf(fan);
                }
            }

            return wind;
        }

        public bool InAnyCone(Vector3 point, int tick)
        {
            foreach (var fan in _fans)
            {
                if (InCone(fan, point, tick))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>소음 반경 배율: 켜진 선풍기·에어컨의 마스킹 반경 안이면 fan.noiseMaskMul.</summary>
        public float NoiseMultiplier(Vector3 point, int tick)
        {
            foreach (var fan in _fans)
            {
                if (IsOn(fan, tick) && Vector3.Distance(point, fan.Position) <= _settings.NoiseMaskRadius)
                {
                    return _settings.NoiseMaskMul;
                }
            }

            return 1f;
        }

        public IReadOnlyList<FanSnapshot> Snapshot(int tick)
        {
            var list = new List<FanSnapshot>();
            foreach (var fan in _fans)
            {
                list.Add(new FanSnapshot(fan.Id, fan.Position, Direction(fan, tick), _settings.RangeOf(fan), _settings.HalfAngleOf(fan), fan.Kind, IsOn(fan, tick)));
            }

            return list;
        }
    }
}
