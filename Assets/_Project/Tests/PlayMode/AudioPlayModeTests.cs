using System.Collections;
using Moqui.Unity.Presentation.Audio;
using Moqui.Unity.Presentation.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    /// <summary>스테이지 오디오 동작 (M11 버그 수정): 일시정지 중 반복음, 출구가 먼저 파괴될 때의 정리.</summary>
    public class AudioPlayModeTests
    {
        private const string StageScenePath = "Assets/_Project/Scenes/Stage.unity";
        private const string TitleScenePath = "Assets/_Project/Scenes/Title.unity";
        private const int WarmupFrames = 30;

        /// <summary>일시정지 중에는 상태 반복음(날갯소리 등)을 끄고, 음악·환경음은 유지한다.</summary>
        [UnityTest]
        public IEnumerator Pause_SilencesStateLoops_KeepsMusicAndAmbience()
        {
            StageBootstrap.RequestedLevelId = "stage01";
            yield return SceneManager.LoadSceneAsync(StageScenePath, LoadSceneMode.Single);
            for (int i = 0; i < WarmupFrames; i++)
            {
                yield return null;
            }

            var output = AudioOutput.Instance;
            var screen = Object.FindAnyObjectByType<Moqui.Unity.UI.Flow.StageScreen>();
            Assert.That(output.IsLoopActive(AudioIds.WingLoop), Is.True, "wing loop while hovering");

            screen.Pause();
            yield return null;
            Assert.That(output.IsLoopActive(AudioIds.WingLoop), Is.False, "silent while paused");
            Assert.That(output.IsLoopActive(AudioIds.AmbienceForLevel("stage01")), Is.True, "ambience kept");
            Assert.That(output.MusicId, Is.EqualTo(AudioIds.BgmStage));

            screen.Resume();
            yield return null;
            Assert.That(output.IsLoopActive(AudioIds.WingLoop), Is.True, "wing loop resumes");
            StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
        }

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
