using System.Collections.Generic;
using UnityEngine;

namespace Moqui.Unity.Presentation.Audio
{
    /// <summary>
    /// 카탈로그 소리를 내는 단일 출구. 씬을 넘어 살아 있어 음악이 화면 전환에도 이어진다.
    /// 볼륨은 매 프레임 `AudioVolumes`에서 다시 읽으므로 설정 변경이 바로 들린다.
    /// 플레이 중이 아니거나 카탈로그가 없으면 만들지 않는다 (호출부는 null을 허용한다).
    /// </summary>
    public sealed class AudioOutput : MonoBehaviour
    {
        private readonly Dictionary<string, AudioSource> _loops = new Dictionary<string, AudioSource>();
        private readonly HashSet<string> _activeLoops = new HashSet<string>();
        private AudioCatalog _catalog;
        private AudioSource _oneShots;
        private AudioSource _music;

        public static AudioOutput Instance { get; private set; }

        /// <summary>지금 재생 중인 음악 ID (없으면 null).</summary>
        public string MusicId { get; private set; }

        public static AudioOutput Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var catalog = AudioCatalog.Load();
            if (!Application.isPlaying || catalog == null)
            {
                return null;
            }

            // 모든 소리가 2D라 듣는 위치가 상관없으므로 리스너도 여기 하나만 둔다 (씬 카메라에는 없음).
            var go = new GameObject("AudioOutput", typeof(AudioListener));
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AudioOutput>();
            Instance._catalog = catalog;
            Instance._oneShots = Instance.CreateSource();
            Instance._music = Instance.CreateSource();
            Instance._music.loop = true;
            return Instance;
        }

        public void PlayOneShot(string id, float pitch = 1f)
        {
            var entry = _catalog.Find(id);
            if (entry?.Clip == null)
            {
                return;
            }

            _oneShots.pitch = pitch;
            _oneShots.PlayOneShot(entry.Clip, entry.Volume * AudioVolumes.For(entry.Bus));
        }

        /// <summary>반복음을 켜거나 끈다. 켜진 동안 pitch를 바꿀 수 있다 (날갯소리 속도 변조).</summary>
        public void SetLoop(string id, bool playing, float pitch = 1f)
        {
            if (!_loops.TryGetValue(id, out var source))
            {
                if (!playing)
                {
                    return;
                }

                var entry = _catalog.Find(id);
                if (entry?.Clip == null)
                {
                    return;
                }

                source = CreateSource();
                source.clip = entry.Clip;
                source.loop = true;
                source.volume = entry.Volume * AudioVolumes.For(entry.Bus);
                _loops[id] = source;
            }

            source.pitch = pitch;
            if (playing && _activeLoops.Add(id))
            {
                source.Play();
            }
            else if (!playing && _activeLoops.Remove(id))
            {
                source.Stop();
            }
        }

        /// <summary>켜 둔 반복음인가 (오디오 장치가 없어도 요청 상태로 판단한다).</summary>
        public bool IsLoopActive(string id) => _activeLoops.Contains(id);

        public float LoopPitch(string id) => _loops.TryGetValue(id, out var source) ? source.pitch : 1f;

        public void StopAllLoops()
        {
            foreach (var source in _loops.Values)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }

            _activeLoops.Clear();
        }

        /// <summary>음악을 바꾼다. 같은 곡이면 이어서 재생한다.</summary>
        public void PlayMusic(string id)
        {
            if (MusicId == id)
            {
                return;
            }

            var entry = _catalog.Find(id);
            MusicId = entry?.Clip != null ? id : null;
            _music.Stop();
            if (MusicId != null)
            {
                _music.clip = entry.Clip;
                _music.volume = entry.Volume * AudioVolumes.Music;
                _music.Play();
            }
        }

        /// <summary>현재 음악의 실제 음량 (카탈로그 음량 × 음악 볼륨).</summary>
        public float MusicVolume => _music.volume;

        private void Update()
        {
            if (MusicId != null)
            {
                _music.volume = _catalog.Find(MusicId).Volume * AudioVolumes.Music;
            }

            foreach (var pair in _loops)
            {
                var entry = _catalog.Find(pair.Key);
                pair.Value.volume = entry.Volume * AudioVolumes.For(entry.Bus);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
