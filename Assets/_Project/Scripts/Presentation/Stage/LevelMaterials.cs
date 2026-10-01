using UnityEngine;

namespace Moqui.Unity.Presentation.Stage
{
    /// <summary>
    /// 레벨·감각 표현에 쓰는 머티리얼 묶음. 씬에서 에셋을 참조해야 빌드에 투명 셰이더 변형이 포함된다
    /// (실행 중에 만든 머티리얼은 빌드에서 변형이 제거될 수 있다).
    /// </summary>
    public sealed class LevelMaterials : MonoBehaviour
    {
        [SerializeField]
        private Material _shadowCue;

        [SerializeField]
        private Material _steam;

        [SerializeField]
        private Material _glass;

        [SerializeField]
        private Material _web;

        [SerializeField]
        private Material _spray;

        [SerializeField]
        private Material _coilSmoke;

        [SerializeField]
        private Material _co2;

        [SerializeField]
        private Material _heat;

        [SerializeField]
        private Material _biteMark;

        public Material ShadowCue => _shadowCue;

        public Material Steam => _steam;

        public Material Glass => _glass;

        public Material Co2 => _co2;

        /// <summary>거미줄 (흰 반투명 격자, spec/06).</summary>
        public Material Web => _web;

        /// <summary>모기약 연무 (옅은 녹색 기체).</summary>
        public Material Spray => _spray;

        /// <summary>모기향 연기 (회녹색 가는 기체).</summary>
        public Material CoilSmoke => _coilSmoke;

        public Material Heat => _heat;

        public Material BiteMark => _biteMark;
    }
}
