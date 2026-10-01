using Moqui.Unity.Presentation.Audio;
using UnityEngine;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// 화면 하나. 실행 중에는 Awake에서 기본 세션·씬 내비게이터로 만들고, 테스트는 Initialize로 가짜 내비게이터를 넣는다.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        public ScreenFlow Flow { get; private set; }

        public bool IsBuilt => Flow != null;

        /// <summary>이 화면에서 틀 음악 (null이면 바꾸지 않는다). 메뉴 화면은 타이틀 음악을 이어서 튼다.</summary>
        protected virtual string MusicId => AudioIds.BgmTitle;

        /// <summary>이 화면이 플레이 화면인가 (커서를 잠근다). 메뉴 화면은 커서를 푼다.</summary>
        protected virtual bool IsGameplay => false;

        public void Initialize(ScreenFlow flow)
        {
            if (IsBuilt)
            {
                return;
            }

            Flow = flow;
            UiFactory.EnsureEventSystem();
            Build();
        }

        protected virtual void Awake()
        {
            Initialize(new ScreenFlow(GameSession.Ensure(), new SceneNavigator()));
            if (IsGameplay)
            {
                CursorPolicy.ForGameplay();
            }
            else
            {
                CursorPolicy.ForMenu();
            }

            if (MusicId != null)
            {
                AudioOutput.Ensure()?.PlayMusic(MusicId);
            }
        }

        protected abstract void Build();
    }
}
