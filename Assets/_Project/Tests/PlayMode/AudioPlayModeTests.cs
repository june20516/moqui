using System.Collections;
using Moqui.Unity.Presentation.Audio;
using Moqui.Unity.Presentation.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    /// <summary>오디오 출구 수명 (M11 버그 수정): 앱 종료처럼 출구가 먼저 파괴되어도 스테이지 정리에서 예외가 나지 않는다.</summary>
    public class AudioPlayModeTests
    {
        private const string StageScenePath = "Assets/_Project/Scenes/Stage.unity";
        private const string TitleScenePath = "Assets/_Project/Scenes/Title.unity";
        private const int WarmupFrames = 30;

        [UnityTest]
        public IEnumerator OutputDestroyedFirst_StageTeardownDoesNotThrow()
        {
            StageBootstrap.RequestedLevelId = "stage01";
            yield return SceneManager.LoadSceneAsync(StageScenePath, LoadSceneMode.Single);
            for (int i = 0; i < WarmupFrames; i++)
            {
                yield return null;
            }

            Assert.That(AudioOutput.Instance, Is.Not.Null);
            Object.Destroy(AudioOutput.Instance.gameObject);
            yield return null;

            // 스테이지를 떠나면 AudioDirector.OnDestroy가 반복음을 끈다. 예외가 로그되면 테스트가 실패한다.
            yield return SceneManager.LoadSceneAsync(TitleScenePath, LoadSceneMode.Single);
            yield return null;
            LogAssert.NoUnexpectedReceived();
            StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
        }
    }
}
