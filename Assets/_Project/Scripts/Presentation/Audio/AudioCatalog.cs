using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moqui.Unity.Presentation.Audio
{
    /// <summary>사운드 ID → 클립·음량·반복·버스 (spec/10). 씬 생성기가 `AudioIds.Definitions`로 채운다.</summary>
    public sealed class AudioCatalog : ScriptableObject
    {
        public const string ResourcePath = "Audio/AudioCatalog";

        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public AudioClip Clip;
            [Range(0f, 1f)]
            public float Volume = 1f;
            public bool Loop;
            public AudioBus Bus;
        }

        [SerializeField]
        private List<Entry> _entries = new List<Entry>();

        private static AudioCatalog _loaded;

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>빌드에서도 쓰도록 Resources에서 불러온다. 없으면 null (오디오 없이 동작).</summary>
        public static AudioCatalog Load() => _loaded != null ? _loaded : _loaded = Resources.Load<AudioCatalog>(ResourcePath);

        public Entry Find(string id)
        {
            foreach (var entry in _entries)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }

        public void SetEntries(IEnumerable<Entry> entries)
        {
            _entries = new List<Entry>(entries);
        }
    }
}
