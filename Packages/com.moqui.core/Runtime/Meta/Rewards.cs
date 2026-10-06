using System;
using Moqui.Core.Data;
using Moqui.Core.Simulation;

namespace Moqui.Core.Meta
{
    /// <summary>클리어 보상 내역 (spec/09 §1). Result 화면에 항목별로 보여 준다.</summary>
    public sealed class RewardBreakdown
    {
        public RewardBreakdown(int clear, int noFrenzy, int carefulBite, int parTime)
        {
            Clear = clear;
            NoFrenzy = noFrenzy;
            CarefulBite = carefulBite;
            ParTime = parTime;
        }

        public int Clear { get; }

        public int NoFrenzy { get; }

        public int CarefulBite { get; }

        public int ParTime { get; }

        public int Total => Clear + NoFrenzy + CarefulBite + ParTime;
    }

    /// <summary>혈액 포인트 계산. 클리어할 때마다 지급하며 실패는 0 (spec/09 §1).</summary>
    public sealed class RewardCalculator
    {
        private readonly Tuning _tuning;

        public RewardCalculator(Tuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>기준 시간: meta.parTime.(레벨 ID)가 있으면 그것, 없으면 meta.parTime.default (새 레벨은 키를 추가하지 않아도 된다).</summary>
        public float ParTime(string levelId)
        {
            string key = $"meta.parTime.{levelId}";
            return _tuning.Contains(key) ? _tuning.GetFloat(key) : _tuning.GetFloat("meta.parTime.default");
        }

        public RewardBreakdown Compute(string levelId, StageResult result)
        {
            int noFrenzy = result.FrenzyCount == 0 ? _tuning.GetInt("meta.noFrenzyBonus") : 0;
            int careful = result.BiteMarkCount <= _tuning.GetInt("meta.carefulBiteMax") ? _tuning.GetInt("meta.carefulBiteBonus") : 0;
            int par = result.ClearSeconds <= ParTime(levelId) ? _tuning.GetInt("meta.parTimeBonus") : 0;
            return new RewardBreakdown(_tuning.GetInt("meta.clearReward"), noFrenzy, careful, par);
        }
    }
}
