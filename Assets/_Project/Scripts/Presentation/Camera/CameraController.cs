using Moqui.Unity.Input;
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
        private Vector3 _pivotUp = Vector3.up;
        private float _thirdPersonDistance = -1f;
        private float _distanceMul = 1f;

        /// <summary>지금 3인칭 거리 배율 (테스트용).</summary>
        public float DistanceMultiplier => _distanceMul;

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

        /// <summary>
        /// 자기 캐릭터를 숨겨야 하는가 (그림자만): 1인칭이거나, 3인칭 카메라가 좁은 곳에 몰려 플레이어에 너무 가까울 때 (M13).
        /// 카메라가 모키 몸 안에 들어가면 외곽선 뒷면이 화면을 덮어 까맣게 보이기 때문이다.
        /// </summary>
        public bool PlayerHidden { get; private set; }

        public void Toggle()
        {
            View = IsFirstPerson ? CameraViewMode.ThirdPerson : CameraViewMode.FirstPerson;

            // 전환 도중 다시 누르면 남은 진행도에서 되돌아간다.
            _transition = 1f - _transition;
            _preferences.SetString(ViewPreferenceKey, View.ToString());
        }

        /// <summary>
        /// 1인칭 부착 상태면 시선을 표면 법선 기준 attachedLookLimit 원뿔 안으로 당긴다 (spec/00).
        /// 제한한 값을 시점 상태에 다시 써서 입력이 원뿔 밖으로 누적되지 않게 한다.
        /// </summary>
        public void ConstrainLook(LookState look, Vector3? surfaceNormal)
        {
            if (!IsFirstPerson || !surfaceNormal.HasValue)
            {
                return;
            }

            float yaw = look.Yaw;
            float pitch = look.Pitch;
            LookConstraint.ClampToCone(ref yaw, ref pitch, surfaceNormal.Value, _settings.FirstPersonAttachedLookLimit);
            look.Set(yaw, pitch);
        }

        /// <summary>
        /// 이번 프레임의 카메라 포즈. 시점 전환은 위치와 FOV만 바꾸고 yaw/pitch는 그대로 쓴다.
        /// 3인칭 (M13): 피벗 기준 방향은 붙어 있으면 표면 법선, 아니면 월드 위이며 camera.pivotBlendTime 동안 돌아간다.
        /// 막혀서 당길 때는 즉시, 다시 물러날 때는 camera.returnSpeed로 천천히 (좁은 곳에서 화면이 튀지 않게).
        /// </summary>
        public CameraPose Update(float deltaTime, Vector3 playerPosition, float yaw, float pitch, Vector3? surfaceNormal = null, bool riding = false)
        {
            // 움직이는 몸에 붙어 있으면 조금 물러난다(몸의 흔들림이 화면을 덜 흔든다, 플레이 피드백 2026-10-07).
            float rate = _settings.RidingBlendTime > 0f ? (_settings.RidingDistanceMul - 1f) * deltaTime / _settings.RidingBlendTime : float.PositiveInfinity;
            _distanceMul = Mathf.MoveTowards(_distanceMul, riding ? _settings.RidingDistanceMul : 1f, rate);

            if (_settings.SwitchTime > 0f)
            {
                _transition = Mathf.Min(1f, _transition + (deltaTime / _settings.SwitchTime));
            }
            else
            {
                _transition = 1f;
            }

            CameraPose thirdPerson = ThirdPerson(deltaTime, playerPosition, yaw, pitch, surfaceNormal ?? Vector3.up);
            CameraPose firstPerson = surfaceNormal.HasValue
                ? _solver.FirstPersonAttached(playerPosition, yaw, pitch, surfaceNormal.Value)
                : _solver.FirstPerson(playerPosition, yaw, pitch);
            CameraPose from = IsFirstPerson ? thirdPerson : firstPerson;
            CameraPose to = IsFirstPerson ? firstPerson : thirdPerson;
            PlayerHidden = IsFirstPerson || Vector3.Distance(thirdPerson.Position, playerPosition) < _settings.HidePlayerDistance;
            return CameraPose.Lerp(from, to, Mathf.SmoothStep(0f, 1f, _transition));
        }

        private CameraPose ThirdPerson(float deltaTime, Vector3 playerPosition, float yaw, float pitch, Vector3 targetUp)
        {
            float maxRadians = _settings.PivotBlendTime > 0f ? Mathf.PI * deltaTime / _settings.PivotBlendTime : Mathf.PI;
            _pivotUp = Vector3.RotateTowards(_pivotUp, targetUp.normalized, maxRadians, 0f).normalized;

            Vector3 pivot = _solver.ThirdPersonPivot(playerPosition, _pivotUp);
            Quaternion rotation = CameraPoseSolver.LookRotation(yaw, pitch);
            float reach = _solver.ThirdPersonReach(pivot, rotation, _distanceMul);
            _thirdPersonDistance = _thirdPersonDistance < 0f || reach <= _thirdPersonDistance
                ? reach
                : Mathf.MoveTowards(_thirdPersonDistance, reach, _settings.ReturnSpeed * deltaTime);
            return _solver.ThirdPersonAt(pivot, rotation, _thirdPersonDistance);
        }

        private CameraViewMode LoadView()
        {
            string saved = _preferences.GetString(ViewPreferenceKey, _settings.DefaultView.ToString());
            return System.Enum.TryParse(saved, out CameraViewMode view) ? view : _settings.DefaultView;
        }
    }
}
