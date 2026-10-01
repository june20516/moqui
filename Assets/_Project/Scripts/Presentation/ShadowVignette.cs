using Moqui.Core.Data;
using Moqui.Unity.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Moqui.Unity.Presentation
{
    /// <summary>Shadow Zone 진입·이탈 시 비네트 강도를 transitionTime 동안 선형으로 보간한다 (spec/03).</summary>
    public sealed class ShadowVignetteFader
    {
        private readonly float _targetIntensity;
        private readonly float _transitionTime;

        public ShadowVignetteFader(Tuning tuning)
            : this(tuning.GetFloat("shadow.vignetteIntensity"), tuning.GetFloat("shadow.transitionTime"))
        {
        }

        public ShadowVignetteFader(float targetIntensity, float transitionTime)
        {
            _targetIntensity = targetIntensity;
            _transitionTime = transitionTime;
        }

        public float Intensity { get; private set; }

        public float TargetIntensity => _targetIntensity;

        public float Update(bool inShadow, float deltaTime)
        {
            float target = inShadow ? _targetIntensity : 0f;
            float step = _transitionTime > 0f ? _targetIntensity / _transitionTime * deltaTime : _targetIntensity;
            Intensity = Mathf.MoveTowards(Intensity, target, step);
            return Intensity;
        }
    }

    /// <summary>Core 스냅샷의 숨은 상태(InShadow)를 읽어 URP Volume의 Vignette에 반영한다.</summary>
    [RequireComponent(typeof(Volume))]
    public sealed class ShadowVignette : MonoBehaviour
    {
        [SerializeField]
        private SimulationRunner _runner;

        private ShadowVignetteFader _fader;
        private Vignette _vignette;

        public float Intensity => _vignette != null ? _vignette.intensity.value : 0f;

        /// <summary>한 프레임 진행. 테스트에서 직접 부를 수 있다.</summary>
        public void Tick(Tuning tuning, bool inShadow, float deltaTime)
        {
            EnsureInitialized(tuning);
            _vignette.intensity.Override(_fader.Update(inShadow, deltaTime));
        }

        private void LateUpdate()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            Tick(_runner.Tuning, _runner.Driver.Simulation.Player.IsHidden, Time.deltaTime);
        }

        private void EnsureInitialized(Tuning tuning)
        {
            if (_fader != null)
            {
                return;
            }

            _fader = new ShadowVignetteFader(tuning);
            var volume = GetComponent<Volume>();
            volume.isGlobal = true;
            if (volume.sharedProfile == null)
            {
                volume.sharedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            if (!volume.sharedProfile.TryGet(out _vignette))
            {
                _vignette = volume.sharedProfile.Add<Vignette>(true);
            }

            _vignette.intensity.Override(0f);
        }
    }
}
