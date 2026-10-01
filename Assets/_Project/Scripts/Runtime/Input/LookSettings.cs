using Moqui.Core.Data;

namespace Moqui.Unity.Input
{
    /// <summary>시점 조작 수치 (spec/tuning.md world / camera).</summary>
    public sealed class LookSettings
    {
        public LookSettings(Tuning tuning)
        {
            MouseSensitivity = tuning.GetFloat("input.mouseSensitivity");
            GamepadLookSpeed = tuning.GetFloat("input.gamepadLookSpeed");
            PitchLimit = tuning.GetFloat("camera.pitchLimit");
        }

        /// <summary>마우스 1px당 회전 각도(°).</summary>
        public float MouseSensitivity { get; }

        /// <summary>스틱을 끝까지 밀었을 때 회전 속도(°/s).</summary>
        public float GamepadLookSpeed { get; }

        /// <summary>pitch 절댓값 상한(°).</summary>
        public float PitchLimit { get; }
    }
}
