using UnityEngine;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>Boot 씬: 세션(저장 데이터·설정)을 준비하고 Title로 간다.</summary>
    public sealed class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            GameSession.Ensure();
            new SceneNavigator().Load(ScreenId.Title);
        }
    }
}
