using Moqui.Unity.Presentation.Audio;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>버튼이 선택(포커스)되면 선택음을 낸다 (게임패드·키보드 이동, spec/10 sfx_ui_select).</summary>
    public sealed class UiSelectSound : MonoBehaviour, ISelectHandler
    {
        public void OnSelect(BaseEventData eventData)
        {
            AudioOutput.Ensure()?.PlayOneShot(AudioIds.UiSelect);
        }
    }
}
