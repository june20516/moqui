using System.Collections.Generic;
using UnityEngine;

namespace Moqui.Unity.Settings
{
    /// <summary>설정 값 저장소. 실행에서는 PlayerPrefs, 테스트에서는 메모리를 쓴다.</summary>
    public interface IPreferenceStore
    {
        bool HasKey(string key);

        string GetString(string key, string defaultValue);

        void SetString(string key, string value);
    }

    public sealed class PlayerPrefsStore : IPreferenceStore
    {
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }
    }

    public sealed class MemoryPreferenceStore : IPreferenceStore
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public bool HasKey(string key)
        {
            return _values.ContainsKey(key);
        }

        public string GetString(string key, string defaultValue)
        {
            return _values.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public void SetString(string key, string value)
        {
            _values[key] = value;
        }
    }
}
