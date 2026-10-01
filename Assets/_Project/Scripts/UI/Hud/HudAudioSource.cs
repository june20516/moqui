using UnityEngine;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>
    /// 절차적으로 합성한 경고음을 내는 오디오 출구 (asset-pipeline: 효과음 합성). 오디오 카탈로그(M10)에서 에셋으로 바꿀 수 있다.
    /// </summary>
    public sealed class HudAudioSource : MonoBehaviour, IHudAudio
    {
        private const int SampleRate = 44100;
        private const float TelegraphSeconds = 0.18f;
        private const float TelegraphStartHz = 1900f;
        private const float TelegraphEndHz = 1200f;
        private const float TelegraphVolume = 0.6f;

        private AudioSource _source;
        private AudioClip _telegraph;

        public AudioClip TelegraphClip => _telegraph ??= CreateTelegraphClip();

        public void PlayTelegraph()
        {
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
            }

            _source.PlayOneShot(TelegraphClip, TelegraphVolume);
        }

        /// <summary>짧고 날카롭게 내려가는 사각파 경고음.</summary>
        private static AudioClip CreateTelegraphClip()
        {
            int count = Mathf.RoundToInt(SampleRate * TelegraphSeconds);
            var samples = new float[count];
            double phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float frequency = Mathf.Lerp(TelegraphStartHz, TelegraphEndHz, t);
                phase += frequency / SampleRate;
                float square = (phase % 1.0) < 0.5 ? 1f : -1f;
                float envelope = Mathf.Min(1f, t * 20f) * (1f - t);
                samples[i] = square * envelope * 0.5f;
            }

            var clip = AudioClip.Create("sfx_telegraph", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
