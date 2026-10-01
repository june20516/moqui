using UnityEngine;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>Boot 씬: 세션(저장 데이터·설정)을 준비하고 Title로 간다. 성능 측정 인자가 있으면 측정을 실행한다 (PerfRunner).</summary>
    public sealed class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            GameSession.Ensure();
            if (PerfRunner.TryStart(System.Environment.GetCommandLineArgs()))
            {
                return;
            }

            new SceneNavigator().Load(ScreenId.Title);
        }
    }
}
