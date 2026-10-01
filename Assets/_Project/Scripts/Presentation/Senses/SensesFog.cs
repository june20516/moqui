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

        public static void Apply(SensesSettings settings, Vector3 origin, bool inSteam)
        {
            Shader.SetGlobalVector(OriginId, origin);
            Shader.SetGlobalFloat(ClearId, FogModel.ClearRange(settings, inSteam));
            Shader.SetGlobalFloat(FullId, settings.FogFullRange);
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
            var player = _runner.Driver.Simulation.Player;
            Apply(_settings, player.Position.ToUnity(), player.InSteam);
        }

        private void OnDisable()
        {
            Disable();
        }
    }
}
