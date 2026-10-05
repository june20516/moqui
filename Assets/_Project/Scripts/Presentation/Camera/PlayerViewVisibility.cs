using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Moqui.Unity.Presentation
{
    /// <summary>1인칭이거나 3인칭 카메라가 몸에 너무 가까우면 자기 캐릭터 메시를 그림자만 남기고 숨긴다. 다시 보이면 원래 설정으로 복구한다 (spec/00, M13).</summary>
    public sealed class PlayerViewVisibility : MonoBehaviour
    {
        private readonly Dictionary<Renderer, ShadowCastingMode> _originalModes = new Dictionary<Renderer, ShadowCastingMode>();
        private bool _hidden;

        public bool IsHidden => _hidden;

        public void SetHidden(bool hidden)
        {
            if (hidden == _hidden)
            {
                return;
            }

            _hidden = hidden;
            if (hidden)
            {
                _originalModes.Clear();
                foreach (var meshRenderer in GetComponentsInChildren<Renderer>(true))
                {
                    _originalModes[meshRenderer] = meshRenderer.shadowCastingMode;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }

                return;
            }

            foreach (var pair in _originalModes)
            {
                if (pair.Key != null)
                {
                    pair.Key.shadowCastingMode = pair.Value;
                }
            }

            _originalModes.Clear();
        }
    }
}
