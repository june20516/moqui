using UnityEngine;
using UnityEngine.SceneManagement;

namespace Moqui.Unity.UI
{
    /// <summary>
    /// UI 글꼴. 허용 라이선스(tech/asset-pipeline.md)의 한글 글꼴이 없어 폰트 파일을 넣지 않고,
    /// 실행 중 OS 글꼴로 동적 글꼴을 만든다 (D-041). 앞쪽 이름부터 찾는다.
    /// 씬을 바꾸면 미사용 에셋 정리로 동적 글꼴의 글리프 텍스처가 비워져 글자가 사라지므로, 활성 씬마다 새로 만든다.
    /// </summary>
    public static class UiFonts
    {
        private const int DynamicFontSize = 32;
        private static readonly string[] PreferredOsFonts = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Segoe UI", "Arial" };
        private static Font _font;
        private static Scene _scene;

        public static Font Default
        {
            get
            {
                Scene active = SceneManager.GetActiveScene();
                if (_font == null || _scene != active)
                {
                    _font = Font.CreateDynamicFontFromOSFont(PreferredOsFonts, DynamicFontSize);
                    _scene = active;
                }

                return _font;
            }
        }
    }
}
