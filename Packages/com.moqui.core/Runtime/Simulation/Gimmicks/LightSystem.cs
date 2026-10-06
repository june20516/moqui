using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Data;

namespace Moqui.Core.Simulation
{
    /// <summary>조명 수치 (spec/06 조명 스위치, M14).</summary>
    public sealed class LightSettings
    {
        public LightSettings(Tuning tuning)
        {
            VisionMul = tuning.GetFloat("light.visionMul");
            SwitchDelay = tuning.GetFloat("light.switchDelay");
            OffDelay = tuning.GetFloat("light.offDelay");
        }

        /// <summary>켜진 영역 안 플레이어에 대한 시각 증가 배율.</summary>
        public float VisionMul { get; }

        /// <summary>인간이 의심·광분한 뒤 불을 켜기까지 (s).</summary>
        public float SwitchDelay { get; }

        /// <summary>평온이 이 시간 이어지면 불을 끈다 (s).</summary>
        public float OffDelay { get; }
    }

    /// <summary>모기장 수치 (spec/06, M14).</summary>
    public sealed class NetSettings
    {
        public NetSettings(Tuning tuning)
        {
            RustleRadius = tuning.GetFloat("net.rustleRadius");
            RustleAwareness = tuning.GetFloat("net.rustleAwareness");
        }

        /// <summary>틈을 빠르게 지날 때 소음 반경 (u).</summary>
        public float RustleRadius { get; }

        /// <summary>그 소음을 들은 인간의 경계 증가.</summary>
        public float RustleAwareness { get; }
    }

    /// <summary>조명 하나 (레벨 데이터 lights[]). 영역은 축 정렬 상자.</summary>
    public sealed class LightDefinition
    {
        public LightDefinition(string id, Vector3 position, Vector3 areaCenter, Vector3 areaHalfSize, FanSchedule schedule, bool onWhenAlert)
        {
            Id = id;
            Position = position;
            AreaCenter = areaCenter;
            AreaHalfSize = areaHalfSize;
            Schedule = schedule;
            OnWhenAlert = onWhenAlert;
        }

        public string Id { get; }

        /// <summary>전구 위치 (표현).</summary>
        public Vector3 Position { get; }

        public Vector3 AreaCenter { get; }

        public Vector3 AreaHalfSize { get; }

        /// <summary>켜짐/꺼짐 주기. 있으면 주기를 따른다.</summary>
        public FanSchedule Schedule { get; }

        /// <summary>인간이 의심·광분하면 켜고 평온해지면 끈다.</summary>
        public bool OnWhenAlert { get; }

        public bool Contains(Vector3 point)
        {
            Vector3 offset = Vector3.Abs(point - AreaCenter);
            return offset.X <= AreaHalfSize.X && offset.Y <= AreaHalfSize.Y && offset.Z <= AreaHalfSize.Z;
        }
    }

    /// <summary>조명 하나의 지금 상태 (표현·HUD용).</summary>
    public readonly struct LightSnapshot
    {
        public LightSnapshot(LightDefinition definition, bool isOn)
        {
            Definition = definition;
            IsOn = isOn;
        }

        public LightDefinition Definition { get; }

        public bool IsOn { get; }
    }

    /// <summary>
    /// 조명 스위치 (spec/06, M14): 주기 또는 인간의 경계로 켜지고 꺼진다. 켜진 영역 안에서는 은신처가 숨겨 주지 못하고
    /// 인간 시각 증가가 light.visionMul배가 된다.
    /// </summary>
    public sealed class LightSystem
    {
        private readonly LightSettings _settings;
        private readonly IReadOnlyList<LightDefinition> _lights;
        private readonly bool[] _on;
        private int _alertSinceTick = Player.NeverTick;
        private int _calmSinceTick;

        public LightSystem(LightSettings settings, IReadOnlyList<LightDefinition> lights)
        {
            _settings = settings;
            _lights = lights ?? System.Array.Empty<LightDefinition>();
            _on = new bool[_lights.Count];
        }

        public IReadOnlyList<LightDefinition> Lights => _lights;

        public bool IsOn(int index) => _on[index];

        /// <summary>
        /// 곧 켜지는가 (gulf §8 예고): 경계로 켜지는 조명은 경계가 시작된 뒤 switchDelay 동안, 주기 조명은 켜지기 leadSeconds 전부터.
        /// 이미 켜져 있으면 거짓.
        /// </summary>
        public bool IsAboutToTurnOn(int index, int tick, float leadSeconds)
        {
            if (_on[index])
            {
                return false;
            }

            var light = _lights[index];
            if (light.Schedule != null)
            {
                return light.Schedule.IsOn((tick + SimulationTime.ToTicks(leadSeconds)) * GameSimulation.DeltaTime);
            }

            return light.OnWhenAlert && _alertSinceTick != Player.NeverTick;
        }

        /// <summary>틱마다: 인간 상태(직전 틱)와 주기로 켜짐을 정한다.</summary>
        public void Step(IReadOnlyList<Human> humans, int tick)
        {
            bool alert = false;
            foreach (var human in humans)
            {
                alert |= human.State != AwarenessState.Safe;
            }

            if (alert)
            {
                if (_alertSinceTick == Player.NeverTick)
                {
                    _alertSinceTick = tick;
                }
            }
            else
            {
                if (_alertSinceTick != Player.NeverTick)
                {
                    _calmSinceTick = tick;
                }

                _alertSinceTick = Player.NeverTick;
            }

            for (int i = 0; i < _lights.Count; i++)
            {
                var light = _lights[i];
                if (light.Schedule != null)
                {
                    _on[i] = light.Schedule.IsOn(tick * GameSimulation.DeltaTime);
                }
                else if (light.OnWhenAlert)
                {
                    if (alert && SimulationTime.HasElapsed(_alertSinceTick, tick, _settings.SwitchDelay))
                    {
                        _on[i] = true;
                    }
                    else if (!alert && _on[i] && SimulationTime.HasElapsed(_calmSinceTick, tick, _settings.OffDelay))
                    {
                        _on[i] = false;
                    }
                }
            }
        }

        /// <summary>그 점이 켜진 조명 영역 안인가.</summary>
        public bool IsLit(Vector3 point)
        {
            for (int i = 0; i < _lights.Count; i++)
            {
                if (_on[i] && _lights[i].Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        public float VisionMultiplier(Vector3 point) => IsLit(point) ? _settings.VisionMul : 1f;

        public IReadOnlyList<LightSnapshot> Snapshot()
        {
            var list = new List<LightSnapshot>();
            for (int i = 0; i < _lights.Count; i++)
            {
                list.Add(new LightSnapshot(_lights[i], _on[i]));
            }

            return list;
        }
    }
}
