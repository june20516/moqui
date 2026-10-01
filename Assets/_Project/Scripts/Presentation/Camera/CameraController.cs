using Moqui.Unity.Settings;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>
    /// 시점 상태와 전환 보간 (spec/00, D-007). 전환은 camera.switchTime 동안 위치·회전·FOV를 보간하고,
    /// 선택한 시점은 설정(spec/08 "기본 시점")으로 저장해 다음 실행에 그대로 시작한다.
    /// </summary>
    public sealed class CameraController
    {
        public const string ViewPreferenceKey = "settings.defaultView";

        private readonly CameraSettings _settings;
        private readonly CameraPoseSolver _solver;
        private readonly IPreferenceStore _preferences;
        private float _transition;

        public CameraController(CameraSettings settings, CameraPoseSolver solver, IPreferenceStore preferences)
        {
            _settings = settings;
            _solver = solver;
            _preferences = preferences;
            View = LoadView();
            _transition = 1f;
        }

        public CameraViewMode View { get; private set; }

        /// <summary>전환 진행도 (0~1). 1이면 전환이 끝났다.</summary>
        public float Transition => _transition;

        public bool IsFirstPerson => View == CameraViewMode.FirstPerson;

        public void Toggle()
        {
            View = IsFirstPerson ? CameraViewMode.ThirdPerson : CameraViewMode.FirstPerson;

            // 전환 도중 다시 누르면 남은 진행도에서 되돌아간다.
            _transition = 1f - _transition;
            _preferences.SetString(ViewPreferenceKey, View.ToString());
        }

        /// <summary>이번 프레임의 카메라 포즈. 시점 전환은 위치와 FOV만 바꾸고 yaw/pitch는 그대로 쓴다.</summary>
        public CameraPose Update(float deltaTime, Vector3 playerPosition, float yaw, float pitch)
        {
            if (_settings.SwitchTime > 0f)
            {
                _transition = Mathf.Min(1f, _transition + (deltaTime / _settings.SwitchTime));
            }
            else
            {
                _transition = 1f;
            }

            CameraPose thirdPerson = _solver.ThirdPerson(playerPosition, yaw, pitch);
            CameraPose firstPerson = _solver.FirstPerson(playerPosition, yaw, pitch);
            CameraPose from = IsFirstPerson ? thirdPerson : firstPerson;
            CameraPose to = IsFirstPerson ? firstPerson : thirdPerson;
            return CameraPose.Lerp(from, to, Mathf.SmoothStep(0f, 1f, _transition));
        }

        private CameraViewMode LoadView()
        {
            string saved = _preferences.GetString(ViewPreferenceKey, _settings.DefaultView.ToString());
            return System.Enum.TryParse(saved, out CameraViewMode view) ? view : _settings.DefaultView;
        }
    }
}
