using System.Numerics;

namespace Moqui.Core.Simulation
{
    /// <summary>
    /// 틱마다 Unity가 넣는 입력 (tech/architecture.md §4.2).
    /// Move: X = 오른쪽(+)/왼쪽(−), Y = 앞(+)/뒤(−). 길이가 1을 넘으면 정규화한다.
    /// Vertical: 상승(+)/하강(−), −1~1. LookYaw/LookPitch: 카메라 각도(도).
    /// *Pressed는 이번 틱에 눌렸는지(edge), *Held는 누르고 있는지.
    /// </summary>
    public struct PlayerCommand
    {
        public Vector2 Move;
        public float Vertical;
        public float LookYaw;
        public float LookPitch;
        public bool DashPressed;
        public bool PrecisionHeld;
        public bool AttachPressed;
        public bool SuckHeld;
        public bool SkillPressed;

        public static PlayerCommand None => default;
    }
}
