namespace Moqui.Core.Data
{
    /// <summary>tuning의 범위 값 (예: "4~9s"). 양 끝을 포함한다.</summary>
    public readonly struct FloatRange
    {
        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }

        public override string ToString()
        {
            return $"{Min}~{Max}";
        }
    }
}
