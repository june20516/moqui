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
        }

        protected abstract void Build();
    }
}
