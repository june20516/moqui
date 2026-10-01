using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Moqui.Unity.Presentation
{
    /// <summary>1인칭에서 자기 캐릭터 메시를 그림자만 남기고 숨긴다. 3인칭으로 돌아오면 원래 설정으로 복구한다 (spec/00).</summary>
    public sealed class PlayerViewVisibility : MonoBehaviour
    {
        private readonly Dictionary<Renderer, ShadowCastingMode> _originalModes = new Dictionary<Renderer, ShadowCastingMode>();
        private bool _firstPerson;

        public bool IsFirstPerson => _firstPerson;

        public void SetFirstPerson(bool firstPerson)
        {
            if (firstPerson == _firstPerson)
            {
                return;
            }

            _firstPerson = firstPerson;
            if (firstPerson)
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
