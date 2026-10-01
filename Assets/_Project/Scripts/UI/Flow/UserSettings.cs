using System;
using System.Globalization;
using Moqui.Unity.Presentation;
using Moqui.Unity.Settings;
using Moqui.Unity.UI.Hud;
using UnityEngine;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// 사용자 설정 (spec/08 §설정). PlayerPrefs(IPreferenceStore)에 저장하며 게임 진행 데이터(save.json)와 분리한다.
    /// </summary>
    public sealed class UserSettings
    {
        public const string SensitivityKey = "settings.mouseSensitivity";
        public const string InvertYKey = "settings.invertY";
        public const string MasterVolumeKey = "settings.masterVolume";
        public const string SfxVolumeKey = "settings.sfxVolume";
        public const string MusicVolumeKey = "settings.musicVolume";
        public const string FullscreenKey = "settings.fullscreen";
        public const string ResolutionKey = "settings.resolution";

        public const float MinSensitivity = 0.25f;
        public const float MaxSensitivity = 4f;

        /// <summary>선택 가능한 해상도 (16:9).</summary>
        public static readonly Vector2Int[] Resolutions = { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) };

        private const int DefaultResolutionIndex = 2;
        private readonly IPreferenceStore _store;

        public UserSettings(IPreferenceStore store)
        {
            _store = store;
        }

        public float MouseSensitivity
        {
            get => Mathf.Clamp(GetFloat(SensitivityKey, 1f), MinSensitivity, MaxSensitivity);
            set => SetFloat(SensitivityKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));
        }

        public bool InvertY
        {
            get => GetBool(InvertYKey, false);
            set => SetBool(InvertYKey, value);
        }

        public bool TutorialHints
        {
            get => new TutorialHints(_store).Enabled;
            set => new TutorialHints(_store).Enabled = value;
        }

        /// <summary>기본 시점 (spec/00). 게임 중 시점 전환도 같은 키를 갱신한다.</summary>
        public CameraViewMode DefaultView
        {
            get => Enum.TryParse(_store.GetString(CameraController.ViewPreferenceKey, CameraViewMode.ThirdPerson.ToString()), out CameraViewMode view) ? view : CameraViewMode.ThirdPerson;
            set => _store.SetString(CameraController.ViewPreferenceKey, value.ToString());
        }

        public float MasterVolume
        {
            get => Mathf.Clamp01(GetFloat(MasterVolumeKey, 1f));
            set => SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
        }

        public float SfxVolume
        {
            get => Mathf.Clamp01(GetFloat(SfxVolumeKey, 1f));
            set => SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        }

        public float MusicVolume
        {
            get => Mathf.Clamp01(GetFloat(MusicVolumeKey, 1f));
            set => SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        }

        public bool Fullscreen
        {
            get => GetBool(FullscreenKey, true);
            set => SetBool(FullscreenKey, value);
        }

        public int ResolutionIndex
        {
            get => Mathf.Clamp((int)GetFloat(ResolutionKey, DefaultResolutionIndex), 0, Resolutions.Length - 1);
            set => SetFloat(ResolutionKey, Mathf.Clamp(value, 0, Resolutions.Length - 1));
        }

        public Vector2Int Resolution => Resolutions[ResolutionIndex];

        /// <summary>전체 볼륨과 화면 모드를 엔진에 반영한다. 에디터에서는 화면 모드를 바꾸지 않는다.</summary>
        public void ApplyToEngine()
        {
            AudioListener.volume = MasterVolume;
            if (!Application.isEditor)
            {
                Screen.SetResolution(Resolution.x, Resolution.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
        }

        private float GetFloat(string key, float defaultValue)
        {
            string text = Read(key);
            return text != null && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : defaultValue;
        }

        private void SetFloat(string key, float value)
        {
            _store.SetString(key, value.ToString("R", CultureInfo.InvariantCulture));
        }

        private bool GetBool(string key, bool defaultValue)
        {
            string text = Read(key);
            return text == null ? defaultValue : text == "1";
        }

        /// <summary>저장된 값이 없으면 null (PlayerPrefs는 기본값 null을 빈 문자열로 돌려줄 수 있어 HasKey로 확인한다).</summary>
        private string Read(string key)
        {
            return _store.HasKey(key) ? _store.GetString(key, string.Empty) : null;
        }

        private void SetBool(string key, bool value)
        {
            _store.SetString(key, value ? "1" : "0");
        }
    }
}
