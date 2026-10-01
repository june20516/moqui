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

        public Material ShadowCue => _shadowCue;

        public Material Steam => _steam;

        public Material Glass => _glass;
    }
}
