using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation
{
    /// <summary>플레이어 그림을 보간 위치와 시뮬레이션 yaw에 맞추고, 이동 방향으로 몸을 기울인다 (spec/10 이동 기울기).</summary>
    public sealed class PlayerView : MonoBehaviour
    {
        /// <summary>최고 비행 속도일 때의 기울기 (도).</summary>
        public const float MaxLeanDegrees = 25f;

        /// <summary>기울기가 목표를 따라가는 속도 (1/s).</summary>
        private const float LeanResponse = 10f;

        [SerializeField]
        private SimulationRunner _runner;

        private Vector2 _lean;
        private bool _wasAttached;
        private bool _hasRendered;
        private Vector3 _glideFrom;
        private float _glideElapsed = float.PositiveInfinity;
        private Transform _landingMarker;

        /// <summary>F 착지 때 붙을 곳까지 미끄러지는 시간 (표현 전용, gulf §2). Core는 즉시 붙는다.</summary>
        public const float SnapGlideSeconds = 0.15f;

        /// <summary>이보다 짧게 움직인 착지는 미끄러지지 않고 바로 붙는다 (u).</summary>
        public const float SnapGlideMinDistance = 0.5f;

        /// <summary>착지 표시(발밑 마법진 대용) 지름과 표면에서 띄우는 높이 (u).</summary>
        public const float LandingMarkerDiameter = 1.6f;
        public const float LandingMarkerLift = 0.05f;

        private static readonly Color LandingMarkerColor = new Color(1f, 0.55f, 0.8f);

        /// <summary>착지 표시가 보이는가 (테스트용).</summary>
        public bool LandingMarkerVisible => _landingMarker != null && _landingMarker.gameObject.activeSelf;

        /// <summary>
        /// 미끄러져 붙기 위치: 처음은 빠르고 끝에서 감속(ease-out)한다. t ≥ 1이면 목표.
        /// </summary>
        public static Vector3 Glide(Vector3 from, Vector3 to, float elapsed)
        {
            float t = Mathf.Clamp01(elapsed / SnapGlideSeconds);
            float eased = 1f - ((1f - t) * (1f - t));
            return Vector3.Lerp(from, to, eased);
        }

        /// <summary>호버링 둥실거림 (표현 전용, M14): 폭(u)과 주기(Hz). 빠르게 날수록 줄어든다.</summary>
        public const float HoverBobAmplitude = 0.06f;
        public const float HoverBobFrequency = 1.4f;

        /// <summary>정지 비행일수록 크게 위아래로 둥실거린다. 최고 속도에서는 0.</summary>
        public static float HoverBob(float time, float speedRatio)
        {
            return HoverBobAmplitude * (1f - Mathf.Clamp01(speedRatio)) * Mathf.Sin(2f * Mathf.PI * HoverBobFrequency * time);
        }

        private void LateUpdate()
        {
            if (!_runner.IsRunning)
            {
                return;
            }

            var simulation = _runner.Driver.Simulation;
            var player = simulation.Player;
            RefreshLandingMarker(simulation);
            bool attached = player.State == Core.Simulation.PlayerState.Attached;
            if (attached)
            {
                // 벽·천장에 붙으면 몸의 up을 표면 법선에 맞춰 "앉은" 자세로 보이게 한다 (spec/03, M12).
                // 멀리서 F로 붙었으면 붙은 자리까지 짧게 미끄러져 간다 (gulf §2).
                Vector3 attachedAt = _runner.Driver.InterpolatedPlayerPosition;
                if (!_wasAttached && _hasRendered && Vector3.Distance(transform.position, attachedAt) > SnapGlideMinDistance)
                {
                    _glideFrom = transform.position;
                    _glideElapsed = 0f;
                }

                _glideElapsed += Time.deltaTime;
                _lean = Vector2.zero;
                _wasAttached = true;
                _hasRendered = true;
                transform.SetPositionAndRotation(Glide(_glideFrom, attachedAt, _glideElapsed), AttachedRotation(player.Up.ToUnity(), player.Yaw));
                return;
            }

            _wasAttached = false;
            _hasRendered = true;
            _glideElapsed = float.PositiveInfinity;

            Quaternion yaw = Quaternion.Euler(0f, player.Yaw, 0f);
            Vector3 localVelocity = Quaternion.Inverse(yaw) * player.Velocity.ToUnity();
            Vector2 target = Lean(localVelocity, simulation.Settings.Flight.Speed);
            _lean = Vector2.Lerp(_lean, target, 1f - Mathf.Exp(-LeanResponse * Time.deltaTime));

            float speedRatio = player.Velocity.Length() / simulation.Settings.Flight.Speed;
            Vector3 bob = Vector3.up * HoverBob(Time.time, speedRatio);
            transform.SetPositionAndRotation(_runner.Driver.InterpolatedPlayerPosition + bob, yaw * Quaternion.Euler(_lean.x, 0f, _lean.y));
        }

        /// <summary>착지 표시: 비행 중 F로 붙을 수 있으면 붙을 자리에 작은 분홍 원 (발밑 마법진 VFX 전 대용, spec/12 land.ready).</summary>
        private void RefreshLandingMarker(Core.Simulation.GameSimulation simulation)
        {
            if (_landingMarker == null)
            {
                var marker = Art.Primitives.Create(PrimitiveType.Cylinder, "LandingMarker", null);
                marker.transform.localScale = new Vector3(LandingMarkerDiameter, 0.01f, LandingMarkerDiameter);
                WorldView.Tint(marker.GetComponent<Renderer>(), LandingMarkerColor);
                marker.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _landingMarker = marker.transform;
            }

            bool show = simulation.TryGetAttachTarget(out var point, out var normal);
            _landingMarker.gameObject.SetActive(show);
            if (show)
            {
                Vector3 up = normal.ToUnity();
                _landingMarker.SetPositionAndRotation(point.ToUnity() + (up * LandingMarkerLift), Quaternion.FromToRotation(Vector3.up, up));
            }
        }

        private void OnDestroy()
        {
            if (_landingMarker != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_landingMarker.gameObject);
                }
                else
                {
                    DestroyImmediate(_landingMarker.gameObject);
                }
            }
        }

        /// <summary>부착 중 몸 방향: up = 표면 법선, 앞 = 시점 방향을 표면에 투영한 방향 (투영이 거의 0이면 월드 위쪽을 투영).</summary>
        public static Quaternion AttachedRotation(Vector3 surfaceNormal, float yawDegrees)
        {
            Vector3 up = surfaceNormal.normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward, up);
            if (forward.sqrMagnitude < 1e-4f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.up, up);
            }

            return Quaternion.LookRotation(forward.normalized, up);
        }

        /// <summary>로컬 속도 → (앞뒤 pitch, 좌우 roll) 기울기. 앞으로 가면 앞으로, 오른쪽으로 가면 오른쪽으로 기운다.</summary>
        public static Vector2 Lean(Vector3 localVelocity, float flightSpeed)
        {
            float forward = Mathf.Clamp(localVelocity.z / flightSpeed, -1f, 1f);
            float right = Mathf.Clamp(localVelocity.x / flightSpeed, -1f, 1f);
            return new Vector2(forward * MaxLeanDegrees, -right * MaxLeanDegrees);
        }
    }
}
