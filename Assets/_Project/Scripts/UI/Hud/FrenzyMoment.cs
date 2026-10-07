using System.Collections.Generic;
using Moqui.Core.Simulation;
using UnityEngine;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>
    /// 왜 들켰나 (gulf §10, D-067): 광분에 들어가는 순간 짧게 느린 화면과 원인 문구를 보인다.
    /// 시간은 실제 시간(unscaled)으로 잰다. 일시정지(timeScale 0) 중에는 시간 배율을 건드리지 않는다.
    /// </summary>
    public sealed class FrenzyMoment
    {
        /// <summary>느린 화면 길이(실제 초)와 배율.</summary>
        public const float SlowSeconds = 0.5f;
        public const float SlowTimeScale = 0.35f;

        /// <summary>원인 문구가 보이는 시간(실제 초).</summary>
        public const float TextSeconds = 1.6f;

        private float _startedAt = float.NegativeInfinity;
        private bool _slowing;

        public AwarenessCause Cause { get; private set; }

        public string Text { get; private set; } = string.Empty;

        public bool TextVisible { get; private set; }

        public static string Label(AwarenessCause cause)
        {
            switch (cause)
            {
                case AwarenessCause.Sight:
                    return "들켰다! — 눈에 띄었다";
                case AwarenessCause.Hearing:
                    return "들켰다! — 날갯소리가 들렸다";
                case AwarenessCause.Itch:
                    return "들켰다! — 물린 자리가 가려웠다";
                case AwarenessCause.Glance:
                    return "들켰다! — 빨다가 눈이 마주쳤다";
                case AwarenessCause.Alarm:
                    return "들켰다! — 같이 있던 사람이 알려 줬다";
                default:
                    return "들켰다!";
            }
        }

        /// <summary>원인별 색 (HUD 문구·결과 타임라인 공통).</summary>
        public static Color ColorOf(AwarenessCause cause)
        {
            switch (cause)
            {
                case AwarenessCause.Sight:
                    return new Color(1f, 0.85f, 0.3f);
                case AwarenessCause.Hearing:
                    return new Color(0.4f, 0.85f, 1f);
                case AwarenessCause.Itch:
                    return new Color(1f, 0.5f, 0.75f);
                case AwarenessCause.Glance:
                    return new Color(1f, 0.55f, 0.25f);
                case AwarenessCause.Alarm:
                    return new Color(0.7f, 0.5f, 1f);
                default:
                    return new Color(0.6f, 0.6f, 0.7f);
            }
        }

        /// <summary>결과 화면처럼 느린 화면이 더는 필요 없을 때: 배율을 건드리지 않게 하고 문구도 끈다.</summary>
        public void Cancel()
        {
            _slowing = false;
            _startedAt = float.NegativeInfinity;
            TextVisible = false;
        }

        public void Trigger(AwarenessCause cause, float now)
        {
            Cause = cause;
            Text = Label(cause);
            _startedAt = now;
            _slowing = true;
        }

        /// <summary>
        /// 실제 시각 now에 맞춰 상태를 갱신하고, 써야 할 시간 배율을 돌려준다(바꾸지 않아도 되면 currentTimeScale 그대로).
        /// </summary>
        public float Update(float now, float currentTimeScale)
        {
            float elapsed = now - _startedAt;
            TextVisible = elapsed >= 0f && elapsed < TextSeconds;
            if (!_slowing || currentTimeScale == 0f)
            {
                return currentTimeScale;
            }

            if (elapsed < SlowSeconds)
            {
                return SlowTimeScale;
            }

            _slowing = false;
            return currentTimeScale == SlowTimeScale ? 1f : currentTimeScale;
        }
    }

    /// <summary>경계 타임라인 (gulf §10): 일정 간격으로 가장 경계한 사람의 경계값과 그때의 주된 원인을 모은다. 결과 화면이 그린다.</summary>
    public sealed class AwarenessTimeline
    {
        public const float SampleSeconds = 0.5f;

        private readonly List<(float Awareness, AwarenessCause Cause)> _samples = new List<(float, AwarenessCause)>();
        private readonly List<(int Sample, AwarenessCause Cause)> _frenzies = new List<(int, AwarenessCause)>();

        /// <summary>광분한 순간: 그때의 표본 번호와 원인 (결과 화면이 그 막대 위에 원인 아이콘을 놓는다).</summary>
        public IReadOnlyList<(int Sample, AwarenessCause Cause)> Frenzies => _frenzies;
        private int _nextTick;

        public IReadOnlyList<(float Awareness, AwarenessCause Cause)> Samples => _samples;

        /// <summary>표본이 maxBars보다 많으면 구간마다 경계가 가장 높은 표본 하나로 묶는다.</summary>
        public static IReadOnlyList<(float Awareness, AwarenessCause Cause)> Downsample(IReadOnlyList<(float Awareness, AwarenessCause Cause)> samples, int maxBars)
        {
            if (samples.Count <= maxBars)
            {
                return samples;
            }

            var bars = new List<(float, AwarenessCause)>(maxBars);
            for (int bar = 0; bar < maxBars; bar++)
            {
                int from = bar * samples.Count / maxBars;
                int to = (bar + 1) * samples.Count / maxBars;
                var best = samples[from];
                for (int i = from + 1; i < to; i++)
                {
                    if (samples[i].Awareness > best.Awareness)
                    {
                        best = samples[i];
                    }
                }

                bars.Add(best);
            }

            return bars;
        }

        public void Observe(GameSimulation simulation)
        {
            foreach (var simulationEvent in simulation.Events)
            {
                if (simulationEvent is FrenzyTriggered triggered)
                {
                    _frenzies.Add((_samples.Count, triggered.Cause));
                }
            }

            if (simulation.Tick < _nextTick)
            {
                return;
            }

            _nextTick = simulation.Tick + SimulationTime.ToTicks(SampleSeconds);
            Human top = null;
            foreach (var human in simulation.Humans)
            {
                if (top == null || human.Awareness > top.Awareness)
                {
                    top = human;
                }
            }

            if (top != null)
            {
                _samples.Add((top.Awareness, top.Causes.Dominant));
            }
        }
    }
}
