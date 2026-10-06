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
        private Transform _wingLeft;
        private Transform _wingRight;
        private Vector3 _wingLeftRest;
        private Vector3 _wingRightRest;
        private float _wingFold;
        private Player.ProximityCues _proximity;
        private Light _wandLight;

        /// <summary>흡혈 중 지팡이 하트 빛 (표현 전용, gulf §13 · 큐 wand.drink): 주변 피부를 분홍으로 물들인다.</summary>
        public const float WandLightRange = 20f;
        public const float WandLightIntensity = 0.6f;
        private static readonly Color WandLightColor = new Color(1f, 0.45f, 0.7f);

        /// <summary>지팡이 빛이 켜져 있는가 (테스트용).</summary>
        public bool WandLightOn => _wandLight != null && _wandLight.enabled;

        /// <summary>거리감 그림자·지면 효과 (gulf §4).</summary>
        public Player.ProximityCues Proximity => _proximity;

        /// <summary>정밀 비행 때 날개를 접는 각(도)과 반응 속도(1/s) (표현 전용, gulf §6).</summary>
        public const float PrecisionWingFoldDegrees = 40f;
        private const float WingFoldResponse = 12f;

        /// <summary>지금 날개를 접은 정도 0~1 (테스트용).</summary>
        public float WingFold => _wingFold;

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
                FoldWings(false);
                RefreshProximity(simulation);
                RefreshWandLight(simulation, Time.time);
                return;
            }

            _wasAttached = false;
            _hasRendered = true;
            _glideElapsed = float.PositiveInfinity;
            FoldWings(player.PrecisionHeld);

            Quaternion yaw = Quaternion.Euler(0f, player.Yaw, 0f);
            Vector3 localVelocity = Quaternion.Inverse(yaw) * player.Velocity.ToUnity();
            Vector2 target = Lean(localVelocity, simulation.Settings.Flight.Speed);
            _lean = Vector2.Lerp(_lean, target, 1f - Mathf.Exp(-LeanResponse * Time.deltaTime));

            float speedRatio = player.Velocity.Length() / simulation.Settings.Flight.Speed;
            Vector3 bob = Vector3.up * HoverBob(Time.time, speedRatio);
            transform.SetPositionAndRotation(_runner.Driver.InterpolatedPlayerPosition + bob, yaw * Quaternion.Euler(_lean.x, 0f, _lean.y));
            RefreshProximity(simulation);
            RefreshWandLight(simulation, Time.time);
        }

        /// <summary>
        /// 정밀 비행: 날개를 뒤로 반쯤 접는다(애니메이터가 쓴 자세 위에 더한다). 조용히 나는 중이라는 것이 몸으로 보인다 (gulf §6).
        /// </summary>
        private void FoldWings(bool precise)
        {
            if (_wingLeft == null)
            {
                _wingLeft = transform.Find("MokiRig/WingPivotL/Wing");
                _wingRight = transform.Find("MokiRig/WingPivotR/Wing");
                _wingLeftRest = _wingLeft != null ? _wingLeft.localPosition : Vector3.zero;
                _wingRightRest = _wingRight != null ? _wingRight.localPosition : Vector3.zero;
            }

            _wingFold = Mathf.Lerp(_wingFold, precise ? 1f : 0f, 1f - Mathf.Exp(-WingFoldResponse * Time.deltaTime));
            float degrees = PrecisionWingFoldDegrees * _wingFold;
            FoldWing(_wingLeft, _wingLeftRest, degrees);
            FoldWing(_wingRight, _wingRightRest, -degrees);
        }

        /// <summary>날개 판을 날개 축(부모 피벗) 둘레로 돌린다. 매 프레임 절대값으로 쓰므로 애니메이터 상태와 무관하게 누적되지 않는다.</summary>
        private static void FoldWing(Transform wing, Vector3 restPosition, float degrees)
        {
            if (wing == null)
            {
                return;
            }

            var fold = Quaternion.Euler(0f, degrees, 0f);
            wing.localPosition = fold * restPosition;
            wing.localRotation = fold;
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

        /// <summary>흡혈 중(세션 + Suck 누름)이면 지팡이 하트가 1.2초 주기로 맥동하며 분홍 빛을 낸다.</summary>
        public void RefreshWandLight(Core.Simulation.GameSimulation simulation, float time)
        {
            if (_wandLight == null)
            {
                _wandLight = new GameObject("WandHeartLight").AddComponent<Light>();
                _wandLight.transform.SetParent(transform, false);
                _wandLight.transform.localPosition = new Vector3(0f, 0.1f, 0.35f);
                _wandLight.type = LightType.Point;
                _wandLight.range = WandLightRange;
                _wandLight.color = WandLightColor;
                _wandLight.shadows = LightShadows.None;
            }

            var player = simulation.Player;
            bool drinking = player.SuckSession != null && simulation.LastCommand.SuckHeld;
            _wandLight.enabled = drinking;
            _wandLight.intensity = WandLightIntensity * (0.75f + (0.25f * Mathf.Sin(2f * Mathf.PI * time / 1.2f)));
        }

        private void RefreshProximity(Core.Simulation.GameSimulation simulation)
        {
            _proximity ??= new Player.ProximityCues(null);
            _proximity.Refresh(simulation, _runner.Driver.InterpolatedPlayerPosition, Time.deltaTime);
        }

        private void OnDestroy()
        {
            _proximity?.Destroy();
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
