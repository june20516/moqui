using UnityEngine;

namespace Moqui.Unity.Input
{
    /// <summary>
    /// 카메라 yaw/pitch (도). yaw 0°는 +Z, 양수는 오른쪽으로 돈다. pitch 양수는 위를 본다.
    /// 3인칭과 1인칭이 같은 값을 공유한다 (spec/00).
    /// </summary>
    public sealed class LookState
    {
        private const float FullTurn = 360f;

        private readonly float _pitchLimit;

        public LookState(float pitchLimit, float yaw = 0f, float pitch = 0f)
        {
            _pitchLimit = pitchLimit;
            Yaw = Mathf.Repeat(yaw, FullTurn);
            Pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
        }

        public float Yaw { get; private set; }

        public float Pitch { get; private set; }

        public float PitchLimit => _pitchLimit;

        public void Rotate(float yawDelta, float pitchDelta)
        {
            Yaw = Mathf.Repeat(Yaw + yawDelta, FullTurn);
            Pitch = Mathf.Clamp(Pitch + pitchDelta, -_pitchLimit, _pitchLimit);
        }

        public void Set(float yaw, float pitch)
        {
            Yaw = Mathf.Repeat(yaw, FullTurn);
            Pitch = Mathf.Clamp(pitch, -_pitchLimit, _pitchLimit);
        }
    }
}
