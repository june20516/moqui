using UnityEngine;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>커서 잠금 정책: 플레이 중에만 잠그고(시점 조작), 메뉴·일시정지·결과 화면에서는 마우스로 누를 수 있게 푼다.</summary>
    public static class CursorPolicy
    {
        /// <summary>마지막으로 요청한 잠금 상태. 창 포커스가 없으면(배치 모드) 실제 Cursor.lockState가 바뀌지 않으므로 검증은 이 값으로 한다.</summary>
        public static CursorLockMode Requested { get; private set; } = CursorLockMode.None;

        public static void ForGameplay()
        {
            Requested = CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void ForMenu()
        {
            Requested = CursorLockMode.None;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
