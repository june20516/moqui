using Moqui.Unity.Presentation.Audio;
using UnityEngine;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>HUD 효과음 출구: 공격 예고 경고음을 오디오 카탈로그(`sfx_telegraph`)로 낸다 (spec/10).</summary>
    public sealed class HudAudioSource : MonoBehaviour, IHudAudio
    {
        public void PlayTelegraph()
        {
            AudioOutput.Ensure()?.PlayOneShot(AudioIds.Telegraph);
        }
    }
}
