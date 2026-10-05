using Moqui.Unity.Simulation;
using UnityEngine;

namespace Moqui.Unity.Presentation.Senses
{
    /// <summary>흐린 시야의 거리 규칙 (spec/11 §1). 셰이더 `Moqui/SensesFog`와 같은 식을 쓴다.</summary>
    public static class FogModel
    {
        /// <summary>선명 거리. 증기 속이면 humid.steamClearRangeMul배, 겹눈 각성은 더한다 (spec/05, M8 스킬).</summary>
        public static float ClearRange(SensesSettings settings, bool inSteam, float compoundEyesAdd = 0f)
        {
            float clear = settings.ClearRange + compoundEyesAdd;
            return inSteam ? clear * settings.SteamClearRangeMul : clear;
        }

        /// <summary>관망 정도 p (0~1)에서의 선명 거리: 기본 선명 거리 × lerp(1, perch.clearRangeMul, p) (M13).</summary>
        public static float PerchClearRange(SensesSettings settings, float clearRange, float perch)
        {
            return clearRange * Mathf.Lerp(1f, settings.PerchClearRangeMul, perch);
        }

        /// <summary>관망 정도 p (0~1)에서의 최대 흐림 거리 (M13).</summary>
        public static float PerchFullRange(SensesSettings settings, float perch)
        {
            return settings.FogFullRange * Mathf.Lerp(1f, settings.PerchFogFullRangeMul, perch);
        }

        /// <summary>
        /// 관망 정도를 한 프레임 진행한다 (M13): 붙은 지 perch.delay가 지나면 perch.blendTime에 걸쳐 1로, 떨어지면 같은 속도로 0으로.
        /// </summary>
        public static float StepPerch(SensesSettings settings, float perch, float attachedSeconds, bool attached, float deltaTime)
        {
            float target = attached && attachedSeconds >= settings.PerchDelay ? 1f : 0f;
            float rate = settings.PerchBlendTime > 0f ? deltaTime / settings.PerchBlendTime : 1f;
            return Mathf.MoveTowards(perch, target, rate);
        }

        /// <summary>기준점에서 distance만큼 떨어진 곳의 흐림 (0 = 선명, maxDensity = 최대 흐림).</summary>
        public static float Amount(float distance, float clearRange, float fullRange, float maxDensity)
        {
            float span = Mathf.Max(fullRange - clearRange, Mathf.Epsilon);
            return Mathf.Clamp01((distance - clearRange) / span) * maxDensity;
        }
    }

    /// <summary>
    /// 흐린 시야 셰이더의 전역 파라미터를 프레임마다 설정한다. 거리 기준점은 카메라가 아니라 플레이어다
    /// (1인칭과 3인칭이 같은 거리 기준을 쓰도록). 이 컴포넌트가 없는 씬은 최대 흐림 0이라 안개가 없다.
    /// </summary>
    public sealed class SensesFog : MonoBehaviour
    {
        public static readonly int OriginId = Shader.PropertyToID("_MoquiFogOrigin");
        public static readonly int ClearId = Shader.PropertyToID("_MoquiFogClear");
        public static readonly int FullId = Shader.PropertyToID("_MoquiFogFull");
        public static readonly int MaxId = Shader.PropertyToID("_MoquiFogMax");
        public static readonly int BlurId = Shader.PropertyToID("_MoquiFogBlur");

        [SerializeField]
        private SimulationRunner _runner;

        private SensesSettings _settings;

        /// <summary>지금 관망 정도 (0~1, M13).</summary>
        public float Perch { get; private set; }

        /// <param name="perch">관망 정도 0~1 (붙어서 지켜보면 시야가 넓어진다, M13).</param>
        public static void Apply(SensesSettings settings, Vector3 origin, bool inSteam, float perch = 0f)
        {
            Shader.SetGlobalVector(OriginId, origin);
            Shader.SetGlobalFloat(ClearId, FogModel.PerchClearRange(settings, FogModel.ClearRange(settings, inSteam), perch));
            Shader.SetGlobalFloat(FullId, FogModel.PerchFullRange(settings, perch));
            Shader.SetGlobalFloat(MaxId, settings.FogMaxDensity);
            Shader.SetGlobalFloat(BlurId, settings.FogBlurPixels);
        }

        /// <summary>안개를 끈다 (Sandbox 씬, 씬 종료).</summary>
        public static void Disable()
        {
            Shader.SetGlobalFloat(MaxId, 0f);
            Shader.SetGlobalFloat(BlurId, 0f);
        }

        private void LateUpdate()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            _settings ??= new SensesSettings(_runner.Tuning);
            var simulation = _runner.Driver.Simulation;
            var player = simulation.Player;
            bool attached = player.State == Core.Simulation.PlayerState.Attached;
            float attachedSeconds = attached ? (simulation.Tick - player.AttachedTick) * Core.Simulation.GameSimulation.DeltaTime : 0f;
            Perch = FogModel.StepPerch(_settings, Perch, attachedSeconds, attached, Time.deltaTime);
            Apply(_settings, player.Position.ToUnity(), player.InSteam, Perch);
        }

        private void OnDisable()
        {
            Disable();
        }
    }
}
