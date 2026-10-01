using System;
using System.Collections.Generic;

namespace Moqui.Unity.Diagnostics
{
    /// <summary>프레임 타임(ms) 표본의 요약: 평균, 백분위, 최대, 예산 초과 비율 (M11 성능 측정).</summary>
    public sealed class FrameStats
    {
        private readonly List<float> _frameMs = new List<float>();

        public int Count => _frameMs.Count;

        public void Add(float frameMs)
        {
            _frameMs.Add(frameMs);
        }

        public float Average
        {
            get
            {
                RequireSamples();
                float sum = 0f;
                foreach (float ms in _frameMs)
                {
                    sum += ms;
                }

                return sum / _frameMs.Count;
            }
        }

        public float Max
        {
            get
            {
                RequireSamples();
                float max = float.MinValue;
                foreach (float ms in _frameMs)
                {
                    max = Math.Max(max, ms);
                }

                return max;
            }
        }

        /// <summary>최근접 순위 백분위 (percent 0~100).</summary>
        public float Percentile(float percent)
        {
            RequireSamples();
            var sorted = new List<float>(_frameMs);
            sorted.Sort();
            int rank = (int)Math.Ceiling(percent / 100f * sorted.Count);
            return sorted[Math.Min(Math.Max(rank, 1), sorted.Count) - 1];
        }

        /// <summary>예산(ms)을 넘은 프레임 비율 (0~1).</summary>
        public float OverBudgetRatio(float budgetMs)
        {
            RequireSamples();
            int over = 0;
            foreach (float ms in _frameMs)
            {
                if (ms > budgetMs)
                {
                    over++;
                }
            }

            return (float)over / _frameMs.Count;
        }

        public static float BudgetMs(float targetFps) => 1000f / targetFps;

        private void RequireSamples()
        {
            if (_frameMs.Count == 0)
            {
                throw new InvalidOperationException("No frame samples.");
            }
        }
    }
}
